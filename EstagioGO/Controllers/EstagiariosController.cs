using EstagioGO.Data;
using EstagioGO.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EstagioGO.Controllers
{
    public partial class EstagiariosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        // Expressão regular corrigida para ser instanciada diretamente
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

        // GET: Estagiarios/Create
        public async Task<IActionResult> Create()
        {
            var (recursosDisponiveis, mensagemErro, redirecionarPara) = await CarregarViewBags();

            if (!recursosDisponiveis)
            {
                ViewBag.MensagemSemRecursos = mensagemErro;
                ViewBag.RedirecionarPara = redirecionarPara;
                return View("SemRecursos");
            }

            return View();
        }

        // POST: Estagiarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Estagiario estagiario)
        {
            // Remover a validação das propriedades de navegação
            ModelState.Remove("User");
            ModelState.Remove("Supervisor");
            ModelState.Remove("Frequencias");
            ModelState.Remove("Avaliacoes");

            // Remover formatação do CPF e Telefone antes de validar
            if (!string.IsNullOrEmpty(estagiario.CPF))
            {
                // A chamada ao método continua igual, pois o nome da variável não mudou
                estagiario.CPF = NonDigitsRegex.Replace(estagiario.CPF, "");
            }

            if (!string.IsNullOrEmpty(estagiario.Telefone))
            {
                // A chamada ao método continua igual, pois o nome da variável não mudou
                estagiario.Telefone = NonDigitsRegex.Replace(estagiario.Telefone, "");
            }

            // DEBUG: Log dos valores recebidos
            Debug.WriteLine($"=== DADOS RECEBIDOS ===");
            Debug.WriteLine($"Nome: {estagiario.Nome}");
            Debug.WriteLine($"Matricula: {estagiario.Matricula}");
            Debug.WriteLine($"UserId: {estagiario.UserId}");
            Debug.WriteLine($"SupervisorId: {estagiario.SupervisorId}");

            // Validação das datas
            if (estagiario.DataInicio >= estagiario.DataTermino)
            {
                ModelState.AddModelError("DataInicio", "A data de início não pode ser posterior ou igual à data de término.");
                ModelState.AddModelError("DataTermino", "A data de término deve ser posterior à data de início.");
            }

            // Verificar se o ModelState é válido antes de qualquer coisa
            if (!ModelState.IsValid)
            {
                Debug.WriteLine("=== ERROS DE VALIDAÇÃO ===");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state != null && state.Errors.Count > 0)
                    {
                        Debug.WriteLine($"{key}: {string.Join(", ", state.Errors.Select(e => e.ErrorMessage))}");
                    }
                }

                await CarregarViewBags();
                return View(estagiario);
            }

            // Verificar se o UserId já está em uso
            bool usuarioJaVinculado = await context.Estagiarios.AnyAsync(e => e.UserId == estagiario.UserId);
            Debug.WriteLine($"Usuário já vinculado: {usuarioJaVinculado}");

            if (usuarioJaVinculado)
            {
                ModelState.AddModelError("UserId", "Este usuário já está vinculado a outro estagiário.");
                Debug.WriteLine("Erro: Usuário já vinculado");

                await CarregarViewBags();
                return View(estagiario);
            }

            try
            {
                // A data de cadastro é definida automaticamente
                estagiario.DataCadastro = DateTime.Now;

                context.Add(estagiario);
                await context.SaveChangesAsync();

                Debug.WriteLine("Estagiário salvo com sucesso!");
                TempData["SuccessMessage"] = "Estagiário cadastrado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                Debug.WriteLine($"Erro ao salvar no banco de dados: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                ModelState.AddModelError("", "Não foi possível salvar o estagiário. Verifique os dados e tente novamente.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado: {ex.Message}");
                ModelState.AddModelError("", "Ocorreu um erro inesperado. Tente novamente.");
            }

            await CarregarViewBags();
            return View(estagiario);
        }

        // GET: Estagiarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var estagiario = await context.Estagiarios.FindAsync(id);
            if (estagiario == null) return NotFound();

            var (recursosDisponiveis, mensagemErro, redirecionarPara) = await CarregarViewBags(estagiario.UserId);

            if (!recursosDisponiveis)
            {
                ViewBag.MensagemSemRecursos = mensagemErro;
                ViewBag.RedirecionarPara = redirecionarPara;
                return View("SemRecursos");
            }

            return View(estagiario);
        }

        // POST: Estagiarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Estagiario estagiario)
        {
            // Remover a validação das propriedades de navegação
            ModelState.Remove("User");
            ModelState.Remove("Supervisor");
            ModelState.Remove("Frequencias");
            ModelState.Remove("Avaliacoes");

            // Remover formatação do CPF e Telefone antes de validar
            if (!string.IsNullOrEmpty(estagiario.CPF))
            {
                estagiario.CPF = NonDigitsRegex.Replace(estagiario.CPF, "");
            }

            if (!string.IsNullOrEmpty(estagiario.Telefone))
            {
                estagiario.Telefone = NonDigitsRegex.Replace(estagiario.Telefone, "");
            }

            // Validação das datas
            if (estagiario.DataInicio >= estagiario.DataTermino)
            {
                ModelState.AddModelError("DataInicio", "A data de início não pode ser posterior ou igual à data de término.");
                ModelState.AddModelError("DataTermino", "A data de término deve ser posterior à data de início.");
            }

            if (id != estagiario.Id)
            {
                return NotFound();
            }

            // Buscar o estagiário existente para preservar o UserId original
            var estagiarioExistente = await context.Estagiarios
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estagiarioExistente == null)
            {
                return NotFound();
            }

            // Manter o UserId original, ignorando qualquer alteração
            estagiario.UserId = estagiarioExistente.UserId;

            if (ModelState.IsValid)
            {
                try
                {
                    context.Update(estagiario);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Estagiário atualizado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EstagiarioExists(estagiario.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await CarregarViewBags(estagiario.UserId);
            return View(estagiario);
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
            var estagiario = await context.Estagiarios.FindAsync(id);
            if (estagiario != null)
            {
                context.Estagiarios.Remove(estagiario);
                await context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Estagiário excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        private bool EstagiarioExists(int id)
        {
            return context.Estagiarios.Any(e => e.Id == id);
        }

        private async Task<(bool recursosDisponiveis, string? mensagemErro, string? redirecionarPara)> CarregarViewBags(string? userIdAtual = null)
        {
            try
            {
                // Buscar supervisores
                var supervisores = await _userManager.GetUsersInRoleAsync("Supervisor");
                if (!supervisores.Any())
                {
                    return (false,
                            "Não há supervisores cadastrados. Você será redirecionado para criar um usuário supervisor.",
                            Url.Action("CreateUser", "Admin", new { contexto = "supervisor" }));
                }
                ViewBag.SupervisorId = new SelectList(supervisores, "Id", "NomeCompleto");

                // Buscar usuários com role "Estagiario"
                var estagiariosUsers = await _userManager.GetUsersInRoleAsync("Estagiario");

                // Verificar se há usuários com role de estagiário
                if (!estagiariosUsers.Any())
                {
                    return (false,
                            "Não há usuários com perfil de Estagiário cadastrados. Você será redirecionado para criar um usuário estagiário.",
                            Url.Action("CreateUser", "Admin", new { contexto = "estagiario" }));
                }

                // Obter IDs de usuários já vinculados
                var usuariosVinculados = await context.Estagiarios
                    .Where(e => e.UserId != null && e.UserId != userIdAtual)
                    .Select(e => e.UserId)
                    .ToListAsync();

                // Filtrar usuários não vinculados
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
                Debug.WriteLine($"Erro ao carregar ViewBags: {ex.Message}");
                ViewBag.SupervisorId = new SelectList(new List<ApplicationUser>(), "Id", "NomeCompleto");
                ViewBag.UserId = new SelectList(new List<ApplicationUser>(), "Id", "NomeCompleto");
                return (false, "Ocorreu um erro ao carregar os dados. Tente novamente.", null);
            }
        }
    }
}