using UrlShortener.Api.Core;

namespace UrlShortener.Api.Common;

/// <summary>Maps a domain <see cref="Error"/> to an HTTP result (RFC 7807 ProblemDetails).</summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Error error) => error.Type switch
    {
        ErrorType.Validation => Results.Problem(
            detail: error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Bad Request"),
        ErrorType.Conflict => Results.Problem(
            detail: error.Message, statusCode: StatusCodes.Status409Conflict, title: "Conflict"),
        ErrorType.NotFound => Results.NotFound(),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}
