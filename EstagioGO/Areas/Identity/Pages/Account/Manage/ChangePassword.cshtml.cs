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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Verifica se é o primeiro acesso
            bool isFirstAccess = !user.PrimeiroAcessoConcluido;

            // Marcar o primeiro acesso como concluído
            if (isFirstAccess)
            {
                user.PrimeiroAcessoConcluido = true;
                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }

            await signInManager.RefreshSignInAsync(user);

            StatusMessage = "Sua senha foi alterada com sucesso.";

            // Redirecionar para a página inicial após o primeiro acesso
            if (isFirstAccess)
            {
                return LocalRedirect("~/");
            }

            return RedirectToPage();
        }
    }
}