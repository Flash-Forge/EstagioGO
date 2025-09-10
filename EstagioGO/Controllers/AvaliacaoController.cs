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
        // Dentro de AvaliacaoController.cs

        // GET: Avaliacao/Edit/5
        [Authorize(Roles = "Administrador,Coordenador")] // Apenas Admins e Coordenadores podem editar
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var avaliacao = await context.Avaliacoes
                .Include(a => a.Estagiario)
                .Include(a => a.CompetenciasAvaliadas)
                    .ThenInclude(ac => ac.Competencia)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (avaliacao == null)
            {
                return NotFound();
            }

            // Mapear a entidade 'Avaliacao' para o 'AvaliacaoViewModel' para preencher o formulário
            var viewModel = await BuildAvaliacaoViewModelParaEdicao(avaliacao);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Erro ao carregar a avaliação para edição.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View(viewModel);
        }

        // POST: Avaliacao/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Coordenador")]
        public async Task<IActionResult> Edit(int id, AvaliacaoViewModel viewModel)
        {
            if (id != viewModel.AvaliacaoId)
            {
                return NotFound();
            }

            // Buscamos a avaliação original do banco de dados para garantir que estamos editando a correta
            var avaliacaoParaAtualizar = await context.Avaliacoes
                .Include(a => a.CompetenciasAvaliadas)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (avaliacaoParaAtualizar == null)
            {
                ModelState.AddModelError("", "A avaliação que você está tentando editar não foi encontrada.");
            }

            if (ModelState.IsValid && avaliacaoParaAtualizar != null)
            {
                try
                {
                    var avaliadorAtual = await userManager.GetUserAsync(User);

                    // Atualiza os campos principais
                    avaliacaoParaAtualizar.ComentariosGerais = viewModel.ComentariosGerais;
                    avaliacaoParaAtualizar.AvaliadorId = avaliadorAtual!.Id; // Registra o último editor
                    avaliacaoParaAtualizar.DataAvaliacao = DateTime.Now; // Atualiza a data da edição

                    // Atualiza as notas e comentários das competências
                    foreach (var categoriaVM in viewModel.Categorias)
                    {
                        foreach (var competenciaVM in categoriaVM.Competencias)
                        {
                            var competenciaParaAtualizar = avaliacaoParaAtualizar.CompetenciasAvaliadas
                                .FirstOrDefault(ac => ac.CompetenciaId == competenciaVM.CompetenciaId);

                            if (competenciaParaAtualizar != null)
                            {
                                competenciaParaAtualizar.Nota = competenciaVM.Nota;
                                competenciaParaAtualizar.Comentario = competenciaVM.Comentario;
                            }
                        }
                    }

                    // Recalcula a média
                    if (avaliacaoParaAtualizar.CompetenciasAvaliadas.Any())
                    {
                        avaliacaoParaAtualizar.MediaNotas = (decimal)Math.Round(
                            avaliacaoParaAtualizar.CompetenciasAvaliadas.Average(c => c.Nota), 2);
                    }

                    context.Update(avaliacaoParaAtualizar);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Avaliação atualizada com sucesso!";
                    // Redireciona para o histórico do estagiário específico
                    return RedirectToAction("Historico", "Dashboard", new { id = avaliacaoParaAtualizar.EstagiarioId });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao atualizar a avaliação {AvaliacaoId}", id);
                    ModelState.AddModelError("", "Ocorreu um erro inesperado ao salvar as alterações.");
                }
            }

            // Se houver um erro, precisamos repopular o ViewModel com os dados para exibir o formulário novamente
            var repopulatedViewModel = await BuildAvaliacaoViewModelParaEdicao(avaliacaoParaAtualizar, viewModel);
            return View(repopulatedViewModel);
        }

        // Dentro de AvaliacaoController.cs

        // GET: Avaliacao/Delete/5
        [Authorize(Roles = "Administrador,Coordenador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var avaliacao = await context.Avaliacoes
                .Include(a => a.Estagiario)
                .Include(a => a.Avaliador)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (avaliacao == null)
            {
                return NotFound();
            }

            return View(avaliacao);
        }

        // POST: Avaliacao/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Coordenador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var avaliacao = await context.Avaliacoes.FindAsync(id);
            if (avaliacao == null)
            {
                TempData["ErrorMessage"] = "A avaliação que você tentou excluir não foi encontrada.";
                return RedirectToAction("Index", "Dashboard");
            }

            try
            {
                var estagiarioId = avaliacao.EstagiarioId;
                context.Avaliacoes.Remove(avaliacao);
                await context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Avaliação excluída com sucesso!";
                // Retorna para o histórico do estagiário de quem a avaliação foi excluída
                return RedirectToAction("Historico", "Dashboard", new { id = estagiarioId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao excluir a avaliação {AvaliacaoId}", id);
                TempData["ErrorMessage"] = "Ocorreu um erro ao excluir a avaliação.";
                return RedirectToAction("Historico", "Dashboard", new { id = avaliacao.EstagiarioId });
            }
        }

        // MÉTODO AUXILIAR NOVO: Para mapear uma Avaliacao existente para um ViewModel
        private async Task<AvaliacaoViewModel?> BuildAvaliacaoViewModelParaEdicao(Avaliacao avaliacao, AvaliacaoViewModel? postedViewModel = null)
        {
            // Reutilizamos o método que já existe para buscar a estrutura de categorias
            var baseViewModel = await BuildAvaliacaoViewModel(new AvaliacaoViewModel());
            if (baseViewModel == null) return null;

            var viewModel = new AvaliacaoViewModel
            {
                AvaliacaoId = avaliacao.Id,
                EstagiarioId = avaliacao.EstagiarioId,
                EstagiarioNome = avaliacao.Estagiario?.Nome,
                ComentariosGerais = postedViewModel?.ComentariosGerais ?? avaliacao.ComentariosGerais,
                Categorias = baseViewModel.Categorias
            };

            // Preenche as notas e comentários com os valores salvos no banco (ou do formulário com erro)
            foreach (var categoriaVM in viewModel.Categorias)
            {
                foreach (var competenciaVM in categoriaVM.Competencias)
                {
                    var competenciaSalva = avaliacao.CompetenciasAvaliadas
                        .FirstOrDefault(ac => ac.CompetenciaId == competenciaVM.CompetenciaId);

                    var competenciaPostada = postedViewModel?.Categorias
                        .SelectMany(c => c.Competencias)
                        .FirstOrDefault(c => c.CompetenciaId == competenciaVM.CompetenciaId);

                    if (competenciaSalva != null)
                    {
                        competenciaVM.Nota = competenciaPostada?.Nota ?? competenciaSalva.Nota;
                        competenciaVM.Comentario = competenciaPostada?.Comentario ?? competenciaSalva.Comentario;
                    }
                }
            }

            return viewModel;
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