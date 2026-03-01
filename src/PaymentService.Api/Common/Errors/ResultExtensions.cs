using Microsoft.AspNetCore.Mvc;
using PaymentService.Domain.Common;

namespace PaymentService.Api.Common.Errors;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new OkResult();
        }

        return result.Error.Type switch
        {
            ErrorType.Validation => new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            }),

            ErrorType.NotFound => new NotFoundObjectResult(new ProblemDetails
            {
                Title = "Not Found",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            }),

            ErrorType.Conflict => new ConflictObjectResult(new ProblemDetails
            {
                Title = "Conflict",
                Detail = result.Error.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            }),

            ErrorType.Unauthorized => new UnauthorizedObjectResult(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = result.Error.Message,
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.2"
            }),

            ErrorType.Forbidden => new ObjectResult(new ProblemDetails
            {
                Title = "Forbidden",
                Detail = result.Error.Message,
                Status = StatusCodes.Status403Forbidden,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            _ => new ObjectResult(new ProblemDetails
            {
                Title = "Server Error",
                Detail = result.Error.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return result.Error.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(new ProblemDetails
            {
                Title = "Not Found",
                Detail = result.Error.Message,
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            }),

            ErrorType.Validation => new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            }),

            ErrorType.Conflict => new ConflictObjectResult(new ProblemDetails
            {
                Title = "Conflict",
                Detail = result.Error.Message,
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            }),

            _ => new ObjectResult(new ProblemDetails
            {
                Title = "Server Error",
                Detail = result.Error.Message,
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
    }
}