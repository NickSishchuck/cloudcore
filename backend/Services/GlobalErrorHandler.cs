using CloudCore.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security;
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

                InvalidOperationException invOpEx => (StatusCodes.Status400BadRequest, ApiResponse.Error($"Invalid operation: {invOpEx.Message}", "INVALID_OPERATION")),
                DirectoryNotFoundException dirNtFdEx => (StatusCodes.Status404NotFound, ApiResponse.Error($"Directory not found: {dirNtFdEx.Message}", "DIRECTORY_NOT_FOUND")),
                FileNotFoundException => (StatusCodes.Status404NotFound, ApiResponse.Error("File not found", "FILE_NOT_FOUND")),
                IOException ioEx => (StatusCodes.Status409Conflict, ApiResponse.Error($"File system conflict occurred: {ioEx.Message}", "FILE_SYSTEM_CONFLICT")),


                ArgumentNullException argNullEx => (StatusCodes.Status400BadRequest, ApiResponse.Error($"Required parameter is missing: {argNullEx.ParamName}", "MISSING_PARAMETER")),
                ArgumentException argEx => (StatusCodes.Status400BadRequest, ApiResponse.Error($"Invalid argument: {argEx.Message}", "INVALID_ARGUMENT")),
                ValidationException valEx => (StatusCodes.Status400BadRequest, ApiResponse.Error($"Validation failed: {valEx.Message}", "VALIDATION_ERROR")),

                HttpRequestException httpEx => (StatusCodes.Status502BadGateway, ApiResponse.Error($"External service error: {httpEx.Message}", "EXTERNAL_SERVICE_ERROR")),
                TaskCanceledException => (StatusCodes.Status408RequestTimeout, ApiResponse.Error("Operation was cancelled due to timeout", "OPERATION_CANCELLED")),
                SocketException sockEx => (StatusCodes.Status503ServiceUnavailable, ApiResponse.Error($"Network error: {sockEx.Message}", "NETWORK_ERROR")),

                UnauthorizedAccessException unAcEx => (StatusCodes.Status401Unauthorized, ApiResponse.Error($"Access denied to file system: {unAcEx.Message}", "FILE_SYSTEM_ACCESS_DENIED")),
                SecurityException secEx => (StatusCodes.Status403Forbidden, ApiResponse.Error($"Security violation: {secEx.Message}", "SECURITY_VIOLATION")),
                AuthenticationException authEx => (StatusCodes.Status401Unauthorized, ApiResponse.Error($"Authentication failed: {authEx.Message}", "AUTHENTICATION_FAILED")),

                OutOfMemoryException => (StatusCodes.Status507InsufficientStorage, ApiResponse.Error("Server is out of memory", "OUT_OF_MEMORY")),
                InvalidDataException dataEx => (StatusCodes.Status422UnprocessableEntity, ApiResponse.Error($"Invalid data format: {dataEx.Message}", "INVALID_DATA_FORMAT")),


                LockRecursionException => (StatusCodes.Status409Conflict, ApiResponse.Error("Resource is locked", "RESOURCE_LOCKED")),

                JsonException jsonEx => (StatusCodes.Status400BadRequest, ApiResponse.Error($"JSON parsing error: {jsonEx.Message}", "JSON_PARSE_ERROR")),

                MySqlException mSqlEx => (StatusCodes.Status500InternalServerError, ApiResponse.Error($"Database error occurred: {mSqlEx.Message}", "DATABASE_ERROR")),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, ApiResponse.Error("Concurrency conflict occurred", "CONCURRENCY_CONFLICT")),
                DbUpdateException dbEx => (StatusCodes.Status409Conflict, ApiResponse.Error($"Database update conflict: {dbEx.Message}", "DATABASE_UPDATE_CONFLICT")),

                TimeoutException => (StatusCodes.Status408RequestTimeout, ApiResponse.Error("Request timeout", "TIMEOUT_ERROR")),
                NotImplementedException => (StatusCodes.Status501NotImplemented, ApiResponse.Error("Feature not implemented", "NOT_IMPLEMENTED")),
                PlatformNotSupportedException => (StatusCodes.Status501NotImplemented, ApiResponse.Error("Platform not supported", "PLATFORM_NOT_SUPPORTED")),

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
