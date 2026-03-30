using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Trickler_API.Exceptions;

namespace Trickler_API.Middleware
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger = logger;

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            var (statusCode, title, detail) = exception switch
            {
                TrickleNotFoundException ex => (
                    StatusCodes.Status404NotFound,
                    "Trickle Not Found",
                    ex.Message
                ),
                AppValidationException ex => (
                    StatusCodes.Status400BadRequest,
                    "Validation Error",
                    ex.Message
                ),
                AuthenticationException ex => (
                    StatusCodes.Status401Unauthorized,
                    "Authentication Error",
                    ex.Message
                ),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred. Please try again later."
                )
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
