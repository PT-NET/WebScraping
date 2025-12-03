using FluentValidation;
using System.Net;
using System.Text.Json;
using WebScraping.Domain.Exceptions;

namespace WebScraping.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

            var (statusCode, message, errors) = exception switch
            {
                ValidationException validationEx => (
                    HttpStatusCode.BadRequest,
                    "Validation failed",
                    validationEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).Cast<object>().ToList()
                ),
                RateLimitExceededException rateLimitEx => (
                    HttpStatusCode.TooManyRequests,
                    rateLimitEx.Message,
                    new List<object> { new { RetryAfter = rateLimitEx.RetryAfterSeconds } }
                ),
                ScrapingException scrapingEx => (
                    HttpStatusCode.ServiceUnavailable,
                    $"Service temporarily unavailable: {scrapingEx.Source}",
                    new List<object> { new { Source = scrapingEx.Source.ToString(), Details = scrapingEx.Message } }
                ),
                ArgumentException => (
                    HttpStatusCode.BadRequest,
                    exception.Message,
                    null as List<object>
                ),
                _ => (
                    HttpStatusCode.InternalServerError,
                    "An internal server error occurred",
                    null as List<object>
                )
            };

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            if (exception is RateLimitExceededException rateLimitException)
            {
                context.Response.Headers.Append("Retry-After", rateLimitException.RetryAfterSeconds.ToString());
            }

            var response = new
            {
                status = (int)statusCode,
                message,
                errors,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
