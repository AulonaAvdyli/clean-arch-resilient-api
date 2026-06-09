using BackendAssignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace BackendAssignment.Api.Authorization;

// Authorization handler that ensures the request is coming from a Greek IP
public class FromGreeceOnlyHandler(
    IHttpContextAccessor httpContextAccessor,
    IIpLocationService ipLocationService)
    : AuthorizationHandler<FromGreeceOnlyRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FromGreeceOnlyRequirement requirement)
    {
        // Retrieve IP address from the incoming HTTP request
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        // If IP is not available, deny access
        if (string.IsNullOrWhiteSpace(ip))
        {
            context.Fail();
            httpContextAccessor.HttpContext.Items["AuthorizationFailureReason"] = "IP address not available.";
            return;
        }

        // Look up country code based on IP using external service
        var countryCode = await ipLocationService.GetCountryCodeAsync(ip);

        // Deny access if the IP is not from Greece (GR)
        if (string.IsNullOrWhiteSpace(countryCode) || !countryCode.Equals("GR", StringComparison.OrdinalIgnoreCase))
        {
            context.Fail();
            httpContextAccessor.HttpContext.Items["AuthorizationFailureReason"] =
                "Access denied: this operation is only allowed for users located in Greece.";
            return;
        }

        // Mark requirement as successfully fulfilled
        context.Succeed(requirement);
    }
}