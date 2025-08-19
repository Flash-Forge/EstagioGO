using EstagioGO.Data;
using EstagioGO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public IActionResult CreateUser()
    {
        var model = new CreateUserViewModel
        {
            Roles = _roleManager.Roles
                .Where(r => r.Name != "Administrador") // Administrador não pode criar outros administradores
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            NomeCompleto = model.NomeCompleto,
            Ativo = true,
            DataCadastro = DateTime.Now,
            PrimeiroAcessoConcluido = false
        };

        // Gerar senha temporária segura
        var password = GenerateTemporaryPassword();

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);

            // Enviar email com credenciais
            await SendCredentialsEmail(user, password);

            TempData["Success"] = "Usuário criado com sucesso. As credenciais foram enviadas por email.";
            return RedirectToAction("UserManagement");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        model.Roles = _roleManager.Roles
            .Where(r => r.Name != "Administrador")
            .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
            .ToList();

        return View(model);
    }

    private string GenerateTemporaryPassword()
    {
        // Implementação para gerar senha temporária segura
        // Exemplo simples (não use em produção sem melhorias):
        return $"Temp#{Guid.NewGuid().ToString().Substring(0, 6)}!";
    }

    private async Task SendCredentialsEmail(ApplicationUser user, string password)
    {
        // Implementação do envio de email
        var emailSender = HttpContext.RequestServices.GetRequiredService<IEmailSender>();
        await emailSender.SendEmailAsync(
            user.Email,
            "Suas credenciais no Sistema de Gestão de Estágios",
            $"<p>Seu login: {user.Email}</p><p>Sua senha temporária: {password}</p>" +
            "<p>Após o primeiro acesso, você será solicitado a alterar sua senha.</p>"
        );
    }
}