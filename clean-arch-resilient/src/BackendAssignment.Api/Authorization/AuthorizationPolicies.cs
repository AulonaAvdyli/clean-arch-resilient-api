using Microsoft.AspNetCore.Authorization;

namespace BackendAssignment.Api.Authorization;

public static class AuthorizationPolicies
{
    public static IServiceCollection AddCustomAuthorizationPolicies(this IServiceCollection services)
    {
        // Register custom authorization policy that restricts access to users from Greece
        services.AddAuthorization(options =>
        {
            options.AddPolicy("FromGreeceOnly", policy =>
                policy.Requirements.Add(new FromGreeceOnlyRequirement()));
        });

        // Add the handler that evaluates the custom policy
        services.AddSingleton<IAuthorizationHandler, FromGreeceOnlyHandler>();

        return services;
    }
}

