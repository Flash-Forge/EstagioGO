using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace EstagioGO.Middleware
{
    public class ForcePasswordChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public ForcePasswordChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                // Verificar se o usuário precisa mudar a senha
                var userId = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Se for a página de alteração de senha, permitir
                    if (context.Request.Path.ToString().Contains("/Identity/Account/ChangePassword"))
                    {
                        await _next(context);
                        return;
                    }

                    // Se não for a página de alteração de senha mas o usuário precisa mudar
                    if (context.Session.GetString("NeedsPasswordChange") == "true" ||
                        context.Request.Cookies["NeedsPasswordChange"] == "true")
                    {
                        context.Response.Redirect("/Identity/Account/ChangePassword?forceChange=true");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    // Extensão para facilitar o uso
    public static class ForcePasswordChangeMiddlewareExtensions
    {
        public static IApplicationBuilder UseForcePasswordChangeMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ForcePasswordChangeMiddleware>();
        }
    }
}