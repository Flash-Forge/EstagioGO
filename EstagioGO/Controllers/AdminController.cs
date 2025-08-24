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
            if (ModelState.IsValid)
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
            if (user.Email.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase))
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model)
        {
            // Verificar primeiro se o modelo é válido
            if (!ModelState.IsValid)
            {
                model.Roles = _roleManager.Roles
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToList();
                return View(model);
            }

            // Verificar se é o administrador padrão
            var userToCheck = await _userManager.FindByIdAsync(id);
            if (userToCheck != null && userToCheck.Email.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = AppConstants.DefaultAdminEditError;
                return RedirectToAction(nameof(UserManagement));
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Verificar se o email foi alterado
            bool emailAlterado = user.Email != model.Email;
            bool primeiroAcessoPendente = !user.PrimeiroAcessoConcluido;

            // Verificar se o novo email já existe para outro usuário
            if (emailAlterado)
            {
                var usuarioComEmail = await _userManager.FindByEmailAsync(model.Email);
                if (usuarioComEmail != null && usuarioComEmail.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "Este email já está em uso por outro usuário.");
                    model.Roles = _roleManager.Roles
                        .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                        .ToList();
                    return View(model);
                }
            }

            // Atualizar propriedades do usuário
            user.NomeCompleto = model.NomeCompleto;
            user.Cargo = model.Role;
            user.Ativo = model.Ativo;

            // Se o email foi alterado, atualizar o email
            if (emailAlterado)
            {
                user.Email = model.Email;
                user.UserName = model.Email;
                user.NormalizedEmail = _userManager.NormalizeEmail(model.Email);
                user.NormalizedUserName = _userManager.NormalizeName(model.Email);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Se o email foi alterado e o primeiro acesso ainda não foi concluído,
                // gerar uma nova senha temporária e enviar por email
                if (emailAlterado && primeiroAcessoPendente)
                {
                    var novaSenha = GenerateTemporaryPassword();
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, novaSenha);

                    if (passwordResult.Succeeded)
                    {
                        await SendCredentialsEmail(user, novaSenha);
                        TempData["SuccessMessage"] = $"Usuário {user.NomeCompleto} atualizado com sucesso. Uma nova senha foi enviada para o email.";
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"Usuário {user.NomeCompleto} atualizado, mas houve um erro ao redefinir a senha.";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = $"Usuário {user.NomeCompleto} atualizado com sucesso.";
                }

                // Atualizar roles - primeiro obter roles atuais
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remover todas as roles atuais
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Adicionar a nova role
                await _userManager.AddToRoleAsync(user, model.Role);

                return RedirectToAction(nameof(UserManagement));
            }

            // Se a atualização do usuário falhar
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Roles = _roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

            return View(model);
        }

        // GET: Visualizar usuário
        public async Task<IActionResult> ViewUser(string id)
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

            // Obter a role do usuário
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new UserDetailViewModel
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto,
                Email = user.Email,
                Cargo = user.Cargo,
                Role = userRoles.FirstOrDefault(),
                DataCadastro = user.DataCadastro,
                Ativo = user.Ativo,
                PrimeiroAcessoConcluido = user.PrimeiroAcessoConcluido
            };

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
            var subject = "Suas credenciais de acesso foram atualizadas";
            var message = $@"
        <h3>Suas credenciais de acesso foram atualizadas</h3>
        <p>Prezado(a) {user.NomeCompleto},</p>
        <p>Suas credenciais de acesso ao Sistema de Gestão de Estágios foram atualizadas:</p>
        <p><strong>Email:</strong> {user.Email}</p>
        <p><strong>Senha temporária:</strong> {password}</p>
        <p><strong>Importante:</strong> Você deve usar esta senha para fazer seu primeiro acesso ao sistema.</p>
        <p>Após o primeiro acesso, você será solicitado a criar uma nova senha.</p>
        <br>
        <p>Acesse o sistema em: <a href='{Url.Action("Login", "Account", new { area = "Identity" }, Request.Scheme)}'>{Url.Action("Login", "Account", new { area = "Identity" }, Request.Scheme)}</a></p>
        <br>
        <p>Atenciosamente,<br>Equipe de Gestão de Estágios</p>
    ";

            await _emailSender.SendEmailAsync(user.Email, subject, message);
        }
    }
}
