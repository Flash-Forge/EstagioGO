using EstagioGO.Data;
using EstagioGO.Models.Estagio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Areas.Identity.Pages.Account.Manage
{
    [Authorize(Roles = "Estagiario")]
    public class EstagiarioProfileModel(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context) : PageModel
    {
        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public Estagiario? Estagiario { get; set; } // Declarado como anulável

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{userManager.GetUserId(User)}'.");
            }

            Estagiario = await context.Estagiarios
                .IgnoreQueryFilters()
                .Include(e => e.Supervisor)
                .Include(e => e.Frequencias)
                .Include(e => e.Avaliacoes)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (Estagiario == null)
            {
                StatusMessage = "Erro: Dados de estagiário não encontrados. Verifique se o perfil está ativo e corretamente associado ao seu usuário.";
                return Page();
            }

            return Page();
        }
    }
}