using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Documentation.Filters;

public sealed class SwaggerSecurityRequirement : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return;

        bool allowAnonymous =
            descriptor.MethodInfo.GetCustomAttributes(true).Any(a => a is AllowAnonymousAttribute) ||
            descriptor.ControllerTypeInfo.GetCustomAttributes(true).Any(a => a is AllowAnonymousAttribute);

        if (allowAnonymous)
            return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    }
                ] = System.Array.Empty<string>()
            }
        ];
    }
}
