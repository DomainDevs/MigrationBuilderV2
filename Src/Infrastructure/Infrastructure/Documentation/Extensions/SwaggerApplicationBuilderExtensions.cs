using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Documentation.Extensions;

public static class SwaggerApplicationBuilderExtensions
{
    public static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Migration API v1"));
        return app;
    }
}
