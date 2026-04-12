using Microsoft.AspNetCore.Http;

namespace ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;

public sealed class ApiException : Exception
{
    public ApiException(int statusCode, string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IDictionary<string, string[]>? Errors { get; }

    public static ApiException Validation(string message, IDictionary<string, string[]> errors) =>
        new(StatusCodes.Status400BadRequest, message, errors);
}