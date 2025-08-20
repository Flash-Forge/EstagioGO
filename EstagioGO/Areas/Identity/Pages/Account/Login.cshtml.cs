// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using EstagioGO.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EstagioGO.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

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

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Adicione estes logs para diagnóstico detalhado
                _logger.LogInformation("Tentativa de login para: {Email}", Input.Email);
                _logger.LogInformation("Configuração RequireConfirmedAccount: {RequireConfirmedAccount}",
                    _signInManager.Options.SignIn.RequireConfirmedAccount);

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    _logger.LogInformation("Usuário encontrado. ID: {UserId}", user.Id);
                    _logger.LogInformation("Email confirmado: {EmailConfirmed}", user.EmailConfirmed);
                    _logger.LogInformation("Conta bloqueada: {LockedOut}", await _userManager.IsLockedOutAsync(user));
                    _logger.LogInformation("Tem senha: {HasPassword}", await _userManager.HasPasswordAsync(user));

                    // Verifique se a senha está correta
                    var passwordCheck = await _userManager.CheckPasswordAsync(user, Input.Password);
                    _logger.LogInformation("Senha correta: {PasswordCorrect}", passwordCheck);
                }
                else
                {
                    _logger.LogWarning("Usuário não encontrado para o email: {Email}", Input.Email);
                }

                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Login bem-sucedido para {Email}", Input.Email);

                    // Verificar se é o primeiro acesso
                    var currentUser = await _userManager.FindByEmailAsync(Input.Email);
                    if (currentUser != null && !currentUser.PrimeiroAcessoConcluido)
                    {
                        _logger.LogInformation("Usuário precisa alterar a senha padrão");

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
                    _logger.LogInformation("Login requer autenticação de dois fatores para {Email}", Input.Email);
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Conta bloqueada para {Email}", Input.Email);
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    _logger.LogWarning("Falha no login para {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}