using System.ServiceModel;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Modern.Facade;

/// <summary>
/// Turns the failure modes of talking to a legacy backend into honest RFC 7807 responses (standard
/// rule 6) for the REST surface: a SOAP fault is a 400, an open circuit or a timeout is a 5xx that
/// says "the legacy backend is unavailable", not a leaked stack trace.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int status, string title) = exception switch
        {
            FaultException => (StatusCodes.Status400BadRequest, "The legacy service rejected the request"),
            BrokenCircuitException => (StatusCodes.Status503ServiceUnavailable, "The legacy backend is unavailable"),
            TimeoutRejectedException => (StatusCodes.Status504GatewayTimeout, "The legacy backend timed out"),
            CommunicationException => (StatusCodes.Status502BadGateway, "The legacy backend could not be reached"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Type = $"https://httpstatuses.io/{status}"
        };
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
