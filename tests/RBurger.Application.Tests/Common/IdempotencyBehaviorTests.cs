using MediatR;
using RBurger.Application.Common.Behaviors;
using RBurger.Application.Common.Exceptions;
using RBurger.Application.Common.Interfaces;
using Xunit;

namespace RBurger.Application.Tests.Common;

public class IdempotencyBehaviorTests
{
    // Minimal stand-in for one of the three real IIdempotentRequest commands
    // (CreateOrderCommand/ChargePaymentCommand/UploadMenuItemImageCommand) - exercises exactly
    // the same IdempotencyBehavior code path without pulling in Orders/Payments/Menu
    // dependencies, following this test project's established "dependency-free fake" style.
    private record TestCommand(Guid ScopeId, string? Key, string Payload)
        : IRequest<TestResponse>, IIdempotentRequest
    {
        string? IIdempotentRequest.IdempotencyKey => Key;
        string IIdempotentRequest.IdempotencyEndpoint => "test:endpoint";
        Guid IIdempotentRequest.IdempotencyScopeId => ScopeId;
        string IIdempotentRequest.IdempotencyFingerprint => Payload;
    }

    private record TestResponse(string Value);

    private static IdempotencyBehavior<TestCommand, TestResponse> BuildBehavior(
        out FakeIdempotencyRepository repository)
    {
        repository = new FakeIdempotencyRepository();
        return new IdempotencyBehavior<TestCommand, TestResponse>(repository);
    }

    [Fact]
    public async Task Handle_first_request_executes_and_stores_the_result()
    {
        var behavior = BuildBehavior(out var repository);
        var scopeId = Guid.NewGuid();
        var executions = 0;

        var response = await behavior.Handle(
            new TestCommand(scopeId, "key-1", "payload-A"),
            () => { executions++; return Task.FromResult(new TestResponse("first-result")); },
            default);

        Assert.Equal(1, executions);
        Assert.Equal("first-result", response.Value);

        var stored = Assert.Single(repository.Records);
        Assert.Equal("Completed", stored.Status);
        Assert.Contains("first-result", stored.ResponseJson);
    }

    [Fact]
    public async Task Handle_retry_with_same_key_and_same_request_returns_the_original_result_without_re_executing()
    {
        var behavior = BuildBehavior(out _);
        var scopeId = Guid.NewGuid();
        var executions = 0;

        Task<TestResponse> Next() { executions++; return Task.FromResult(new TestResponse($"result-{executions}")); }

        var first = await behavior.Handle(new TestCommand(scopeId, "key-1", "payload-A"), Next, default);
        var second = await behavior.Handle(new TestCommand(scopeId, "key-1", "payload-A"), Next, default);

        Assert.Equal(1, executions); // the underlying write only ran once
        Assert.Equal(first.Value, second.Value);
        Assert.Equal("result-1", second.Value); // replayed, not a fresh "result-2"
    }

    [Fact]
    public async Task Handle_same_key_with_different_request_is_rejected()
    {
        var behavior = BuildBehavior(out _);
        var scopeId = Guid.NewGuid();

        await behavior.Handle(
            new TestCommand(scopeId, "key-1", "payload-A"),
            () => Task.FromResult(new TestResponse("result")),
            default);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            behavior.Handle(
                new TestCommand(scopeId, "key-1", "payload-B"), // same key, different fingerprint
                () => Task.FromResult(new TestResponse("should-not-run")),
                default));

