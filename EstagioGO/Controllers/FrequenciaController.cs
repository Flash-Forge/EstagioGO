using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EstagioGO.Data;
using EstagioGO.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace EstagioGO.Controllers
{
    [Authorize]
    public class FrequenciaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FrequenciaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Frequencia
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                // Para estagiários, buscar apenas seus próprios registros
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario != null)
                {
                    ViewBag.EstagiarioId = estagiario.Id;
                    ViewBag.EstagiarioNome = estagiario.Nome;

                    // Carregar as frequências do estagiário
                    var frequencias = await _context.Frequencias
                        .Include(f => f.Estagiario)
                        .Where(f => f.EstagiarioId == estagiario.Id)
                        .OrderByDescending(f => f.Data)
                        .ThenByDescending(f => f.DataRegistro)
                        .Take(10) // Últimos 10 registros
                        .ToListAsync();

                    return View("IndexEstagiario", frequencias);
                }
                else
                {
                    return NotFound("Estagiário não encontrado para este usuário.");
                }
            }
            else
            {
                // Para coordenadores/supervisores, mostrar todos os estagiários
                ViewBag.Estagiarios = await _context.Estagiarios.OrderBy(e => e.Nome).ToListAsync();
                return View();
            }
        }

        // GET: Frequencia/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var frequencia = await _context.Frequencias
                .Include(f => f.Estagiario)
                .Include(f => f.Justificativa)
                .Include(f => f.RegistradoPor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (frequencia == null)
            {
                return NotFound();
            }

            // Verificar se o usuário tem permissão para ver este registro
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }
            }

            return View(frequencia);
        }

        // GET: Frequencia/Create
        public async Task<IActionResult> Create(int? estagiarioId)
        {
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            // Se for estagiário, buscar seu próprio ID
            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null)
                {
                    return NotFound("Estagiário não encontrado para este usuário.");
                }

                estagiarioId = estagiario.Id;
            }

            // Carregar Estagiários com nome
            if (isEstagiario)
            {
                ViewData["EstagiarioId"] = new SelectList(
                    _context.Estagiarios.Where(e => e.Id == estagiarioId),
                    "Id", "Nome", estagiarioId);
            }
            else
            {
                ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", estagiarioId);
            }

            // Carregar Justificativas apenas para não-estagiários
            if (!isEstagiario)
            {
                ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao");
            }

            // Se for estagiário, definir valores padrão
            if (isEstagiario)
            {
                var model = new Frequencia
                {
                    EstagiarioId = estagiarioId.Value,
                    Data = DateTime.Today,
                    HoraEntrada = DateTime.Now.TimeOfDay,
                    Presente = true, // Estagiário está batendo ponto, portanto presente
                    DataRegistro = DateTime.Now,
                    RegistradoPorId = user.Id
                };

                return View(model);
            }

            return View();
        }

        // POST: Frequencia/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EstagiarioId,Data,HoraEntrada,HoraSaida,Presente,Observacao,JustificativaId")] Frequencia frequencia)
        {
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            // Validações
            if (frequencia.Data > DateTime.Today)
            {
                ModelState.AddModelError("Data", "A data não pode ser futura.");
            }

            if (frequencia.HoraEntrada.HasValue && frequencia.HoraSaida.HasValue &&
                frequencia.HoraEntrada.Value > frequencia.HoraSaida.Value)
            {
                ModelState.AddModelError("HoraSaida", "A hora de saída não pode ser anterior à hora de entrada.");
            }

            // Para estagiários, forçar presença = true e remover justificativa
            if (isEstagiario)
            {
                frequencia.Presente = true;
                frequencia.JustificativaId = null; // Estagiários não podem definir justificativa

                // Verificar se o estagiário está tentando registrar para si mesmo
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }
            }

            // Verifica se já existe frequência registrada para o estagiário na mesma data
            bool existeRegistro = await _context.Frequencias
                .AnyAsync(f => f.EstagiarioId == frequencia.EstagiarioId && f.Data.Date == frequencia.Data.Date);

            if (existeRegistro)
            {
                ModelState.AddModelError("", "Já existe um registro de frequência para esse estagiário nesta data.");
            }

            // Definir automaticamente data de registro e usuário que registrou
            frequencia.DataRegistro = DateTime.Now;
            frequencia.RegistradoPorId = user.Id;

            if (ModelState.IsValid)
            {
                _context.Add(frequencia);
                await _context.SaveChangesAsync();

                if (isEstagiario)
                {
                    TempData["SuccessMessage"] = "Ponto registrado com sucesso!";
                }
                else
                {
                    TempData["SuccessMessage"] = "Frequência registrada com sucesso!";
                }

                return RedirectToAction(nameof(Index));
            }

            // Recarregar as listas em caso de erro
            if (isEstagiario)
            {
                ViewData["EstagiarioId"] = new SelectList(
                    _context.Estagiarios.Where(e => e.Id == frequencia.EstagiarioId),
                    "Id", "Nome", frequencia.EstagiarioId);
            }
            else
            {
                ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", frequencia.EstagiarioId);
                ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao", frequencia.JustificativaId);
            }

            return View(frequencia);
        }

        // GET: Frequencia/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var frequencia = await _context.Frequencias.FindAsync(id);
            if (frequencia == null)
            {
                return NotFound();
            }

            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }
            }

            ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", frequencia.EstagiarioId);

            if (!isEstagiario)
            {
                ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao", frequencia.JustificativaId);
            }

            return View(frequencia);
        }

        // POST: Frequencia/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EstagiarioId,Data,HoraEntrada,HoraSaida,Presente,Observacao,JustificativaId,DataRegistro,RegistradoPorId")] Frequencia frequencia)
        {
            if (id != frequencia.Id)
            {
                return NotFound();
            }

            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || frequencia.EstagiarioId != estagiario.Id)
                {
                    return Forbid();
                }

                // Estagiários não podem alterar a presença ou justificativa
                var originalFrequencia = await _context.Frequencias.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (originalFrequencia != null)
                {
                    frequencia.Presente = originalFrequencia.Presente;
                    frequencia.JustificativaId = originalFrequencia.JustificativaId;
                }
            }

            // Validações
            if (frequencia.Data > DateTime.Today)
            {
                ModelState.AddModelError("Data", "A data não pode ser futura.");
            }

            if (frequencia.HoraEntrada.HasValue && frequencia.HoraSaida.HasValue &&
                frequencia.HoraEntrada.Value > frequencia.HoraSaida.Value)
            {
                ModelState.AddModelError("HoraSaida", "A hora de saída não pode ser anterior à hora de entrada.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(frequencia);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Frequência atualizada com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FrequenciaExists(frequencia.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", frequencia.EstagiarioId);

            if (!isEstagiario)
            {
                ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao", frequencia.JustificativaId);
            }

            return View(frequencia);
        }

        // GET: Frequencia/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var frequencia = await _context.Frequencias
                .Include(f => f.Estagiario)
                .Include(f => f.Justificativa)
                .Include(f => f.RegistradoPor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (frequencia == null)
            {
                return NotFound();
            }

            // Verificar permissões (apenas administradores/coordenadores podem excluir)
            var isEstagiario = User.IsInRole("Estagiario");
            var isSupervisor = User.IsInRole("Supervisor");

            if (isEstagiario || isSupervisor)
            {
                return Forbid();
            }

            return View(frequencia);
        }

        // POST: Frequencia/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var frequencia = await _context.Frequencias.FindAsync(id);

            if (frequencia != null)
            {
                _context.Frequencias.Remove(frequencia);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Frequência excluída com sucesso!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Frequencia/EditList/5
        public async Task<IActionResult> EditList(int estagiarioId)
        {
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            // Se for estagiário, verificar se está tentando acessar seus próprios registros
            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || estagiario.Id != estagiarioId)
                {
                    return Forbid(); // Não permitir que estagiário acesse registros de outros
                }
            }

            var frequencias = await _context.Frequencias
                .Include(f => f.Estagiario)
                .Include(f => f.Justificativa)
                .Where(f => f.EstagiarioId == estagiarioId)
                .OrderByDescending(f => f.Data)
                .ThenByDescending(f => f.DataRegistro)
                .ToListAsync();

            ViewBag.EstagiarioId = estagiarioId;
            ViewBag.EstagiarioNome = await _context.Estagiarios
                .Where(e => e.Id == estagiarioId)
                .Select(e => e.Nome)
                .FirstOrDefaultAsync();

            return View(frequencias);
        }

        [HttpGet]
        public async Task<JsonResult> GetFrequenciasParaCalendario(int estagiarioId)
        {
            // Verificar permissões
            var user = await _userManager.GetUserAsync(User);
            var isEstagiario = User.IsInRole("Estagiario");

            if (isEstagiario)
            {
                var estagiario = await _context.Estagiarios
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (estagiario == null || estagiario.Id != estagiarioId)
                {
                    return Json(new { error = "Acesso não autorizado" });
                }
            }

            var frequencias = await _context.Frequencias
                .Where(f => f.EstagiarioId == estagiarioId)
                .Select(f => new
                {
                    date = f.Data.ToString("yyyy-MM-dd"),  // formato padrão ISO para JS
                    presente = f.Presente
                })
                .ToListAsync();

            return Json(frequencias);
        }

        private bool FrequenciaExists(int id)
        {
            return _context.Frequencias.Any(e => e.Id == id);
        }
    }
}