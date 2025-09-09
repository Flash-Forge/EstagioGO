using EstagioGO.Data;
using EstagioGO.Models.Analise;
using EstagioGO.Models.Analise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Supervisor,Coordenador,Administrador")]
    public class AvaliacaoController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<AvaliacaoController> logger) : Controller
    {
        // GET: Avaliacao/Create
        public async Task<IActionResult> Create(int? estagiarioId)
        {
            // VALIDAÇÃO ADICIONADA AQUI
            var existemEstagiariosAtivos = await context.Estagiarios.AnyAsync(e => e.Ativo);
            if (!existemEstagiariosAtivos)
            {
                TempData["ErrorMessage"] = "Não é possível criar uma avaliação, pois não há estagiários ativos cadastrados.";
                return RedirectToAction("Index", "Estagiarios");
            }

            var viewModel = await BuildAvaliacaoViewModel(new AvaliacaoViewModel { EstagiarioId = estagiarioId ?? 0 });
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Erro ao carregar o formulário de avaliação.";
                return RedirectToAction("Index", "Home");
            }

            return View(viewModel);
        }


        // POST: Avaliacao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AvaliacaoViewModel viewModel)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "Não foi possível identificar o avaliador.");
            }

            if (ModelState.IsValid)
            {
                var estagiario = await context.Estagiarios
                    .FirstOrDefaultAsync(e => e.Id == viewModel.EstagiarioId && e.Ativo);

                if (estagiario == null)
                {
                    ModelState.AddModelError("EstagiarioId", "Estagiário não encontrado ou inativo.");
                }
                else
                {
                    var hoje = DateTime.UtcNow.Date;
                    bool avaliacaoJaExiste = await context.Avaliacoes
                        .AnyAsync(a => a.EstagiarioId == viewModel.EstagiarioId && a.DataAvaliacao.Date == hoje);

                    if (avaliacaoJaExiste)
                    {
                        ModelState.AddModelError("", "Já existe uma avaliação registrada para este estagiário hoje. Não é possível criar avaliações duplicadas no mesmo dia.");
                    }
                    else
                    {
                        var avaliacao = new Avaliacao
                        {
                            EstagiarioId = viewModel.EstagiarioId,
                            AvaliadorId = user!.Id,
                            DataAvaliacao = DateTime.Now,
                            ComentariosGerais = viewModel.ComentariosGerais
                        };

                        var competenciasAvaliadas = viewModel.Categorias
                            .SelectMany(c => c.Competencias)
                            .Select(comp => new AvaliacaoCompetencia
                            {
                                CompetenciaId = comp.CompetenciaId,
                                Nota = comp.Nota,
                                Comentario = comp.Comentario
                            }).ToList();

                        avaliacao.CompetenciasAvaliadas = competenciasAvaliadas;

                        if (competenciasAvaliadas.Count != 0)
                        {
                            avaliacao.MediaNotas = (decimal)Math.Round(competenciasAvaliadas.Average(c => c.Nota), 2);
                        }

                        context.Add(avaliacao);
                        await context.SaveChangesAsync();

                        TempData["SuccessMessage"] = $"Avaliação registrada com sucesso! Média: {avaliacao.MediaNotas}/5";
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            // Linha de erro
            logger.LogWarning("ModelState inválido ao criar avaliação. Recarregando dados do formulário.");
            var repopulatedViewModel = await BuildAvaliacaoViewModel(viewModel);
            return View(repopulatedViewModel);
        }

        // Método auxiliar para construir ViewModel
        private async Task<AvaliacaoViewModel?> BuildAvaliacaoViewModel(AvaliacaoViewModel existingViewModel)
        {
            try
            {
                // CONSULTA OTIMIZADA: Filtra as competências ativas diretamente no banco
                var categoriasComCompetenciasAtivas = await context.Categorias
                    .Where(c => c.Ativo)
                    .OrderBy(c => c.OrdemExibicao)
                    .Select(c => new
                    {
                        c.Id,
                        c.Nome,
                        c.Descricao,
                        Competencias = c.Competencias
                                        .Where(comp => comp.Ativo)
                                        .OrderBy(comp => comp.OrdemExibicao)
                                        .ToList()
                    })
                    .ToListAsync();

                // Mapeia para o ViewModel
                existingViewModel.Categorias = [.. categoriasComCompetenciasAtivas.Select(c => new CategoriaAvaliacaoViewModel
                {
                    CategoriaId = c.Id,
                    Nome = c.Nome,
                    Descricao = c.Descricao,
                    Competencias = [.. c.Competencias.Select(comp =>
                    {
                        // Se já houver dados no viewModel (caso de erro), preserva-os
                        var existingCompetencia = existingViewModel.Categorias?
                            .SelectMany(cat => cat.Competencias)
                            .FirstOrDefault(ec => ec.CompetenciaId == comp.Id);

                        return new CompetenciaAvaliacaoViewModel
                        {
                            CompetenciaId = comp.Id,
                            Descricao = comp.Descricao,
                            Nota = existingCompetencia?.Nota ?? 0,
                            Comentario = existingCompetencia?.Comentario
                        };
                    })]
                })];

                var estagiarios = await context.Estagiarios
                    .Where(e => e.Ativo)
                    .OrderBy(e => e.Nome)
                    .Select(e => new { e.Id, e.Nome })
                    .ToListAsync();

                ViewBag.Estagiarios = new SelectList(estagiarios, "Id", "Nome", existingViewModel.EstagiarioId);

                return existingViewModel;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao construir AvaliacaoViewModel.");
                return null;
            }
        }
    }
}