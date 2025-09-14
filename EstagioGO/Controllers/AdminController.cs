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
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");

            // A consulta base para usuários.
            var usersQuery = userManager.Users;

            // A consulta que combina usuários com suas roles.
            // CORREÇÃO: Voltamos a usar 'from' em vez de 'join' para a propriedade de navegação UserRoles.
            var usersWithRolesQuery = from user in usersQuery
                                      from userRole in user.UserRoles
                                      join role in roleManager.Roles on userRole.RoleId equals role.Id
                                      select new UserManagementViewModel
                                      {
                                          Id = user.Id,
                                          NomeCompleto = user.NomeCompleto,
                                          Email = user.Email ?? string.Empty,
                                          Cargo = user.Cargo,
                                          DataCadastro = user.DataCadastro,
                                          Ativo = user.Ativo,
                                          PrimeiroAcessoConcluido = user.PrimeiroAcessoConcluido,
                                          Role = role.Name ?? string.Empty
                                      };

            // O filtro para coordenador continua funcionando perfeitamente.
            if (isCoordenador)
            {
                usersWithRolesQuery = usersWithRolesQuery.Where(u => u.Role == "Estagiario");
            }

            var usersWithRoles = await usersWithRolesQuery.OrderBy(u => u.NomeCompleto).ToListAsync();

            return View(usersWithRoles);
        }

        // GET: Criar novo usuário
        public async Task<IActionResult> CreateUser()
        {
            var model = new CreateUserViewModel
            {
                NomeCompleto = string.Empty,
                Email = string.Empty,
                Role = "Estagiario",
                Roles = await GetRolesForCurrentUser()
            };

            return View(model);
        }

        // POST: Criar novo usuário
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            ModelState.Remove("Roles");

            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador && model.Role != "Estagiario")
            {
                ModelState.AddModelError("Role", "Coordenadores só podem criar usuários com perfil de Estagiário.");
            }

            if (await userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Este email já está em uso.");
            }

            if (!ModelState.IsValid)
            {
                model.Roles = await GetRolesForCurrentUser();
                return View(model);
            }

            logger.LogInformation("Admin {AdminUser} iniciando tentativa de criação de usuário: {NewUserEmail}", User?.Identity?.Name, model.Email);

            var user = new ApplicationUser
            {
                UserName = model.Email.ToLowerInvariant(),
                Email = model.Email.ToLowerInvariant(),
                NomeCompleto = model.NomeCompleto,
                Cargo = model.Role,
                PrimeiroAcessoConcluido = false
            };

            var password = GenerateTemporaryPassword();
            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                logger.LogInformation("Usuário {NewUserEmail} criado com sucesso por {AdminUser}.", user.Email, User?.Identity?.Name);
                await userManager.AddToRoleAsync(user, model.Role);
                if (model.SendEmail)
                {
                    await SendFirstAccessEmailWithToken(user);
                }
                TempData["SuccessMessage"] = $"Usuário {user.NomeCompleto} criado com sucesso.";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                logger.LogError("Falha ao criar usuário {NewUserEmail}: {ErrorDescription}", model.Email, error.Description);
            }
            model.Roles = await GetRolesForCurrentUser();
            return View(model);
        }

        public async Task<IActionResult> EditUser(string id)
        {
            var (isAuthorized, result, user) = await AuthorizeAdminAction(id);
            if (!isAuthorized || user == null) return result!;

            var userRolesList = await userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto,
                Email = user.Email ?? string.Empty,
                Role = userRolesList.FirstOrDefault() ?? string.Empty,
                Ativo = user.Ativo,
                Roles = await GetRolesForCurrentUser(userRolesList.FirstOrDefault())
            };
            return View(model);
        }

        // POST: Editar usuário
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model)
        {
            var (isAuthorized, authResult, userToEdit) = await AuthorizeAdminAction(id);
            if (!isAuthorized || userToEdit == null) return authResult!;

            ModelState.Remove("Roles");
            if (!ModelState.IsValid)
            {
                model.Roles = await GetRolesForCurrentUser(model.Role);
                return View(model);
            }

            // --- LÓGICA DE ATUALIZAÇÃO DE E-MAIL (REINSERIDA E MELHORADA) ---

            var emailChanged = !string.Equals(userToEdit.Email, model.Email, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                // 1. Validar se o novo e-mail já está em uso por outro usuário
                var ownerOfEmail = await userManager.FindByEmailAsync(model.Email);
                if (ownerOfEmail != null && ownerOfEmail.Id != userToEdit.Id)
                {
                    ModelState.AddModelError("Email", "Este e-mail já está em uso por outro usuário.");
                    model.Roles = await GetRolesForCurrentUser(model.Role);
                    return View(model);
                }

                // 2. Atualizar todos os campos relacionados ao e-mail
                userToEdit.Email = model.Email.ToLowerInvariant();
                userToEdit.UserName = model.Email.ToLowerInvariant();
                userToEdit.NormalizedEmail = userManager.NormalizeEmail(model.Email);
                userToEdit.NormalizedUserName = userManager.NormalizeName(model.Email);
            }

            // --- FIM DA LÓGICA DE E-MAIL ---

            // Atualiza as outras propriedades
            userToEdit.NomeCompleto = model.NomeCompleto;
            userToEdit.Cargo = model.Role;
            userToEdit.Ativo = model.Ativo;

            var updateResult = await userManager.UpdateAsync(userToEdit);

            if (updateResult.Succeeded)
            {
                if (emailChanged)
                {
                    // 3. (BOA PRÁTICA) Atualiza o selo de segurança para invalidar logins antigos
                    await userManager.UpdateSecurityStampAsync(userToEdit);
                }

                var currentRoles = await userManager.GetRolesAsync(userToEdit);
                await userManager.RemoveFromRolesAsync(userToEdit, [.. currentRoles]);
                await userManager.AddToRoleAsync(userToEdit, model.Role);

                if (model.ForcarRedefinicaoSenha)
                {
                    userToEdit.PrimeiroAcessoConcluido = false;
                    await userManager.UpdateAsync(userToEdit);
                    await SendFirstAccessEmailWithToken(userToEdit);
                    TempData["SuccessMessage"] = $"Usuário {userToEdit.NomeCompleto} atualizado. Um novo email para definição de senha foi enviado.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Usuário {userToEdit.NomeCompleto} atualizado com sucesso.";
                }
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.Roles = await GetRolesForCurrentUser(model.Role);
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
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var (isAuthorized, result, user) = await AuthorizeAdminAction(id);
            if (!isAuthorized || user == null)
            {
                return result!; // Retorna NotFound ou Redirect se não autorizado
            }

            // Validação específica para a ação de DELETAR
            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                TempData["ErrorMessage"] = "Coordenadores não têm permissão para excluir usuários.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Passa o objeto de usuário (já buscado) para a view de confirmação
            return View(user);
        }

        // POST: Deletar usuário
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            // REATORAÇÃO: Reutiliza a mesma lógica de autorização
            var (isAuthorized, result, user) = await AuthorizeAdminAction(id);
            if (!isAuthorized || user == null)
            {
                return result!;
            }

            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                TempData["ErrorMessage"] = "Coordenadores não têm permissão para excluir usuários.";
                return RedirectToAction(nameof(UserManagement));
            }

            var deleteResult = await userManager.DeleteAsync(user);
            if (deleteResult.Succeeded)
            {
                TempData["SuccessMessage"] = $"Usuário {user.NomeCompleto} foi excluído com sucesso.";
            }
            else
            {
                // Adiciona os erros específicos do Identity para o TempData, se houver
                var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Erro ao excluir o usuário {user.NomeCompleto}: {errors}";
            }
            logger.LogInformation("Usuário {UserId} excluído com sucesso por {AdminUser}.", user.Id, User?.Identity?.Name);
            return RedirectToAction(nameof(UserManagement));
        }

        private async Task<List<SelectListItem>> GetRolesForCurrentUser(string? selectedRole = null)
        {
            var rolesQuery = roleManager.Roles;

            var isCoordenador = User.IsInRole("Coordenador") && !User.IsInRole("Administrador");
            if (isCoordenador)
            {
                rolesQuery = rolesQuery.Where(r => r.Name == "Estagiario");
            }

            return await rolesQuery.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name,
                Selected = r.Name == selectedRole
            }).ToListAsync();
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

        private async Task<(bool IsAuthorized, IActionResult? ActionResult, ApplicationUser? User)> AuthorizeAdminAction(string targetUserId)
        {
            if (string.IsNullOrEmpty(targetUserId))
            {
                return (false, NotFound(), null);
            }

            var targetUser = await userManager.FindByIdAsync(targetUserId);
            if (targetUser == null)
            {
                return (false, NotFound(), null);
            }

            if (IsDefaultAdministrator(targetUser))
            {
                TempData["ErrorMessage"] = "Não é possível gerenciar o usuário administrador padrão.";
                return (false, RedirectToAction(nameof(UserManagement)), null);
            }

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return (false, Unauthorized(), null);
            }

            if (!IsDefaultAdministrator(currentUser) && await userManager.IsInRoleAsync(targetUser, "Administrador"))
            {
                TempData["ErrorMessage"] = "Administradores comuns não podem gerenciar outros administradores.";
                return (false, RedirectToAction(nameof(UserManagement)), null);
            }

            var isCoordenador = await userManager.IsInRoleAsync(currentUser, "Coordenador") && !await userManager.IsInRoleAsync(currentUser, "Administrador");
            if (isCoordenador && !await userManager.IsInRoleAsync(targetUser, "Estagiario"))
            {
                TempData["ErrorMessage"] = "Coordenadores só podem gerenciar usuários com perfil de Estagiário.";
                return (false, RedirectToAction(nameof(UserManagement)), null);
            }

            return (true, null, targetUser);
        }

        private static bool IsDefaultAdministrator(ApplicationUser user) =>
            user != null &&
            user.Email != null &&
            user.Email.Equals(AppConstants.DefaultAdminEmail, StringComparison.OrdinalIgnoreCase);

        // Método de envio de email foi renomeado e atualizado para ser mais seguro
        private async Task SendFirstAccessEmailWithToken(ApplicationUser user)
        {
            if (string.IsNullOrEmpty(user.Email)) return;

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = token },
                protocol: Request.Scheme);

            var subject = "Bem-vindo(a) ao Sistema de Gestão de Estágios - Defina sua Senha";
            var message = $@"
                <h3>Bem-vindo(a), {user.NomeCompleto}!</h3>
                <p>Sua conta foi criada. Para seu primeiro acesso, defina uma senha segura clicando no link abaixo:</p>
                <p><a href='{callbackUrl}' style='background-color: #0d6efd; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Definir Minha Senha</a></p>
                <p>Seu login é: <strong>{user.Email}</strong></p>
                <p>Atenciosamente,<br>Equipe de Gestão de Estágios</p>";

            await emailSender.SendEmailAsync(user.Email, subject, message);
        }
    }
}