using CloudCore.Models;
using MySqlConnector;
using System.Text.Json;

namespace CloudCore.Services
{
    public class GlobalErrorHandler
    {
        private readonly RequestDelegate _next;

        public GlobalErrorHandler(RequestDelegate next) => _next = next;

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

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statuscode, response) = exception switch
            {
                MySqlException => (StatusCodes.Status500InternalServerError, ApiResponse.Error("Database error occurred", "DATABASE_ERROR")),

                TimeoutException => (StatusCodes.Status408RequestTimeout, ApiResponse.Error("Request timeout", "TIMEOUT_ERROR")),

                InvalidOperationException invOpEx => (StatusCodes.Status400BadRequest, ApiResponse.Error($"Invalid operation: {invOpEx.Message}", "INVALID_OPERATION")),

                UnauthorizedAccessException unAcEx => (StatusCodes.Status401Unauthorized, ApiResponse.Error($"Access denied to file system: {unAcEx.Message}", "FILE_SYSTEM_ACCESS_DENIED")),

                DirectoryNotFoundException dirNtFdEx => (StatusCodes.Status404NotFound, ApiResponse.Error($"Directory not found: {dirNtFdEx.Message}", "DIRECTORY_NOT_FOUND")),

                IOException ioEx => (StatusCodes.Status409Conflict, ApiResponse.Error($"File system conflict occurred: {ioEx.Message}", "FILE_SYSTEM_CONFLICT")),

                

                _ => (StatusCodes.Status500InternalServerError,
                      ApiResponse.Error("Internal server error", "INTERNAL_ERROR"))
            };

            context.Response.StatusCode = statuscode;
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
