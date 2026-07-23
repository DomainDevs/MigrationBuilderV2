using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.DTOs;
using Shared.Exceptions;
using System.Net;

namespace Infrastructure.Middlewares;

/// <summary>
/// Middleware responsable de capturar todas las excepciones no controladas,
/// registrarlas y devolver una respuesta JSON uniforme al cliente.
/// </summary>
public sealed class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlerMiddleware> logger)
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
        //finally{}
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(
                exception,
                "The response has already started. The error response could not be written.");

            return;
        }

        var statusCode = GetStatusCode(exception);
        var response = CreateResponse(exception);

        LogException(exception, statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsJsonAsync(response);
    }

    private static HttpStatusCode GetStatusCode(Exception exception) =>
        exception switch
        {
            ControlValidationException => HttpStatusCode.BadRequest,

            ArgumentNullException => HttpStatusCode.BadRequest,

            ArgumentException => HttpStatusCode.BadRequest,

            UnauthorizedAccessException => HttpStatusCode.Unauthorized,

            KeyNotFoundException => HttpStatusCode.NotFound,

            ApplicationException => HttpStatusCode.Conflict,

            _ => HttpStatusCode.InternalServerError
        };

    private static ResponseDTO<object> CreateResponse(Exception exception)
    {
        if (exception is ControlValidationException validation)
        {
            return new ResponseDTO<object>
            {
                Success = false,
                Message = validation.Message,
                ErrorCode = GetErrorCode(validation),
                Errors = validation.Errors.ToDictionary(
                    item => item.Key,
                    item => item.Value.ToList())
            };
        }

        return new ResponseDTO<object>
        {
            Success = false,
            Message = exception.Message,
            ErrorCode = GetErrorCode(exception)
        };
    }

    private static string GetErrorCode(Exception exception) =>
        exception switch
        {
            ControlValidationException => "VALIDATION_ERROR",

            ArgumentNullException => "ARGUMENT_NULL",

            ArgumentException => "INVALID_ARGUMENT",

            UnauthorizedAccessException => "UNAUTHORIZED",

            KeyNotFoundException => "NOT_FOUND",

            ApplicationException => "BUSINESS_ERROR",

            _ => "INTERNAL_ERROR"
        };

    private void LogException(Exception exception, HttpStatusCode statusCode)
    {
        if ((int)statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception ({ExceptionType}).",
                exception.GetType().Name);

            return;
        }

        _logger.LogWarning(
            exception,
            "Handled exception ({ExceptionType}).",
            exception.GetType().Name);
    }
}