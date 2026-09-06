using MediatR;
using RBurger.Application.Common.Behaviors;
using RBurger.Application.Common.Interfaces;
using Xunit;

namespace RBurger.Application.Tests.Common;

// Day 15 addition (Backend Parity Spec §12).
public class CachingBehaviorTests
{
    private record TestQuery(string Key) : IRequest<TestResponse>, ICacheableQuery
    {
        string ICacheableQuery.CacheKey => Key;
        int ICacheableQuery.CacheDurationSeconds => 60;
    }

    private record TestResponse(string Value);
    private record PlainQuery : IRequest<TestResponse>;

    [Fact]
    public async Task Handle_cache_miss_executes_and_stores_the_result()
    {
        var behavior = new CachingBehavior<TestQuery, TestResponse>(new FakeCacheService());
        var executions = 0;

        var result = await behavior.Handle(
            new TestQuery("key-1"),
            () => { executions++; return Task.FromResult(new TestResponse("computed")); },
            default);

        Assert.Equal(1, executions);
        Assert.Equal("computed", result.Value);
    }

    [Fact]
    public async Task Handle_cache_hit_returns_stored_result_without_re_executing()
    {
        var cache = new FakeCacheService();
        var behavior = new CachingBehavior<TestQuery, TestResponse>(cache);
        var executions = 0;

        Task<TestResponse> Next() { executions++; return Task.FromResult(new TestResponse($"result-{executions}")); }

        var first = await behavior.Handle(new TestQuery("key-1"), Next, default);
        var second = await behavior.Handle(new TestQuery("key-1"), Next, default);

        Assert.Equal(1, executions); // second call served entirely from cache
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task Handle_different_cache_keys_execute_independently()
    {
        var behavior = new CachingBehavior<TestQuery, TestResponse>(new FakeCacheService());
        var executions = 0;

        Task<TestResponse> Next() { executions++; return Task.FromResult(new TestResponse($"result-{executions}")); }

        await behavior.Handle(new TestQuery("key-1"), Next, default);
        await behavior.Handle(new TestQuery("key-2"), Next, default);

        Assert.Equal(2, executions);
    }

    [Fact]
    public async Task Handle_non_cacheable_request_passes_through_unaffected()
    {
        var behavior = new CachingBehavior<PlainQuery, TestResponse>(new FakeCacheService());
        var executions = 0;

        var result = await behavior.Handle(
            new PlainQuery(), () => { executions++; return Task.FromResult(new TestResponse("plain")); }, default);

        Assert.Equal(1, executions);
        Assert.Equal("plain", result.Value);
    }
}
