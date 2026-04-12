using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ApiException apiException)
        {
            await WriteProblemAsync(httpContext, apiException.StatusCode, apiException.Message, apiException.Errors, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception while executing request {Path}", httpContext.Request.Path);
        await WriteProblemAsync(httpContext, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null, cancellationToken);
        return true;
    }

    private static Task WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        IDictionary<string, string[]>? errors,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        if (errors is not null && errors.Count > 0)
        {
            var validationProblem = new HttpValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title = title,
                Type = $"https://httpstatuses.com/{statusCode}",
                Instance = httpContext.Request.Path
            };

            return httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken: cancellationToken);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };

        return httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
    }
}