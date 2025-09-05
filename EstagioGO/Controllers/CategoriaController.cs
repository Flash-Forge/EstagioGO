using EstagioGO.Data;
using EstagioGO.Models.Analise;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Administrador,Coordenador")]
    public class CategoriaController(ApplicationDbContext context, ILogger<CategoriaController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<CategoriaController> _logger = logger;

        // GET: Categoria
        public async Task<IActionResult> Index()
        {
            try
            {
                var categorias = await _context.Categorias
                    .Include(c => c.Competencias)
                    .OrderBy(c => c.OrdemExibicao)
                    .ThenBy(c => c.Nome)
                    .ToListAsync();

                return View(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar lista de categorias");
                TempData["ErrorMessage"] = "Erro ao carregar as categorias.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Categoria/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias
                .Include(c => c.Competencias.Where(comp => comp.Ativo))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // GET: Categoria/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categoria/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nome,Descricao,OrdemExibicao,Ativo")] Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(categoria);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Categoria criada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar categoria");
                    ModelState.AddModelError("", "Erro ao salvar a categoria. Tente novamente.");
                }
            }
            return View(categoria);
        }

        // GET: Categoria/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }
            return View(categoria);
        }

        // POST: Categoria/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nome,Descricao,OrdemExibicao,Ativo")] Categoria categoria)
        {
            if (id != categoria.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoria);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Categoria atualizada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoriaExists(categoria.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao atualizar categoria");
                    ModelState.AddModelError("", "Erro ao atualizar a categoria. Tente novamente.");
                }
            }
            return View(categoria);
        }

        // GET: Categoria/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias
                .Include(c => c.Competencias)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // POST: Categoria/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var categoria = await _context.Categorias
                    .Include(c => c.Competencias)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (categoria != null)
                {
                    // Verificar se há competências associadas
                    if (categoria.Competencias.Count != 0)
                    {
                        TempData["ErrorMessage"] = "Não é possível excluir uma categoria que possui competências associadas. Desative-a ou remova as competências primeiro.";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.Categorias.Remove(categoria);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Categoria excluída com sucesso!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir categoria");
                TempData["ErrorMessage"] = "Erro ao excluir a categoria. Tente novamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Categoria/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var categoria = await _context.Categorias.FindAsync(id);
                if (categoria != null)
                {
                    categoria.Ativo = !categoria.Ativo;
                    await _context.SaveChangesAsync();

                    string status = categoria.Ativo ? "ativada" : "desativada";
                    TempData["SuccessMessage"] = $"Categoria {status} com sucesso!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar status da categoria");
                TempData["ErrorMessage"] = "Erro ao alterar o status da categoria.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoriaExists(int id)
        {
            return _context.Categorias.Any(e => e.Id == id);
        }
    }
}

