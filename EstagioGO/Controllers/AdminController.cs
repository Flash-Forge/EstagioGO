using EstagioGO.Constants;
using EstagioGO.Data;
using EstagioGO.Models;
using EstagioGO.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Administrador,Coordenador")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailSender emailSender,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> UserManagement()
        {
            var usersQuery = _userManager.Users.AsQueryable();

            // Se for coordenador, filtrar apenas usuários com role Estagiario
            if (User.IsInRole("Coordenador") && !User.IsInRole("Administrador"))
            {
                // Obter IDs dos usuários com role Estagiario
                var estagiarios = await _userManager.GetUsersInRoleAsync("Estagiario");
                var estagiariosIds = estagiarios.Select(e => e.Id).ToList();

                // Filtrar usando Contains que é traduzível para SQL
                usersQuery = usersQuery.Where(u => estagiariosIds.Contains(u.Id));
            }

            var users = await usersQuery
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
        public IActionResult CreateUser(string contexto)
        {
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");

            var roles = _roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

            // Coordenador só pode criar estagiários
            if (isCoordenador)
            {
                roles = roles.Where(r => r.Value == "Estagiario").ToList();
            }

            var model = new CreateUserViewModel
            {
                Roles = roles
            };

            // Pré-selecionar a role com base no contexto
            if (!string.IsNullOrEmpty(contexto))
            {
                if (contexto == "supervisor")
                    model.Role = "Supervisor";
                else if (contexto == "estagiario")
                    model.Role = "Estagiario";
            }
            // Se for coordenador e não tiver contexto, forçar Estagiario
            else if (isCoordenador)
            {
                model.Role = "Estagiario";
            }

            ViewBag.Contexto = contexto;
            ViewBag.IsCoordenador = isCoordenador;
            return View(model);
        }

        // POST: Criar novo usuário
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            // Remover a validação da propriedade Roles do ModelState
            ModelState.Remove("Roles");

            // Adicionar logging para diagnóstico
            _logger.LogInformation("Tentativa de criação de usuário por: {UserName}", User.Identity.Name);
            _logger.LogInformation("Dados do modelo: {Email}, {Role}", model.Email, model.Role);

            // Verificar se é coordenador e validar permissões
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador && model.Role != "Estagiario")
            {
                ModelState.AddModelError("Role", "Coordenadores só podem criar usuários com perfil de Estagiário.");
                _logger.LogWarning("Tentativa de criar usuário com role não permitida: {Role}", model.Role);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState inválido: {Errors}",
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));

                // Preencher as roles disponíveis para o usuário atual
                model.Roles = await GetRolesForCurrentUser();
                return View(model);
            }

            // Verificar se email já existe
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este email já está em uso.");
                _logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", model.Email);

                // Preencher as roles disponíveis para o usuário atual
                model.Roles = await GetRolesForCurrentUser();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                NomeCompleto = model.NomeCompleto,
                Cargo = model.Role,
                Ativo = true,
                DataCadastro = DateTime.Now,
                PrimeiroAcessoConcluido = false
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
                _logger.LogInformation("Usuário criado com sucesso: {Email}, Role: {Role}", user.Email, model.Role);

                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                _logger.LogError("Erro ao criar usuário: {Error}", error.Description);
            }

            // Preencher as roles disponíveis para o usuário atual
            model.Roles = await GetRolesForCurrentUser();
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

            // Coordenador só pode editar estagiários
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Estagiario"))
                {
                    TempData["ErrorMessage"] = "Coordenadores só podem editar usuários com perfil de Estagiário.";
                    return RedirectToAction(nameof(UserManagement));
                }
            }

            var userRolesList = await _userManager.GetRolesAsync(user);
            var roles = _roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name,
                    Selected = userRolesList.Contains(r.Name)
                })
                .ToList();

            // Coordenador só pode ver role de Estagiario
            if (isCoordenador)
            {
                roles = roles.Where(r => r.Value == "Estagiario").ToList();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto,
                Email = user.Email,
                Role = userRolesList.FirstOrDefault(),
                Ativo = user.Ativo,
                Roles = roles
            };

            ViewBag.IsCoordenador = isCoordenador;
            return View(model);
        }


        // POST: Editar usuário
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model)
        {
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");

            // Coordenador só pode editar estagiários e manter como estagiários
            if (isCoordenador)
            {
                if (model.Role != "Estagiario")
                {
                    ModelState.AddModelError("Role", "Coordenadores só podem manter o perfil de Estagiário.");
                }

                // Verificar se o usuário sendo editado é um estagiário
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (!userRoles.Contains("Estagiario"))
                    {
                        TempData["ErrorMessage"] = "Coordenadores só podem editar usuários com perfil de Estagiário.";
                        return RedirectToAction(nameof(UserManagement));
                    }
                }
            }

            // Preencher Roles antes de qualquer verificação
            model.Roles = _roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name,
                    Selected = r.Name == model.Role
                })
                .ToList();

            if (isCoordenador)
            {
                model.Roles = model.Roles.Where(r => r.Value == "Estagiario").ToList();
            }

            // Remover a validação da propriedade Roles do ModelState
            ModelState.Remove("Roles");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            var userToEdit = await _userManager.FindByIdAsync(id);
            if (userToEdit == null)
            {
                return NotFound();
            }

            // Verificar se é o administrador padrão
            if (userToEdit.Email.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = AppConstants.DefaultAdminEditError;
                return RedirectToAction(nameof(UserManagement));
            }

            // Verificar se o email foi alterado
            bool emailAlterado = userToEdit.Email != model.Email;
            bool primeiroAcessoPendente = !userToEdit.PrimeiroAcessoConcluido;

            // Verificar se o novo email já existe para outro usuário
            if (emailAlterado)
            {
                var usuarioComEmail = await _userManager.FindByEmailAsync(model.Email);
                if (usuarioComEmail != null && usuarioComEmail.Id != userToEdit.Id)
                {
                    ModelState.AddModelError("Email", "Este email já está em uso por outro usuário.");
                    return View(model);
                }
            }

            // Atualizar propriedades do usuário
            userToEdit.NomeCompleto = model.NomeCompleto;
            userToEdit.Cargo = model.Role;
            userToEdit.Ativo = model.Ativo;

            // Se o email foi alterado, atualizar o email
            if (emailAlterado)
            {
                userToEdit.Email = model.Email;
                userToEdit.UserName = model.Email;
                userToEdit.NormalizedEmail = _userManager.NormalizeEmail(model.Email);
                userToEdit.NormalizedUserName = _userManager.NormalizeName(model.Email);
            }

            var result = await _userManager.UpdateAsync(userToEdit);
            if (result.Succeeded)
            {
                // Atualizar roles
                var currentRoles = await _userManager.GetRolesAsync(userToEdit);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(userToEdit, currentRoles);
                }
                await _userManager.AddToRoleAsync(userToEdit, model.Role);

                // Enviar nova senha apenas se o email foi alterado E primeiro acesso pendente
                if (emailAlterado && primeiroAcessoPendente)
                {
                    var novaSenha = GenerateTemporaryPassword();
                    var token = await _userManager.GeneratePasswordResetTokenAsync(userToEdit);
                    var passwordResult = await _userManager.ResetPasswordAsync(userToEdit, token, novaSenha);

                    if (passwordResult.Succeeded)
                    {
                        await SendCredentialsEmail(userToEdit, novaSenha);
                        TempData["SuccessMessage"] = $"Usuário {userToEdit.NomeCompleto} atualizado com sucesso. Uma nova senha foi enviada para o email.";
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"Usuário {userToEdit.NomeCompleto} atualizado, mas houve um erro ao redefinir a senha.";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = $"Usuário {userToEdit.NomeCompleto} atualizado com sucesso.";
                }

                return RedirectToAction(nameof(UserManagement));
            }

            // Se a atualização do usuário falhar
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

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

            // Coordenador só pode visualizar estagiários
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Estagiario"))
                {
                    TempData["ErrorMessage"] = "Coordenadores só podem visualizar usuários com perfil de Estagiário.";
                    return RedirectToAction(nameof(UserManagement));
                }
            }

            // Obter a role do usuário
            var userRolesList = await _userManager.GetRolesAsync(user);

            var model = new UserDetailViewModel
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto,
                Email = user.Email,
                Cargo = user.Cargo,
                Role = userRolesList.FirstOrDefault(),
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

            // Coordenador não pode deletar usuários
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                TempData["ErrorMessage"] = "Coordenadores não têm permissão para excluir usuários.";
                return RedirectToAction(nameof(UserManagement));
            }

            return View(user);
        }

        // POST: Deletar usuário
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            // Coordenador não pode deletar usuários
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                TempData["ErrorMessage"] = "Coordenadores não têm permissão para excluir usuários.";
                return RedirectToAction(nameof(UserManagement));
            }

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

        private async Task<List<SelectListItem>> GetRolesForCurrentUser()
        {
            var roles = _roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

            // Se for coordenador, filtrar apenas a role Estagiario
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                roles = roles.Where(r => r.Value == "Estagiario").ToList();
            }

            return roles;
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
