using EstagioGO.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager) : PageModel
    {
        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

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
            var userName = await userManager.GetUserNameAsync(user);
            Username = userName;

            Input = new InputModel
            {
                NewEmail = user.Email
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{userManager.GetUserId(User)}'.");
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
                var setEmailResult = await userManager.SetEmailAsync(user, Input.NewEmail);
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
                var setUserNameResult = await userManager.SetUserNameAsync(user, Input.NewEmail);
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
                user.NormalizedEmail = userManager.NormalizeEmail(Input.NewEmail);
                user.NormalizedUserName = userManager.NormalizeName(Input.NewEmail);
                await userManager.UpdateAsync(user);

                // Forçar novo login com as credenciais atualizadas
                await userManager.UpdateSecurityStampAsync(user);
            }

            StatusMessage = "Seu email foi atualizado com sucesso.";
            return RedirectToPage();
        }
    }
}