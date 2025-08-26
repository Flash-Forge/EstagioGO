using EstagioGO.Data;
using EstagioGO.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace EstagioGO.Controllers
{
    public class EstagiariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EstagiariosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Estagiarios
        public async Task<IActionResult> Index()
        {
            var estagiarios = await _context.Estagiarios
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

            var estagiario = await _context.Estagiarios
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
                    foreach (var error in state.Errors)
                    {
                        Debug.WriteLine($"{key}: {error.ErrorMessage}");
                    }
                }

                await CarregarViewBags();
                return View(estagiario);
            }

            // Verificar se o UserId já está em uso
            bool usuarioJaVinculado = await _context.Estagiarios.AnyAsync(e => e.UserId == estagiario.UserId);
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

                _context.Add(estagiario);
                await _context.SaveChangesAsync();

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

            var estagiario = await _context.Estagiarios.FindAsync(id);
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
            var estagiarioExistente = await _context.Estagiarios
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
                    _context.Update(estagiario);
                    await _context.SaveChangesAsync();

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

            var estagiario = await _context.Estagiarios
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
            var estagiario = await _context.Estagiarios.FindAsync(id);
            _context.Estagiarios.Remove(estagiario);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Estagiário excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        private bool EstagiarioExists(int id)
        {
            return _context.Estagiarios.Any(e => e.Id == id);
        }

        private async Task<(bool recursosDisponiveis, string mensagemErro, string redirecionarPara)> CarregarViewBags(string userIdAtual = null)
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
                var usuariosVinculados = await _context.Estagiarios
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
                if (!usuariosDisponiveis.Any())
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