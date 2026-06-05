using Microsoft.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

public static class OpenApiSecurityExtensions
{
    /// <summary>
    /// Registers OpenAPI document generation with an integrated global JWT Bearer security scheme layer.
    /// </summary>
    public static IServiceCollection AddOpenApiWithSecurity(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                // 1. Initialize component boundaries safely using the exact interface types
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                // 2. Define the architectural layout for your JWT Authentication configuration
                var bearerScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your valid JWT token payload to authenticate against secure tracking gateways."
                };

                // Insert the schema object safely into the matching collection interface
                document.Components.SecuritySchemes["Bearer"] = bearerScheme;

                // 3. Bind the security definition globally to the document using the proper .NET 10 types
                var securityRequirement = new OpenApiSecurityRequirement
                {
                    // Uses the explicit .NET 10 scheme reference class and maps to a concrete List<string>
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                };

                // Fixes 'SecurityRequirements' by routing to the correct collection name: 'Security'
                document.Security ??= new List<OpenApiSecurityRequirement>();
                document.Security.Add(securityRequirement);

                return Task.CompletedTask;
            });
        });

        return services;
    }
}