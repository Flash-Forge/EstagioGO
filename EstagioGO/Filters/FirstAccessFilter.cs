using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EstagioGO.Filters
{
    public class FirstAccessFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // O filtro só deve rodar se o endpoint for uma página ou controller, ignorando arquivos estáticos
            if (context.ActionDescriptor is not PageActionDescriptor && context.ActionDescriptor is not ControllerActionDescriptor)
            {
                await next();
                return;
            }

            // O filtro só deve rodar para usuários autenticados
            if (context.HttpContext.User.Identity?.IsAuthenticated ?? false)
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.GetUserAsync(context.HttpContext.User);

                // Se o usuário precisa concluir o primeiro acesso
                if (user != null && !user.PrimeiroAcessoConcluido)
                {
                    var path = context.HttpContext.Request.Path.Value ?? "";

                    // Lista de caminhos que o usuário PODE acessar
                    var allowedPaths = new[]
                    {
                        "/Identity/Account/Manage/ChangePassword",
                        "/Identity/Account/Logout"
                    };

                    // Verifica se o caminho atual está na lista de permissões
                    bool isPathAllowed = allowedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));

                    // Se o caminho NÃO for permitido, redireciona
                    if (!isPathAllowed)
                    {
                        // Usar RedirectToPage com a área especificada é mais seguro
                        context.Result = new RedirectToPageResult("/Account/Manage/ChangePassword", new { area = "Identity" });
                        return; // Impede a execução da action original
                    }
                }
            }

            // Se tudo estiver OK, continua para a action solicitada
            await next();
        }
    }
}