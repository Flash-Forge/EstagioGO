// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Areas.Identity.Pages.Account
{
    public class LoginModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LoginModel> logger,
        UserManager<ApplicationUser> userManager) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = [.. (await signInManager.GetExternalAuthenticationSchemesAsync())];

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = [.. (await signInManager.GetExternalAuthenticationSchemesAsync())];

            if (ModelState.IsValid)
            {
                // Adicione estes logs para diagnóstico detalhado
                logger.LogInformation("Tentativa de login para: {Email}", Input.Email);
                logger.LogInformation("Configuração RequireConfirmedAccount: {RequireConfirmedAccount}",
                    signInManager.Options.SignIn.RequireConfirmedAccount);

                var user = await userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    logger.LogInformation("Usuário encontrado. ID: {UserId}", user.Id);
                    logger.LogInformation("Email confirmado: {EmailConfirmed}", user.EmailConfirmed);
                    logger.LogInformation("Conta bloqueada: {LockedOut}", await userManager.IsLockedOutAsync(user));
                    logger.LogInformation("Tem senha: {HasPassword}", await userManager.HasPasswordAsync(user));

                    // Verifique se a senha está correta
                    var passwordCheck = await userManager.CheckPasswordAsync(user, Input.Password);
                    logger.LogInformation("Senha correta: {PasswordCorrect}", passwordCheck);
                }
                else
                {
                    logger.LogWarning("Usuário não encontrado para o email: {Email}", Input.Email);
                }

                var result = await signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    logger.LogInformation("Login bem-sucedido para {Email}", Input.Email);

                    // Verificar se é o primeiro acesso
                    var currentUser = await userManager.FindByEmailAsync(Input.Email);
                    if (currentUser != null && !currentUser.PrimeiroAcessoConcluido)
                    {
                        logger.LogInformation("Usuário precisa alterar a senha padrão");

                        // Marcar que o usuário precisa mudar a senha
                        HttpContext.Response.Cookies.Append("NeedsPasswordChange", "true", new CookieOptions
                        {
                            Expires = DateTimeOffset.Now.AddMinutes(5),
                            HttpOnly = true,
                            Secure = HttpContext.Request.IsHttps
                        });

                        // Redirecionar para alteração de senha
                        return LocalRedirect("/Identity/Account/Manage/ChangePassword?forceChange=true");
                    }

                    return LocalRedirect(returnUrl ?? Url.Content("~/"));
                }

                if (result.RequiresTwoFactor)
                {
                    logger.LogInformation("Login requer autenticação de dois fatores para {Email}", Input.Email);
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    logger.LogWarning("Conta bloqueada para {Email}", Input.Email);
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    logger.LogWarning("Falha no login para {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}