using EstagioGO.Constants;
using EstagioGO.Data;
using EstagioGO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailSender _emailSender;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
        }

        // Página inicial de gerenciamento de usuários
        public async Task<IActionResult> UserManagement()
        {
            var users = await _userManager.Users
                .Select(u => new UserManagementViewModel
                {
                    Id = u.Id,
                    NomeCompleto = u.NomeCompleto,
                    Email = u.Email,
                    Cargo = u.Cargo,
                    DataCadastro = u.DataCadastro,
                    Ativo = u.Ativo,
                    PrimeiroAcessoConcluido = u.PrimeiroAcessoConcluido
                })
                .ToListAsync();

            // Adicionar roles para cada usuário
            foreach (var user in users)
            {
                var appUser = await _userManager.FindByIdAsync(user.Id);
                var roles = await _userManager.GetRolesAsync(appUser);
                user.Role = roles.FirstOrDefault();
            }

            return View(users);
        }

        // GET: Criar novo usuário
        public IActionResult CreateUser()
        {
            var model = new CreateUserViewModel
            {
                Roles = _roleManager.Roles
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToList()
            };
            return View(model);
        }

        // POST: Criar novo usuário
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = _roleManager.Roles
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToList();
                return View(model);
            }

            // Verificar se email já existe
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este email já está em uso.");
                model.Roles = _roleManager.Roles
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToList();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                NomeCompleto = model.NomeCompleto,
                Cargo = model.Role, // Usando o papel como cargo
                Ativo = true,
                DataCadastro = DateTime.Now,
                PrimeiroAcessoConcluido = false // Forçar alteração de senha no primeiro acesso
            };

            // Gerar senha temporária segura
            var password = GenerateTemporaryPassword();

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Adicionar ao role selecionado
                await _userManager.AddToRoleAsync(user, model.Role);

                if (model.SendEmail)
                {
                    // Enviar email com credenciais
                    await SendCredentialsEmail(user, password);
                }

                TempData["SuccessMessage"] = $"Usuário {user.NomeCompleto} criado com sucesso.";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Roles = _roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

            return View(model);
        }

        // GET: Editar usuário
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Impedir a edição do usuário administrador padrão
            if (user.Email.Equals("admin@estagio.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Não é possível editar o usuário administrador padrão.";
                return RedirectToAction(nameof(UserManagement));
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto,
                Email = user.Email,
                Role = userRoles.FirstOrDefault(),
                Ativo = user.Ativo,
                Roles = _roleManager.Roles
                    .Select(r => new SelectListItem
                    {
                        Value = r.Name,
                        Text = r.Name,
                        Selected = userRoles.Contains(r.Name)
                    })
                    .ToList()
            };

            return View(model);
        }

        // POST: Editar usuário
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model)
        {
            // Impedir a edição do usuário administrador padrão
            var userToCheck = await _userManager.FindByIdAsync(id);
            if (userToCheck != null && userToCheck.Email.Equals("admin@estagio.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Não é possível editar o usuário administrador padrão.";
                return RedirectToAction(nameof(UserManagement));
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Roles = _roleManager.Roles
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.NomeCompleto = model.NomeCompleto;
            user.Cargo = model.Role;
            user.Ativo = model.Ativo;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Atualizar roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);

                TempData["SuccessMessage"] = $"Usuário {user.NomeCompleto} atualizado com sucesso.";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Roles = _roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

            return View(model);
        }

        // GET: Deletar usuário
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Deletar usuário
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Impedir a exclusão do usuário administrador padrão
            if (user.Email.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Não é possível excluir o usuário administrador padrão.";
                return RedirectToAction(nameof(UserManagement));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Usuário {user.NomeCompleto} deletado com sucesso.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Erro ao deletar usuário {user.NomeCompleto}.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // Gerar senha temporária segura
        private string GenerateTemporaryPassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_-+=[{]};:<>|./?";

            var random = new Random();
            var password = new char[12];

            // Garantir pelo menos um de cada tipo
            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            // Preencher o restante
            const string allChars = uppercase + lowercase + digits + special;
            for (int i = 4; i < 12; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Embaralhar
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        // GET: Visualizar administrador padrão
        public async Task<IActionResult> ViewAdmin()
        {
            var adminUser = await _userManager.FindByEmailAsync("admin@estagio.com");
            if (adminUser == null)
            {
                return NotFound();
            }

            return View(adminUser);
        }

        private async Task SendCredentialsEmail(ApplicationUser user, string password)
        {
            var subject = "Suas credenciais no Sistema de Gestão de Estágios";
            var message = $@"
                <h3>Bem-vindo ao Sistema de Gestão de Estágios</h3>
                <p>Suas credenciais de acesso foram criadas:</p>
                <p><strong>Email:</strong> {user.Email}</p>
                <p><strong>Senha temporária:</strong> {password}</p>
                <p><strong>Importante:</strong> Você será solicitado a alterar sua senha no primeiro acesso.</p>
                <br>
                <p>Acesse o sistema em: {Url.Action("Login", "Account", new { area = "Identity" }, Request.Scheme)}</p>
            ";

            await _emailSender.SendEmailAsync(user.Email, subject, message);
        }
    }
}