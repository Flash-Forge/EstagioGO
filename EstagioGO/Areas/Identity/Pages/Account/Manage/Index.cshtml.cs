using EstagioGO.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel(UserManager<ApplicationUser> userManager) : PageModel // signInManager removido
    {
        public string Username { get; set; } = string.Empty;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [EmailAddress]
            [Display(Name = "Novo email")]
            public string NewEmail { get; set; } = string.Empty;

            [Display(Name = "Confirmar email")]
            [Compare("NewEmail", ErrorMessage = "O email e a confirmação não coincidem.")]
            public string ConfirmEmail { get; set; } = string.Empty;
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await userManager.GetUserNameAsync(user);
            Username = userName ?? string.Empty; // Usar ?? para evitar nulos

            Input = new InputModel
            {
                NewEmail = user.Email ?? string.Empty // Usar ?? para evitar nulos
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

            if (Input.NewEmail != user.Email)
            {
                var setEmailResult = await userManager.SetEmailAsync(user, Input.NewEmail);
                if (!setEmailResult.Succeeded)
                {
                    // ... (código de erro)
                    return Page();
                }

                var setUserNameResult = await userManager.SetUserNameAsync(user, Input.NewEmail);
                if (!setUserNameResult.Succeeded)
                {
                    // ... (código de erro)
                    return Page();
                }

                user.NormalizedEmail = userManager.NormalizeEmail(Input.NewEmail);
                user.NormalizedUserName = userManager.NormalizeName(Input.NewEmail);
                await userManager.UpdateAsync(user);

                // O SignInManager era usado aqui, mas como estamos apenas atualizando
                // o email/username e não o password/security stamp, a atualização do cookie
                // não é estritamente necessária. Se fosse, teríamos que manter o signInManager.
                // await signInManager.RefreshSignInAsync(user);
            }

            StatusMessage = "Seu perfil foi atualizado";
            return RedirectToPage();
        }
    }
}