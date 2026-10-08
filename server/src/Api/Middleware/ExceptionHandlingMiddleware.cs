using System.Text.Json;
using Domain.Exceptions;
using FluentValidation;

namespace Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // invoke the next action
            await _next(context);
        }
        catch (Exception ex)
        {
            // handle exceptions
            _logger.LogError(ex, "An unhandled exception occured while processing the request {Path}: {Message}", context.Request.Path, ex.Message);
            await HandleException(context, ex);
        }
    }

    private static Task HandleException(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // determine the status code and message to return
        var (statusCode, message) = exception switch
        {
            ValidationException ex => (StatusCodes.Status400BadRequest, ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, exception.Message)
        };

        context.Response.StatusCode = statusCode;
        // create the response object
        var response = new
        {
            statusCode,
            message,
            timestamp = DateTime.UtcNow
        };

        var jsonOptions = new JsonSerializerOptions{ PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));

    }
}