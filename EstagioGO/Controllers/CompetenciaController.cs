using EstagioGO.Data;
using EstagioGO.Models.Analise;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Administrador,Coordenador")]
    public class CompetenciaController(ApplicationDbContext context, ILogger<CompetenciaController> logger) : Controller
    {

        // GET: Competencia
        public async Task<IActionResult> Index(int? categoriaId)
        {
            try
            {
                var competenciasQuery = context.Competencias
                    .Include(c => c.Categoria)
                    .AsQueryable();

                if (categoriaId.HasValue)
                {
                    competenciasQuery = competenciasQuery.Where(c => c.CategoriaId == categoriaId.Value);
                }

                var competencias = await competenciasQuery
                    .OrderBy(c => c.Categoria.OrdemExibicao)
                    .ThenBy(c => c.OrdemExibicao)
                    .ThenBy(c => c.Descricao)
                    .ToListAsync();

                // Carregar categorias para o filtro
                var categorias = await context.Categorias
                    .Where(c => c.Ativo)
                    .OrderBy(c => c.OrdemExibicao)
                    .ToListAsync();

                ViewBag.Categorias = new SelectList(categorias, "Id", "Nome", categoriaId);
                ViewBag.CategoriaIdSelecionada = categoriaId;

                return View(competencias);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao carregar lista de competências");
                TempData["ErrorMessage"] = "Erro ao carregar as competências.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Competencia/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competencia = await context.Competencias
                .Include(c => c.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (competencia == null)
            {
                return NotFound();
            }

            return View(competencia);
        }

        // GET: Competencia/Create
        public async Task<IActionResult> Create(int? categoriaId)
        {
            await CarregarCategoriasDropdown(categoriaId);

            var competencia = new Competencia();
            if (categoriaId.HasValue)
            {
                competencia.CategoriaId = categoriaId.Value;
            }

            return View(competencia);
        }

        // POST: Competencia/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Competencia competencia)
        {
            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                try
                {
                    context.Add(competencia);
                    await context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Competência criada com sucesso!";
                    return RedirectToAction(nameof(Index), new { categoriaId = competencia.CategoriaId });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao criar competência");
                    ModelState.AddModelError("", "Erro ao salvar a competência. Tente novamente.");
                }
            }

            // Se houver erro, recarrega o dropdown de categorias.
            await CarregarCategoriasDropdown(competencia.CategoriaId);
            return View(competencia);
        }

        // GET: Competencia/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competencia = await context.Competencias.FindAsync(id);
            if (competencia == null)
            {
                return NotFound();
            }

            await CarregarCategoriasDropdown(competencia.CategoriaId);
            return View(competencia);
        }

        // POST: Competencia/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Descricao,CategoriaId,OrdemExibicao,Ativo")] Competencia competencia)
        {
            if (id != competencia.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Categoria");

            if (ModelState.IsValid)
            {
                try
                {
                    context.Update(competencia);
                    await context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Competência atualizada com sucesso!";
                    return RedirectToAction(nameof(Index), new { categoriaId = competencia.CategoriaId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompetenciaExists(competencia.Id))
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
                    logger.LogError(ex, "Erro ao atualizar competência");
                    ModelState.AddModelError("", "Erro ao atualizar a competência. Tente novamente.");
                }
            }

            await CarregarCategoriasDropdown(competencia.CategoriaId);
            return View(competencia);
        }

        // GET: Competencia/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competencia = await context.Competencias
                .Include(c => c.Categoria)
                .Include(c => c.AvaliacoesCompetencia)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (competencia == null)
            {
                return NotFound();
            }

            return View(competencia);
        }

        // POST: Competencia/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var competencia = await context.Competencias
                    .Include(c => c.AvaliacoesCompetencia)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (competencia != null)
                {
                    // Verificar se há avaliações associadas
                    if (competencia.AvaliacoesCompetencia.Count != 0)
                    {
                        TempData["ErrorMessage"] = "Não é possível excluir uma competência que possui avaliações associadas. Desative-a primeiro.";
                        return RedirectToAction(nameof(Index), new { categoriaId = competencia.CategoriaId });
                    }

                    var categoriaId = competencia.CategoriaId;
                    context.Competencias.Remove(competencia);
                    await context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Competência excluída com sucesso!";
                    return RedirectToAction(nameof(Index), new { categoriaId });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao excluir competência");
                TempData["ErrorMessage"] = "Erro ao excluir a competência. Tente novamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Competencia/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var competencia = await context.Competencias.FindAsync(id);
                if (competencia != null)
                {
                    competencia.Ativo = !competencia.Ativo;
                    await context.SaveChangesAsync();

                    string status = competencia.Ativo ? "ativada" : "desativada";
                    TempData["SuccessMessage"] = $"Competência {status} com sucesso!";
                    return RedirectToAction(nameof(Index), new { categoriaId = competencia.CategoriaId });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao alterar status da competência");
                TempData["ErrorMessage"] = "Erro ao alterar o status da competência.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CarregarCategoriasDropdown(int? categoriaIdSelecionada = null)
        {
            var categorias = await context.Categorias
                .Where(c => c.Ativo)
                .OrderBy(c => c.OrdemExibicao)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            ViewBag.CategoriaId = new SelectList(categorias, "Id", "Nome", categoriaIdSelecionada);
        }

        private bool CompetenciaExists(int id)
        {
            return context.Competencias.Any(e => e.Id == id);
        }
    }
}

