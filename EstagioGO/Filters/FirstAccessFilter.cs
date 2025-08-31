using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EstagioGO.Filters
{
    public class FirstAccessFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userManager = context.HttpContext.RequestServices.GetService<UserManager<ApplicationUser>>();
            var signInManager = context.HttpContext.RequestServices.GetService<SignInManager<ApplicationUser>>();

            if (signInManager.IsSignedIn(context.HttpContext.User))
            {
                var user = await userManager.GetUserAsync(context.HttpContext.User);

                if (user != null && user.PrimeiroAcessoConcluido == false)
                {
                    var path = context.HttpContext.Request.Path.Value;

                    // Permitir acesso à página de alteração de senha e recursos estáticos
                    if (path.Contains("/Account/Manage/ChangePassword") &&
                        path.Contains("/Identity/Account/Manage/ChangePassword") &&
                        path.Contains("/Account/Logout") &&
                        path.Contains("/Identity/Account/Logout") &&
                        path.StartsWith("/_") &&
                        path.StartsWith("/lib/") &&
                        path.StartsWith("/css/") &&
                        path.StartsWith("/js/") &&
                        path.StartsWith("/images/"))
                    {
                        context.Result = new RedirectToPageResult("/Home/Index");
                        context.Result = new RedirectToPageResult("/Account/Manage/ChangePassword");
                        return;
                    }
                }
            }

            await next();
        }
    }
}