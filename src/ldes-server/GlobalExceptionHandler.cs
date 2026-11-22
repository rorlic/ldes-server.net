using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace LdesServer;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const int RetryAfterSeconds = 5;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var overloaded = IsOverload(exception);
        var status = overloaded
            ? StatusCodes.Status429TooManyRequests
            : exception is ArgumentException
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = overloaded
                ? "Too Many Requests"
                : exception is ArgumentException
                    ? "Bad Request"
                    : "Internal Server Error",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = status;
        if (overloaded) httpContext.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    // Database pool exhaustion or timeouts mean the server is overloaded.
    // NOTE: the exact exception type for pool exhaustion varies between Npgsql versions; verify with a test.
    private static bool IsOverload(Exception e) =>
        e is TimeoutException
        || (e is InvalidOperationException &&
            e.Message.Contains("connection pool has been exhausted", StringComparison.OrdinalIgnoreCase))
        || (e is NpgsqlException { InnerException: TimeoutException })
        || (e is NpgsqlException { SqlState: PostgresErrorCodes.TooManyConnections }); // too_many_connections
}
