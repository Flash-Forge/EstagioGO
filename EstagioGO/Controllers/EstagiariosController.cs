using EstagioGO.Data;
using EstagioGO.Models.Estagio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Para envio de e-mail
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities; // Para o token
using Microsoft.EntityFrameworkCore;
using System.Text; // Para o token
using System.Text.RegularExpressions;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Administrador,Coordenador")]
    public class EstagiariosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender) : Controller
    {
        private static readonly Regex NonDigitsRegex = new(@"[^\d]", RegexOptions.Compiled);

        // GET: Estagiarios
        public async Task<IActionResult> Index()
        {
            var estagiarios = await context.Estagiarios
                .Include(e => e.Supervisor)
                .Include(e => e.User)
                .ToListAsync();
            return View(estagiarios);
        }

        // GET: Estagiarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var estagiario = await context.Estagiarios
                .Include(e => e.Supervisor)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (estagiario == null)
            {
                return NotFound();
            }

            return View(estagiario);
        }

        // GET: Estagiarios/Create (Atualizado)
        public async Task<IActionResult> Create()
        {
            var supervisores = await userManager.GetUsersInRoleAsync("Supervisor");
            if (!supervisores.Any())
            {
                // Redireciona para uma view de erro se não houver supervisores
                ViewBag.MensagemSemRecursos = "Não há supervisores cadastrados. Por favor, cadastre um supervisor antes de adicionar um estagiário.";
                ViewBag.RedirecionarPara = Url.Action("CreateUser", "Admin", new { contexto = "supervisor" });
                return View("SemRecursos");
            }

            ViewBag.SupervisorId = new SelectList(supervisores, "Id", "NomeCompleto");
            return View(new CreateEstagiarioViewModel());
        }

        // POST: Estagiarios/Create (Totalmente refeito)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEstagiarioViewModel viewModel)
        {
            // Validações personalizadas
            if (viewModel.DataTermino.HasValue && viewModel.DataInicio >= viewModel.DataTermino.Value)
            {
                ModelState.AddModelError("DataInicio", "A data de início deve ser anterior à data de término.");
            }

            var existingUser = await userManager.FindByEmailAsync(viewModel.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Este e-mail já está em uso por outro usuário no sistema.");
            }

            if (!ModelState.IsValid)
            {
                // Recarrega o dropdown de supervisores em caso de erro
                ViewBag.SupervisorId = new SelectList(await userManager.GetUsersInRoleAsync("Supervisor"), "Id", "NomeCompleto", viewModel.SupervisorId);
                return View(viewModel);
            }

            // 1. Criar a conta de usuário (ApplicationUser)
            var newUser = new ApplicationUser
            {
                UserName = viewModel.Email,
                Email = viewModel.Email,
                NomeCompleto = viewModel.Nome,
                Cargo = "Estagiario",
                PrimeiroAcessoConcluido = false
            };

            // É preciso uma senha temporária, mas o usuário definirá a sua no primeiro acesso
            var tempPassword = GenerateTemporaryPassword();
            var result = await userManager.CreateAsync(newUser, tempPassword);

            if (result.Succeeded)
            {
                // Adiciona o usuário ao Role "Estagiario"
                await userManager.AddToRoleAsync(newUser, "Estagiario");

                // 2. Criar o perfil do Estagiário
                var estagiario = new Estagiario
                {
                    Nome = viewModel.Nome,
                    CPF = NonDigitsRegex.Replace(viewModel.CPF, ""),
                    DataNascimento = viewModel.DataNascimento,
                    Telefone = !string.IsNullOrEmpty(viewModel.Telefone) ? NonDigitsRegex.Replace(viewModel.Telefone, "") : null,
                    Matricula = viewModel.Matricula,
                    Curso = viewModel.Curso,
                    InstituicaoEnsino = viewModel.InstituicaoEnsino,
                    DataInicio = viewModel.DataInicio,
                    DataTermino = viewModel.DataTermino,
                    SupervisorId = viewModel.SupervisorId,
                    Ativo = viewModel.Ativo,
                    DataCadastro = DateTime.Now,
                    UserId = newUser.Id // Vincula o perfil do estagiário ao usuário recém-criado
                };

                context.Add(estagiario);
                await context.SaveChangesAsync();

                // Enviar e-mail de boas-vindas para definição de senha
                await SendFirstAccessEmail(newUser);

                TempData["SuccessMessage"] = $"Estagiário {estagiario.Nome} e seu usuário de acesso foram criados com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            // Se a criação do usuário falhar, adiciona os erros ao ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.SupervisorId = new SelectList(await userManager.GetUsersInRoleAsync("Supervisor"), "Id", "NomeCompleto", viewModel.SupervisorId);
            return View(viewModel);
        }

        // Em EstagiariosController.cs

        // GET: Estagiarios/Edit/5 (Atualizado para usar ViewModel)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Busca o estagiário e seu usuário associado
            var estagiario = await context.Estagiarios
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estagiario == null || estagiario.User == null) return NotFound();

            // Mapeia os dados da entidade para o ViewModel
            var viewModel = new EditEstagiarioViewModel
            {
                Id = estagiario.Id,
                Nome = estagiario.Nome,
                Email = estagiario.User.Email, // Pega o e-mail do usuário associado
                CPF = estagiario.CPF,
                DataNascimento = estagiario.DataNascimento,
                Telefone = estagiario.Telefone,
                Matricula = estagiario.Matricula,
                Curso = estagiario.Curso,
                InstituicaoEnsino = estagiario.InstituicaoEnsino,
                DataInicio = estagiario.DataInicio,
                DataTermino = estagiario.DataTermino,
                SupervisorId = estagiario.SupervisorId,
                Ativo = estagiario.Ativo
            };

            ViewBag.SupervisorId = new SelectList(await userManager.GetUsersInRoleAsync("Supervisor"), "Id", "NomeCompleto", estagiario.SupervisorId);
            return View(viewModel);
        }

        // POST: Estagiarios/Edit/5 (Atualizado para usar ViewModel e lógica de e-mail)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditEstagiarioViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            // Validações personalizadas
            if (viewModel.DataTermino.HasValue && viewModel.DataInicio >= viewModel.DataTermino.Value)
            {
                ModelState.AddModelError("DataInicio", "A data de início deve ser anterior à data de término.");
            }

            // Busca o estagiário original e seu usuário
            var estagiarioParaAtualizar = await context.Estagiarios
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estagiarioParaAtualizar?.User == null)
            {
                return NotFound();
            }

            // Verifica se o e-mail mudou e se o novo e-mail já está em uso por OUTRO usuário
            var emailChanged = !string.Equals(estagiarioParaAtualizar.User.Email, viewModel.Email, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                var ownerOfEmail = await userManager.FindByEmailAsync(viewModel.Email);
                if (ownerOfEmail != null && ownerOfEmail.Id != estagiarioParaAtualizar.UserId)
                {
                    ModelState.AddModelError("Email", "Este e-mail já está em uso por outro usuário.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Atualiza as propriedades do perfil do Estagiário
                    estagiarioParaAtualizar.Nome = viewModel.Nome;
                    estagiarioParaAtualizar.CPF = NonDigitsRegex.Replace(viewModel.CPF, "");
                    estagiarioParaAtualizar.DataNascimento = viewModel.DataNascimento;
                    estagiarioParaAtualizar.Telefone = NonDigitsRegex.Replace(viewModel.Telefone, "");
                    estagiarioParaAtualizar.Matricula = viewModel.Matricula;
                    estagiarioParaAtualizar.Curso = viewModel.Curso;
                    estagiarioParaAtualizar.InstituicaoEnsino = viewModel.InstituicaoEnsino;
                    estagiarioParaAtualizar.DataInicio = viewModel.DataInicio;
                    estagiarioParaAtualizar.DataTermino = viewModel.DataTermino;
                    estagiarioParaAtualizar.SupervisorId = viewModel.SupervisorId;
                    estagiarioParaAtualizar.Ativo = viewModel.Ativo;

                    // 2. Atualiza e sincroniza as propriedades do usuário (ApplicationUser)
                    var user = estagiarioParaAtualizar.User;
                    user.NomeCompleto = viewModel.Nome;
                    user.Ativo = viewModel.Ativo;

                    if (emailChanged)
                    {
                        user.Email = viewModel.Email.ToLowerInvariant();
                        user.UserName = viewModel.Email.ToLowerInvariant();
                        user.NormalizedEmail = userManager.NormalizeEmail(viewModel.Email);
                        user.NormalizedUserName = userManager.NormalizeName(viewModel.Email);
                    }

                    await userManager.UpdateAsync(user);
                    if (emailChanged)
                    {
                        await userManager.UpdateSecurityStampAsync(user);
                    }

                    // 3. Salva as alterações no perfil do estagiário
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Estagiário atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!context.Estagiarios.Any(e => e.Id == estagiarioParaAtualizar.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.SupervisorId = new SelectList(await userManager.GetUsersInRoleAsync("Supervisor"), "Id", "NomeCompleto", viewModel.SupervisorId);
            return View(viewModel);
        }

        // GET: Estagiarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var estagiario = await context.Estagiarios
                .Include(e => e.Supervisor)
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (estagiario == null)
            {
                return NotFound();
            }

            return View(estagiario);
        }

        // POST: Estagiarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Usamos 'Include' para garantir que a propriedade 'User' seja carregada
            var estagiario = await context.Estagiarios
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estagiario != null)
            {
                // 1. Encontra o usuário associado (se existir)
                var user = estagiario.User;

                // 2. Remove primeiro o perfil do estagiário
                context.Estagiarios.Remove(estagiario);

                // 3. Se um usuário estava vinculado, remove-o também
                if (user != null)
                {
                    var result = await userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        // Se houver um erro ao deletar o usuário, exibe a mensagem de erro
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        TempData["ErrorMessage"] = $"Não foi possível excluir a conta de usuário associada: {errors}";
                        return RedirectToAction(nameof(Index));
                    }
                }

                await context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Estagiário e sua conta de usuário foram excluídos com sucesso!";
            }
            else
            {
                TempData["ErrorMessage"] = "Estagiário não encontrado.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EstagiarioExists(int id)
        {
            return context.Estagiarios.Any(e => e.Id == id);
        }

        // Em EstagiariosController.cs

        // POST: Estagiarios/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var estagiario = await context.Estagiarios.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
            if (estagiario?.User == null)
            {
                TempData["ErrorMessage"] = "Usuário associado ao estagiário não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Força o usuário a redefinir a senha no próximo login
            estagiario.User.PrimeiroAcessoConcluido = false;
            await userManager.UpdateAsync(estagiario.User);

            // Envia o e-mail com o link de redefinição
            await SendFirstAccessEmail(estagiario.User);

            TempData["SuccessMessage"] = $"Link para redefinição de senha enviado para {estagiario.User.Email}.";
            return RedirectToAction(nameof(Edit), new { id = estagiario.Id });
        }

        // --- MÉTODOS AUXILIARES ---
        private static string GenerateTemporaryPassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_-+={[]}|:;?";
            const string allChars = uppercase + lowercase + digits + special;

            var password = new char[12];
            var random = new Random();

            // Garante que a senha tenha pelo menos um de cada tipo de caractere
            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            // Preenche o resto da senha com caracteres aleatórios
            for (int i = 4; i < 12; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Embaralha o resultado final para que os caracteres especiais não fiquem sempre no início
            return new string([.. password.OrderBy(x => random.Next())]);
        }

        private async Task SendFirstAccessEmail(ApplicationUser user)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = token },
                protocol: Request.Scheme);

            await emailSender.SendEmailAsync(user.Email,
                "Sua conta no Sistema de Gestão de Estágios foi criada!",
                $"Olá {user.NomeCompleto},<br/><br/>Sua conta foi criada com sucesso. Por favor, defina sua senha de acesso clicando <a href='{callbackUrl}'>aqui</a>.");
        }

        private async Task<(bool recursosDisponiveis, string? mensagemErro, string? redirecionarPara)> CarregarViewBags(string? userIdAtual = null)
        {
            try
            {
                // Buscar supervisores
                var supervisores = await userManager.GetUsersInRoleAsync("Supervisor");
                if (!supervisores.Any())
                {
                    return (false,
                            "Não há supervisores cadastrados. Você será redirecionado para criar um usuário supervisor.",
                            Url.Action("CreateUser", "Admin", new { contexto = "supervisor" }));
                }
                ViewBag.SupervisorId = new SelectList(supervisores, "Id", "NomeCompleto");

                // Buscar usuários com role "Estagiario"
                var estagiariosUsers = await userManager.GetUsersInRoleAsync("Estagiario");

                // Verificar se há usuários com role de estagiário
                if (!estagiariosUsers.Any())
                {
                    return (false,
                            "Não há usuários com perfil de Estagiário cadastrados. Você será redirecionado para criar um usuário estagiário.",
                            Url.Action("CreateUser", "Admin", new { contexto = "estagiario" }));
                }

                var usuariosVinculados = await context.Estagiarios
                    .Where(e => e.UserId != null && e.UserId != userIdAtual)
                    .Select(e => e.UserId)
                    .ToListAsync();

                var usuariosDisponiveis = estagiariosUsers
                    .Where(u => !usuariosVinculados.Contains(u.Id))
                    .ToList();

                // Se estiver editando, garantir que o usuário atual está na lista
                if (!string.IsNullOrEmpty(userIdAtual))
                {
                    var usuarioAtual = estagiariosUsers.FirstOrDefault(u => u.Id == userIdAtual);
                    if (usuarioAtual != null && !usuariosDisponiveis.Any(u => u.Id == userIdAtual))
                    {
                        usuariosDisponiveis.Add(usuarioAtual);
                    }
                }

                // Verificar se há usuários disponíveis para vincular
                if (usuariosDisponiveis.Count == 0)
                {
                    return (false,
                            "Não há usuários disponíveis com perfil de Estagiário. Todos os usuários estagiários já estão vinculados a outros cadastros. Você será redirecionado para criar um novo usuário estagiário.",
                            Url.Action("CreateUser", "Admin", new { contexto = "estagiario" }));
                }

                ViewBag.UserId = new SelectList(usuariosDisponiveis, "Id", "NomeCompleto");

                return (true, null, null);
            }
            catch (Exception ex)
            {
                ViewBag.SupervisorId = new SelectList(new List<ApplicationUser>(), "Id", "NomeCompleto");
                ViewBag.UserId = new SelectList(new List<ApplicationUser>(), "Id", "NomeCompleto");
                return (false, "Ocorreu um erro ao carregar os dados. Tente novamente.", null);
            }
        }
    }
}