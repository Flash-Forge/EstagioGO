using EstagioGO.Constants;
using EstagioGO.Data;
using EstagioGO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Administrador,Coordenador")]
    public class AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IEmailSender emailSender,
        ILogger<AdminController> logger) : Controller
    {
        // GET: Admin/UserManagement
        public async Task<IActionResult> UserManagement()
        {
            // Usamos IQueryable para construir a consulta sem executar no banco ainda
            IQueryable<ApplicationUser> usersQuery = userManager.Users;

            // Se for coordenador, pré-filtrar apenas usuários com a role Estagiario
            if (User.IsInRole("Coordenador") && !User.IsInRole("Administrador"))
            {
                var estagiarios = await userManager.GetUsersInRoleAsync("Estagiario");
                var estagiariosIds = estagiarios.Select(e => e.Id).ToList();
                usersQuery = usersQuery.Where(u => estagiariosIds.Contains(u.Id));
            }

            // Agora, buscamos os usuários
            var usersWithRoles = await (from user in usersQuery
                                        from userRole in user.UserRoles
                                        join role in roleManager.Roles on userRole.RoleId equals role.Id
                                        select new UserManagementViewModel
                                        {
                                            Id = user.Id,
                                            NomeCompleto = user.NomeCompleto ?? string.Empty,
                                            Email = user.Email ?? string.Empty,
                                            Cargo = user.Cargo ?? string.Empty,
                                            DataCadastro = user.DataCadastro,
                                            Ativo = user.Ativo,
                                            PrimeiroAcessoConcluido = user.PrimeiroAcessoConcluido,
                                            Role = role.Name ?? string.Empty
                                        }).ToListAsync();

            return View(usersWithRoles);
        }

        // GET: Criar novo usuário
        public async Task<IActionResult> CreateUser(string? contexto)
        {
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            var roles = await GetRolesForCurrentUser();

            var model = new CreateUserViewModel
            {
                NomeCompleto = "",
                Email = "",
                Role = isCoordenador ? "Estagiario" : "Estagiario",
                Roles = roles
            };

            if (!string.IsNullOrEmpty(contexto))
            {
                if (contexto == "supervisor" && !isCoordenador)
                    model.Role = "Supervisor";
                else if (contexto == "estagiario")
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
            ModelState.Remove("Roles");

            logger.LogInformation("Tentativa de criação de usuário por: {UserName}", User.Identity?.Name);
            logger.LogInformation("Dados do modelo: {Email}, {Role}", model.Email, model.Role);

            // Verificar se é coordenador e validar permissões
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador && model.Role != "Estagiario")
            {
                ModelState.AddModelError("Role", "Coordenadores só podem criar usuários com perfil de Estagiário.");
                logger.LogWarning("Tentativa de criar usuário com role não permitida: {Role}", model.Role);
            }

            if (!ModelState.IsValid)
            {
                logger.LogWarning("ModelState inválido: {Errors}",
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));

                model.Roles = await GetRolesForCurrentUser();
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError("Email", "Email é obrigatório.");
                model.Roles = await GetRolesForCurrentUser();
                return View(model);
            }

            model.Email = model.Email.ToLowerInvariant();

            // Verificar se email já existe
            var existingUser = await userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este email já está em uso.");
                logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", model.Email);

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

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, model.Role);

                if (model.SendEmail)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code = token },
                        protocol: Request.Scheme);

                    await SendFirstAccessEmail(user, callbackUrl!);
                }

                TempData["SuccessMessage"] = $"Usuário {user.NomeCompleto} criado com sucesso.";
                logger.LogInformation("Usuário criado com sucesso: {Email}, Role: {Role}", user.Email, model.Role);

                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                logger.LogError("Erro ao criar usuário: {Error}", error.Description);
            }

            model.Roles = await GetRolesForCurrentUser();
            return View(model);
        }

        // GET: Editar usuário
        public async Task<IActionResult> EditUser(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Validações de permissão
            if (IsDefaultAdministrator(user))
            {
                TempData["ErrorMessage"] = AppConstants.DefaultAdminEditError;
                return RedirectToAction(nameof(UserManagement));
            }

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser != null && !IsDefaultAdministrator(currentUser) && await IsUserAdministrator(user))
            {
                TempData["ErrorMessage"] = AppConstants.RegularAdminEditError;
                return RedirectToAction(nameof(UserManagement));
            }

            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                var userRoles = await userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Estagiario"))
                {
                    TempData["ErrorMessage"] = "Coordenadores só podem editar usuários com perfil de Estagiário.";
                    return RedirectToAction(nameof(UserManagement));
                }
            }

            var userRolesList = await userManager.GetRolesAsync(user);
            var roles = await GetRolesForCurrentUser();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto,
                Email = user.Email ?? string.Empty,
                Role = userRolesList.FirstOrDefault() ?? string.Empty,
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
                var user = await userManager.FindByIdAsync(id);
                if (user != null)
                {
                    var userRoles = await userManager.GetRolesAsync(user);
                    if (!userRoles.Contains("Estagiario"))
                    {
                        TempData["ErrorMessage"] = "Coordenadores só podem editar usuários com perfil de Estagiário.";
                        return RedirectToAction(nameof(UserManagement));
                    }
                }
            }

            // Preencher Roles antes de qualquer verificação
            model.Roles = [.. roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name,
                    Selected = r.Name == model.Role
                })];

            if (isCoordenador)
            {
                model.Roles = [.. model.Roles.Where(r => r.Value == "Estagiario")];
            }

            ModelState.Remove("Roles");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Email = model.Email.ToLowerInvariant();

            if (id != model.Id)
            {
                return NotFound();
            }

            var userToEdit = await userManager.FindByIdAsync(id);
            if (userToEdit == null)
            {
                return NotFound();
            }

            // Verificar se é o administrador padrão
            if (userToEdit.Email != null && userToEdit.Email.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = AppConstants.DefaultAdminEditError;
                return RedirectToAction(nameof(UserManagement));
            }

            // Verificar se usuário atual não é admin padrão tentando editar outro admin
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser != null && !IsDefaultAdministrator(currentUser) && await IsUserAdministrator(userToEdit))
            {
                TempData["ErrorMessage"] = "Administradores comuns não podem editar outros administradores.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Verificar se o email foi alterado
            bool emailAlterado = !string.Equals(userToEdit.Email, model.Email, StringComparison.OrdinalIgnoreCase);
            bool primeiroAcessoPendente = !userToEdit.PrimeiroAcessoConcluido;

            // Verificar se o novo email já existe para outro usuário
            if (emailAlterado)
            {
                if (string.IsNullOrEmpty(model.Email))
                {
                    ModelState.AddModelError("Email", "Email não pode ser vazio.");
                    return View(model);
                }

                var usuarioComEmail = await userManager.FindByEmailAsync(model.Email);
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
                userToEdit.NormalizedEmail = userManager.NormalizeEmail(model.Email);
                userToEdit.NormalizedUserName = userManager.NormalizeName(model.Email);
            }

            var result = await userManager.UpdateAsync(userToEdit);
            if (result.Succeeded)
            {
                // Atualizar roles
                var currentRoles = await userManager.GetRolesAsync(userToEdit);
                if (currentRoles.Any())
                {
                    await userManager.RemoveFromRolesAsync(userToEdit, currentRoles);
                }
                await userManager.AddToRoleAsync(userToEdit, model.Role);

                if (model.ForcarRedefinicaoSenha)
                {
                    // Define o primeiro acesso como NÃO concluído
                    userToEdit.PrimeiroAcessoConcluido = false;
                    await userManager.UpdateAsync(userToEdit);

                    // Gera um novo token e envia o email de redefinição
                    var token = await userManager.GeneratePasswordResetTokenAsync(userToEdit);
                    token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code = token },
                        protocol: Request.Scheme);

                    await SendFirstAccessEmail(userToEdit, callbackUrl!);
                    TempData["SuccessMessage"] = $"Usuário {userToEdit.NomeCompleto} atualizado. Um novo email para definição de senha foi enviado.";
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
        public async Task<IActionResult> ViewUser(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                var userRoles = await userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Estagiario"))
                {
                    TempData["ErrorMessage"] = "Coordenadores só podem visualizar usuários com perfil de Estagiário.";
                    return RedirectToAction(nameof(UserManagement));
                }
            }

            var userRolesList = await userManager.GetRolesAsync(user);

            var model = new UserDetailViewModel
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto,
                Email = user.Email!,
                Cargo = user.Cargo,
                Role = userRolesList.FirstOrDefault() ?? string.Empty,
                DataCadastro = user.DataCadastro,
                Ativo = user.Ativo,
                PrimeiroAcessoConcluido = user.PrimeiroAcessoConcluido
            };

            return View(model);
        }

        // GET: Deletar usuário
        public async Task<IActionResult> DeleteUser(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                TempData["ErrorMessage"] = "Coordenadores não têm permissão para excluir usuários.";
                return RedirectToAction(nameof(UserManagement));
            }

            if (IsDefaultAdministrator(user))
            {
                TempData["ErrorMessage"] = AppConstants.DefaultAdminEditError;
                return RedirectToAction(nameof(UserManagement));
            }

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Usuário não encontrado.";
                return RedirectToAction(nameof(UserManagement));
            }

            if (!IsDefaultAdministrator(currentUser) && await IsUserAdministrator(user))
            {
                TempData["ErrorMessage"] = AppConstants.RegularAdminDeleteError;
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

            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Impedir a exclusão do usuário administrador padrão
            if (IsDefaultAdministrator(user))
            {
                TempData["ErrorMessage"] = "Não é possível excluir o usuário administrador padrão.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Verificar se usuário atual não é admin padrão tentando excluir outro admin
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser != null && !IsDefaultAdministrator(currentUser) && await IsUserAdministrator(user))
            {
                TempData["ErrorMessage"] = "Administradores comuns não podem excluir outros administradores.";
                return RedirectToAction(nameof(UserManagement));
            }

            var result = await userManager.DeleteAsync(user);
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

        private Task<List<SelectListItem>> GetRolesForCurrentUser()
        {
            var roles = roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

            // Se for coordenador, filtrar apenas a role Estagiario
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                roles = [.. roles.Where(r => r.Value == "Estagiario")];
            }

            return Task.FromResult(roles);
        }

        private static string GenerateTemporaryPassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_-+={[]}|:;?";
            const string allChars = uppercase + lowercase + digits + special;

            var password = new char[12];
            var random = new Random();

            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            for (int i = 4; i < 12; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            return new string([.. password.OrderBy(x => random.Next())]);
        }

        // GET: Visualizar administrador padrão
        public async Task<IActionResult> ViewAdmin()
        {
            var adminUser = await userManager.FindByEmailAsync("admin@estagio.com");
            if (adminUser == null)
            {
                return NotFound();
            }

            return View(adminUser);
        }

        private async Task<bool> IsUserAdministrator(ApplicationUser user)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            return userRoles.Contains("Administrador");
        }

        private static bool IsDefaultAdministrator(ApplicationUser user) =>
            user != null &&
            user.Email != null &&
            user.Email.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase);

        // Método de envio de email foi renomeado e atualizado para ser mais seguro
        private async Task SendFirstAccessEmail(ApplicationUser user, string callbackUrl)
        {
            if (string.IsNullOrEmpty(user.Email)) return;

            var subject = "Bem-vindo(a) ao Sistema de Gestão de Estágios - Defina sua Senha";
            var message = $@"
                <html>
                <body>
                    <h3>Bem-vindo(a) ao Sistema de Gestão de Estágios, {user.NomeCompleto}!</h3>
                    <p>Sua conta foi criada com sucesso. Para seu primeiro acesso, você precisa definir uma senha segura.</p>
                    <p>Por favor, clique no link abaixo para criar sua senha:</p>
                    <p><a href='{callbackUrl}' style='background-color: #0d6efd; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Definir Minha Senha</a></p>
                    <br>
                    <p>Se você não conseguir clicar no botão, copie e cole o seguinte link no seu navegador:</p>
                    <p><code>{callbackUrl}</code></p>
                    <br>
                    <p>Seu login é o seu email: <strong>{user.Email}</strong></p>
                    <p>Este link é válido por um tempo limitado. Se expirar, você pode usar a opção 'Esqueci minha senha' na tela de login.</p>
                    <br>
                    <p>Atenciosamente,<br>Equipe de Gestão de Estágios</p>
                </body>
                </html>";

            await emailSender.SendEmailAsync(user.Email, subject, message);
        }
    }
}