using CloudCore.Domain.Entities;
using System.Security.Claims;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using CloudCore.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using CloudCore.Common.Errors;
using Microsoft.AspNetCore.Authorization;

namespace CloudCore.Services.Implementations
{
    public class UserAuthorizationFilter : IAsyncActionFilter
    {
        private readonly IValidationService _validationService;
        private readonly ILogger<UserAuthorizationFilter> _logger;

        public UserAuthorizationFilter(IValidationService validationService, ILogger<UserAuthorizationFilter> logger)
        {
            _validationService = validationService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                await next();
                return;
            }

            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                _logger.LogWarning("Claim 'NameIdentifier' not found or invalid in JWT token.");
                var errorResponse = ApiResponse.Error("Unauthorized access.", ErrorCodes.ACCESS_DENIED);
                context.Result = new ObjectResult(errorResponse)
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            if (!context.ActionArguments.TryGetValue("userId", out var userIdFromRoute) || !(userIdFromRoute is int targetUserId))
            {
                _logger.LogError("Action filter requires a 'userId' route parameter.");
                var errorResponse = ApiResponse.Error("Bad Request: User ID missing in the request.", ErrorCodes.BAD_REQUEST);
                context.Result = new ObjectResult(errorResponse)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
                return;
            }

            var authValidation = _validationService.ValidateUserAuthorization(currentUserId, targetUserId);
            if (!authValidation.IsValid)
            {
                _logger.LogWarning("User authorization failed. CurrentUserId={CurrentUserId}, TargetUserId={TargetUserId}",
                    currentUserId, targetUserId);
                var errorResponse = ApiResponse.Error(authValidation.ErrorMessage!, authValidation.ErrorCode);
                context.Result = new ObjectResult(errorResponse)
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
            await next();
        }
    }
}
