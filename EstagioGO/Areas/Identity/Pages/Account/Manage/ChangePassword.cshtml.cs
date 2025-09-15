using EstagioGO.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Areas.Identity.Pages.Account.Manage
{
    public class ChangePasswordModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Senha atual")]
            public required string OldPassword { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nova senha")]
            public required string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar nova senha")]
            [Compare("NewPassword", ErrorMessage = "A nova senha e a confirmação não coincidem.")]
            public required string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{userManager.GetUserId(User)}'.");
            }

            // Verifique se é o primeiro acesso
            if (!user.PrimeiroAcessoConcluido)
            {
                ViewData["Title"] = "Primeiro Acesso - Alterar Senha";
                ViewData["IsFirstAccess"] = true;
            }

            return Page();
        }

        // Em ChangePassword.cshtml.cs

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{userManager.GetUserId(User)}'.");
            }

            bool isDefaultAdmin = user.Email!.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase);

            // --- CORREÇÃO APLICADA AQUI ---
            // Se for o admin padrão no primeiro acesso, removemos a validação da senha antiga
            if (isDefaultAdmin && !user.PrimeiroAcessoConcluido)
            {
                ModelState.Remove("Input.OldPassword");
            }
            // --- FIM DA CORREÇÃO ---

            if (!ModelState.IsValid)
            {
                return Page();
            }

            IdentityResult changePasswordResult;

            if (isDefaultAdmin && !user.PrimeiroAcessoConcluido)
            {
                // Usa a lógica de reset para definir a nova senha sem a antiga
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                changePasswordResult = await userManager.ResetPasswordAsync(user, token, Input.NewPassword);
            }
            else
            {
                // Para todos os outros casos, usa o fluxo normal que exige a senha antiga
                changePasswordResult = await userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            }

            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            if (!user.PrimeiroAcessoConcluido)
            {
                user.PrimeiroAcessoConcluido = true;
                await userManager.UpdateAsync(user);
            }

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "Sua senha foi alterada com sucesso.";

            // Após o primeiro acesso, redireciona para a página inicial
            if (isDefaultAdmin)
            {
                return LocalRedirect("~/");
            }

            return RedirectToPage();
        }
    }
}