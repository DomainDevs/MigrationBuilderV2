using Infrastructure.ErrorHandling.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Infrastructure.ErrorHandling;

public static class AddMiddlewareExtensions
{
    /// <summary>
    /// Agrega el middleware global de manejo de errores.
    /// </summary>
    public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlerMiddleware>();
    }
}
