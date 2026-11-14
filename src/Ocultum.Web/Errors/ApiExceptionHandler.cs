using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ocultum.Core.Errors;

namespace Ocultum.Web.Errors;

public sealed class ApiExceptionHandler(IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var problem = Describe(exception);
        if (problem.Status >= 500)
        {
            logger.LogError(exception, "Unhandled error while serving {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            jsonOptions.Value.SerializerOptions,
            contentType: "application/problem+json",
            cancellationToken);
        return true;
    }

    public static ProblemDetails Create(int status, string errorCode, string title, string? detail = null)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["errorCode"] = errorCode;
        return problem;
    }

    private static ProblemDetails Describe(Exception exception)
    {
        switch (exception)
        {
            case OcultumException known:
                var problem = Create(StatusOf(known), known.ErrorCode, known.Message, known.Detail);
                foreach (var (key, value) in known.Extensions)
                {
                    if (value is not null)
                    {
                        problem.Extensions[key] = value;
                    }
                }

                return problem;

            case BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }:
            case InvalidDataException when exception.Message.Contains("length limit", StringComparison.OrdinalIgnoreCase):
                return Create(
                    StatusCodes.Status413PayloadTooLarge,
                    "FILE_TOO_LARGE",
                    "The request is larger than the allowed limit.");

            case BadHttpRequestException or InvalidDataException:
                return Create(StatusCodes.Status400BadRequest, "INVALID_REQUEST", "The request could not be read.");

            default:
                return Create(StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static int StatusOf(OcultumException exception) => exception switch
    {
        InvalidInputException => StatusCodes.Status400BadRequest,
        ResourceNotFoundException => StatusCodes.Status404NotFound,
        FileTooLargeException => StatusCodes.Status413PayloadTooLarge,
        UnsupportedCoverFormatException => StatusCodes.Status415UnsupportedMediaType,
        CapacityExceededException or WrongPassphraseOrNoDataException or EmptyPayloadException or ImageProcessingException
            or CoverNotProcessableException
            => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };
}
