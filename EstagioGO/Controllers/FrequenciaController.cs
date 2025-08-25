using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EstagioGO.Data;
using EstagioGO.Models.Domain;

namespace EstagioGO.Controllers
{
    public class FrequenciaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FrequenciaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Frequencia
        public async Task<IActionResult> Index()
        {
            ViewBag.Estagiarios = await _context.Estagiarios.OrderBy(e => e.Nome).ToListAsync();
            return View();
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

            return View(frequencia);
        }

        // GET: Frequencia/Create
        public IActionResult Create()
        {
            // Carregar Estagiários com nome (supondo que sua entidade tenha a propriedade Nome)
            ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome");

            // Carregar Justificativas normalmente
            ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao"); // ou outra propriedade que queira mostrar

            // Carregar Usuários responsáveis pelo registro com o nome
            ViewData["RegistradoPorId"] = new SelectList(_context.Users, "Id", "NomeCompleto"); // ajuste NomeCompleto para a propriedade correta

            return View();
        }


        // POST: Frequencia/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EstagiarioId,Data,HoraEntrada,HoraSaida,Presente,Observacao,JustificativaId,RegistradoPorId")] Frequencia frequencia)
        {
            frequencia.DataRegistro = DateTime.Now;

            // Verifica se já existe frequência registrada para o estagiário na mesma data
            bool existeRegistro = await _context.Frequencias
                .AnyAsync(f => f.EstagiarioId == frequencia.EstagiarioId && f.Data.Date == frequencia.Data.Date);

            if (existeRegistro)
            {
                ModelState.AddModelError("", "Já existe um registro de frequência para esse estagiário nesta data.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(frequencia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "Nome", frequencia.EstagiarioId);
            ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Descricao", frequencia.JustificativaId);
            ViewData["RegistradoPorId"] = new SelectList(_context.Users, "Id", "NomeCompleto", frequencia.RegistradoPorId);

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
            ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "CoordenadorId", frequencia.EstagiarioId);
            ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Id", frequencia.JustificativaId);
            ViewData["RegistradoPorId"] = new SelectList(_context.Users, "Id", "Id", frequencia.RegistradoPorId);
            return View(frequencia);
        }

        // POST: Frequencia/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EstagiarioId,Data,HoraEntrada,HoraSaida,Presente,Observacao,JustificativaId,RegistradoPorId")] Frequencia frequencia)
        {
            if (id != frequencia.Id)
            {
                return NotFound();
            }

            frequencia.DataRegistro = DateTime.Now;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(frequencia);
                    await _context.SaveChangesAsync();
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
            ViewData["EstagiarioId"] = new SelectList(_context.Estagiarios, "Id", "CoordenadorId", frequencia.EstagiarioId);
            ViewData["JustificativaId"] = new SelectList(_context.Justificativas, "Id", "Id", frequencia.JustificativaId);
            ViewData["RegistradoPorId"] = new SelectList(_context.Users, "Id", "Id", frequencia.RegistradoPorId);
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
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FrequenciaExists(int id)
        {
            return _context.Frequencias.Any(e => e.Id == id);
        }


        [HttpGet]
        public async Task<JsonResult> GetFrequenciasParaCalendario(int estagiarioId)
        {
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


    }
}
