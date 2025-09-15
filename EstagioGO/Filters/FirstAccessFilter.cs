using EstagioGO.Constants;
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

                if (user != null && !user.PrimeiroAcessoConcluido)
                {
                    var path = context.HttpContext.Request.Path.Value ?? "";

                    // O admin padrão tem uma página de escolha, então damos a ele mais permissões
                    bool isAdminDefault = user.Email!.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase);

                    // Lista de caminhos que o usuário PODE acessar
                    var allowedPaths = new List<string>
            {
                "/Identity/Account/Logout"
            };

                    string redirectPage;

                    if (isAdminDefault)
                    {
                        // Se for o admin padrão, a página de destino é a de escolha
                        redirectPage = "/Account/Manage/DefaultAdminFirstAccess";
                        allowedPaths.Add(redirectPage);
                        // Ele também precisa acessar a página ChangePassword se decidir mudar
                        allowedPaths.Add("/Identity/Account/Manage/ChangePassword");
                    }
                    else
                    {
                        // Para os outros, a página de destino é a de redefinição forçada
                        redirectPage = "/Account/Manage/ChangePassword";
                        allowedPaths.Add(redirectPage);
                    }

                    bool isPathAllowed = allowedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));

                    if (!isPathAllowed)
                    {
                        context.Result = new RedirectToPageResult(redirectPage, new { area = "Identity" });
                        return;
                    }
                }
            }

            await next();
        }
    }
}