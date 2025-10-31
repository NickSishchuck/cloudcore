using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CloudCore.Services.Implementations;
using CloudCore.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Routing;
using CloudCore.Common.Errors;
using Xunit;
using CloudCore.Common.Validation;

namespace CloudCore.Tests
{
    public class UserAuthorizationFilterTests
    {
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<ILogger<UserAuthorizationFilter>> _mockLogger;
        private readonly UserAuthorizationFilter _service;

        public UserAuthorizationFilterTests()
        {
            _mockValidationService = new Mock<IValidationService>();
            _mockLogger = new Mock<ILogger<UserAuthorizationFilter>>();
            _service = new UserAuthorizationFilter(_mockValidationService.Object, _mockLogger.Object);
        }

        private ActionExecutingContext CreateContext(ClaimsPrincipal user, object userIdRouteValue, bool allowAnonymous = false)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = user;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var actionArguments = new System.Collections.Generic.Dictionary<string, object>();
            if (userIdRouteValue != null)
                actionArguments["userId"] = userIdRouteValue;

            var endpointMetadata = allowAnonymous ? new object[] { new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute() } : new object[0];
            var endpoint = new Endpoint(
                (c) => Task.CompletedTask,
                new EndpointMetadataCollection(endpointMetadata),
                "test");

            httpContext.SetEndpoint(endpoint);

            return new ActionExecutingContext(actionContext, new System.Collections.Generic.List<IFilterMetadata>(), actionArguments, controller: null);
        }

        [Fact]
        public async Task OnActionExecutionAsync_AllowAnonymous_CallsNext()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var context = CreateContext(user, null, allowAnonymous: true);

            var wasNextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                wasNextCalled = true;
                return Task.FromResult<ActionExecutedContext>(null);
            };

            await _service.OnActionExecutionAsync(context, next);

            Assert.True(wasNextCalled);
        }

        [Fact]
        public async Task OnActionExecutionAsync_MissingUserIdClaim_Returns401()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity()); // No claims
            var context = CreateContext(user, userIdRouteValue: 1);

            var next = new ActionExecutionDelegate(() => Task.FromResult<ActionExecutedContext>(null));

            await _service.OnActionExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var objResult = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objResult.StatusCode);
        }

        [Fact]
        public async Task OnActionExecutionAsync_MissingUserIdRouteParameter_Returns400()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);
            var context = CreateContext(user, userIdRouteValue: null);

            var next = new ActionExecutionDelegate(() => Task.FromResult<ActionExecutedContext>(null));

            await _service.OnActionExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task OnActionExecutionAsync_AuthorizationFails_Returns403()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);
            var context = CreateContext(user, userIdRouteValue: 2);

            _mockValidationService.Setup(x => x.ValidateUserAuthorization(1, 2))
                .Returns(ValidationResult.Failure("Not authorized", ErrorCodes.ACCESS_DENIED));

            var next = new ActionExecutionDelegate(() => Task.FromResult<ActionExecutedContext>(null));

            await _service.OnActionExecutionAsync(context, next);

            Assert.NotNull(context.Result);
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task OnActionExecutionAsync_AuthorizationSucceeds_CallsNext()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);
            var context = CreateContext(user, userIdRouteValue: 1);

            _mockValidationService.Setup(x => x.ValidateUserAuthorization(1, 1))
                .Returns(ValidationResult.Success());

            var nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult<ActionExecutedContext>(null);
            };

            await _service.OnActionExecutionAsync(context, next);

            Assert.True(nextCalled);
        }
    }
}
