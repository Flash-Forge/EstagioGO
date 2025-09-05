using EstagioGO.Constants;
using EstagioGO.Data;
using EstagioGO.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Areas.Identity.Pages.Account.Manage
{
    public class EstagiarioProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public EstagiarioProfileModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public Estagiario Estagiario { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [EmailAddress]
            [Display(Name = "Novo email")]
            public string NewEmail { get; set; }

            [Display(Name = "Confirmar email")]
            [Compare("NewEmail", ErrorMessage = "O email e a confirmação não coincidem.")]
            public string ConfirmEmail { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            // Buscar dados do estagiário vinculado ao usuário
            Estagiario = await _context.Estagiarios
                .Include(e => e.Supervisor)
                .Include(e => e.Frequencias)
                .Include(e => e.Avaliacoes)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            Input = new InputModel
            {
                NewEmail = user.Email
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);

            if (Estagiario == null)
            {
                return NotFound("Dados de estagiário não encontrados para este usuário.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            // Verificar se é o administrador padrão
            if (user.Email == AppConstants.DefaultAdminEmail)
            {
                StatusMessage = "Error: " + AppConstants.DefaultAdminEditError;
                return RedirectToPage();
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Atualizar apenas o email se foi alterado
            if (Input.NewEmail != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.NewEmail);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }

                // Atualizar também o nome de usuário
                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.NewEmail);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }

                // Atualizar o NormalizedEmail e NormalizedUserName também
                user.NormalizedEmail = _userManager.NormalizeEmail(Input.NewEmail);
                user.NormalizedUserName = _userManager.NormalizeName(Input.NewEmail);
                await _userManager.UpdateAsync(user);

                // Forçar novo login com as credenciais atualizadas
                await _userManager.UpdateSecurityStampAsync(user);
            }

            StatusMessage = "Seu email foi atualizado com sucesso.";
            return RedirectToPage();
        }
    }
}