using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RBurger.Application.Common.Exceptions;

namespace RBurger.Api.Common;

// §5.5: "Global exception handler -> RFC 7807 ProblemDetails JSON on every error, with a
// stable errorCode field the clients can localize."
// §7.0: Error envelope -> { "type", "title", "status", "errorCode", "traceId" }.
// Minimal scope per approved decision #5 - no additional middleware/features beyond this.
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, errorCode, errors) = Map(exception);

        _logger.LogError(exception, "Unhandled exception mapped to {Status} {ErrorCode}", status, errorCode);

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status
        };
        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (errors is not null)
        {
            // §7.8: "VALIDATION_ERROR, details per-field" - exact field name for the per-field
            // details is not given literally in the doc; "errors" is an implementation decision
            // (flagged in the Day 3 report).
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int Status, string Title, string ErrorCode, object? Errors) Map(Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                "VALIDATION_ERROR",
                validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                conflictEx.Message,
                conflictEx.ErrorCode,
                null),

            InvalidCredentialsException invalidCredentialsEx => (
                StatusCodes.Status401Unauthorized,
                invalidCredentialsEx.Message,
                "INVALID_CREDENTIALS",
                null),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                notFoundEx.Message,
                "NOT_FOUND",
                null),

            // §7.8: 403 Forbidden - "not the resource owner" (Day 4 addition).
            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                forbiddenEx.Message,
                "FORBIDDEN",
                null),

            // §7.8: 422 Unprocessable Entity - "Business-rule violation" (Day 4 addition).
            UnprocessableEntityException unprocessableEx => (
                StatusCodes.Status422UnprocessableEntity,
                unprocessableEx.Message,
                unprocessableEx.ErrorCode,
                null),

            // Day 10 addition (Blocking Issue #2, scaffold-only decision). Not a §7.8-documented
            // status code - honest "feature not yet deployed" signal for the menu-photo
            // upload/delete endpoints, distinct from every documented client/business error.
            StorageNotConfiguredException storageEx => (
                StatusCodes.Status503ServiceUnavailable,
                storageEx.Message,
                "IMAGE_STORAGE_NOT_CONFIGURED",
                null),

            // Day 12 addition (Blocking Issue #3, same scaffold-only pattern as
            // StorageNotConfiguredException above - approved). Honest "feature not yet
            // deployed" signal for POST /payments/{orderId}/charge, POST /payments/webhook,
            // and the card-refund branch of DELETE /admin/orders/{id} (§9.4) - no Paymob/Fawry
            // credentials exist in Documentation v1.2.
            PaymentProviderNotConfiguredException paymentProviderEx => (
                StatusCodes.Status503ServiceUnavailable,
                paymentProviderEx.Message,
                "PAYMENT_PROVIDER_NOT_CONFIGURED",
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "INTERNAL_SERVER_ERROR",
                null)
        };
    }
}
