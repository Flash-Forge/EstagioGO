using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EstagioGO.Areas.Identity.Pages.Account.Manage
{
    public class DefaultAdminFirstAccessModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null || user.PrimeiroAcessoConcluido)
            {
                // Se não for primeiro acesso, redireciona para a home
                return LocalRedirect("~/");
            }
            return Page();
        }

        // Handler para quando o admin escolhe MANTER a senha
        public async Task<IActionResult> OnPostKeepPasswordAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user != null)
            {
                user.PrimeiroAcessoConcluido = true;
                await userManager.UpdateAsync(user);
            }
            return LocalRedirect("~/");
        }
    }
}