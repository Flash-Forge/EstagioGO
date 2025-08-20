using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

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

                if (user != null && !user.PrimeiroAcessoConcluido &&
                    !context.HttpContext.Request.Path.Value.Contains("/Manage/ChangePassword") &&
                    !context.HttpContext.Request.Path.Value.Contains("/Account/Logout") &&
                    !context.HttpContext.Request.Path.Value.Contains("/Identity/Account/Logout"))
                {
                    context.Result = new RedirectToPageResult("/Account/Manage/ChangePassword");
                    return;
                }
            }

            await next();
        }
    }
}