        Assert.Equal("IDEMPOTENCY_KEY_REUSED", ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_different_users_can_use_the_same_key_independently()
    {
        var behavior = BuildBehavior(out _);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var resultA = await behavior.Handle(
            new TestCommand(userA, "same-key", "payload-A"),
            () => Task.FromResult(new TestResponse("A-result")),
            default);

        var resultB = await behavior.Handle(
            new TestCommand(userB, "same-key", "payload-B"), // same key value, different ScopeId
            () => Task.FromResult(new TestResponse("B-result")),
            default);

        Assert.Equal("A-result", resultA.Value);
        Assert.Equal("B-result", resultB.Value); // not rejected, not replayed from A's record
    }

    [Fact]
    public async Task Handle_persisted_record_is_reused_across_separate_behavior_instances()
    {
        // Two separate IdempotencyBehavior instances sharing one repository instance stands in
        // for "survives an application restart": the behavior itself holds no state - every
        // correctness guarantee lives in the repository (DB-backed in production), so a brand
        // new behavior instance (as a fresh request would construct via DI) must still replay
        // correctly from what a prior instance persisted.
        var repository = new FakeIdempotencyRepository();
        var firstBehaviorInstance = new IdempotencyBehavior<TestCommand, TestResponse>(repository);
        var secondBehaviorInstance = new IdempotencyBehavior<TestCommand, TestResponse>(repository);
        var scopeId = Guid.NewGuid();
        var executions = 0;

        Task<TestResponse> Next() { executions++; return Task.FromResult(new TestResponse("stored-result")); }

        await firstBehaviorInstance.Handle(new TestCommand(scopeId, "key-1", "payload-A"), Next, default);
        var replayed = await secondBehaviorInstance.Handle(
            new TestCommand(scopeId, "key-1", "payload-A"), Next, default);

        Assert.Equal(1, executions);
        Assert.Equal("stored-result", replayed.Value);
    }

    // Concurrent requests must not execute twice: simulated by having the FIRST call's "next"
    // delegate itself invoke a SECOND overlapping Handle call for the identical key before the
    // first one completes - reproducing the exact window a real concurrent request would race
    // into (the first request's IdempotencyRecord row already exists with Status=InProgress,
    // no Completed result yet).
    [Fact]
    public async Task Handle_concurrent_requests_with_the_same_key_do_not_execute_the_operation_twice()
    {
        var behavior = BuildBehavior(out _);
        var scopeId = Guid.NewGuid();
        var executions = 0;

        async Task<TestResponse> OuterNext()
        {
            executions++;

            // The "concurrent" second caller, racing in while the first is still InProgress.
            var concurrentEx = await Assert.ThrowsAsync<ConflictException>(() =>
                behavior.Handle(
                    new TestCommand(scopeId, "key-1", "payload-A"),
                    () => { executions++; return Task.FromResult(new TestResponse("should-not-run")); },
                    default));
            Assert.Equal("IDEMPOTENCY_KEY_IN_PROGRESS", concurrentEx.ErrorCode);

            return new TestResponse("first-result");
        }

        var result = await behavior.Handle(new TestCommand(scopeId, "key-1", "payload-A"), OuterNext, default);

        Assert.Equal(1, executions); // only the first, outer call ever actually executed
        Assert.Equal("first-result", result.Value);
    }

    // A failed attempt must not permanently consume the key - the caller should be able to
    // retry once the underlying problem is fixed (IdempotencyBehavior's XML comment).
    [Fact]
    public async Task Handle_failed_attempt_does_not_lock_the_key_and_allows_a_retry()
    {
        var behavior = BuildBehavior(out var repository);
        var scopeId = Guid.NewGuid();
        var attempt = 0;

        Task<TestResponse> FailThenSucceed()
        {
            attempt++;
            if (attempt == 1)
            {
                throw new InvalidOperationException("transient failure");
            }

            return Task.FromResult(new TestResponse("succeeded-on-retry"));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(new TestCommand(scopeId, "key-1", "payload-A"), FailThenSucceed, default));

        Assert.Empty(repository.Records); // abandoned, not left dangling as "InProgress"

        var result = await behavior.Handle(
            new TestCommand(scopeId, "key-1", "payload-A"), FailThenSucceed, default);

        Assert.Equal(2, attempt);
        Assert.Equal("succeeded-on-retry", result.Value);
    }

    // Requests that don't implement IIdempotentRequest must pass straight through untouched -
    // confirms this behavior never applies idempotency to an undocumented endpoint.
    private record PlainCommand : IRequest<TestResponse>;

    [Fact]
    public async Task Handle_non_idempotent_request_passes_through_unaffected()
    {
        var repository = new FakeIdempotencyRepository();
        var behavior = new IdempotencyBehavior<PlainCommand, TestResponse>(repository);
        var executions = 0;

        var result = await behavior.Handle(
            new PlainCommand(),
            () => { executions++; return Task.FromResult(new TestResponse("plain-result")); },
            default);

        Assert.Equal(1, executions);
        Assert.Equal("plain-result", result.Value);
        Assert.Empty(repository.Records);
    }
}
