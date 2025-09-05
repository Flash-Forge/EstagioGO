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
        public async Task<IActionResult> Create()
        {
            logger.LogInformation("Carregando página de criação de avaliação");

            try
            {
                // Carregar categorias e competências ativas
                var categorias = await context.Categorias
                    .Include(c => c.Competencias)
                    .Where(c => c.Ativo)
                    .OrderBy(c => c.OrdemExibicao)
                    .ToListAsync();

                logger.LogInformation("Encontradas {Count} categorias ativas", categorias.Count);

                var viewModel = new AvaliacaoViewModel
                {
                    Categorias = [.. categorias.Select(c => new CategoriaAvaliacaoViewModel
                    {
                        CategoriaId = c.Id,
                        Nome = c.Nome,
                        Descricao = c.Descricao,
                        Competencias = [.. c.Competencias
                            .Where(comp => comp.Ativo)
                            .OrderBy(comp => comp.OrdemExibicao)
                            .Select(comp => new CompetenciaAvaliacaoViewModel
                            {
                                CompetenciaId = comp.Id,
                                Descricao = comp.Descricao,
                                Nota = 0 // Valor padrão
                            })]
                    })]
                };

                // Carregar estagiários ativos para o dropdown
                var estagiarios = await context.Estagiarios
                    .Where(e => e.Ativo)
                    .OrderBy(e => e.Nome)
                    .ToListAsync();

                logger.LogInformation("Encontrados {Count} estagiários ativos", estagiarios.Count);

                ViewBag.Estagiarios = new SelectList(estagiarios, "Id", "Nome");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao carregar página de criação de avaliação");
                TempData["ErrorMessage"] = "Erro ao carregar o formulário de avaliação.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Avaliacao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AvaliacaoViewModel viewModel)
        {
            logger.LogInformation("Iniciando processamento de nova avaliação");

            if (ModelState.IsValid)
            {
                try
                {
                    logger.LogInformation("ModelState é válido");

                    // Obter o ID do usuário logado
                    var user = await userManager.GetUserAsync(User);
                    var avaliadorId = user?.Id;

                    if (string.IsNullOrEmpty(avaliadorId))
                    {
                        logger.LogError("Não foi possível obter o ID do usuário logado");
                        ModelState.AddModelError("", "Não foi possível identificar o avaliador.");
                        return View(viewModel);
                    }

                    logger.LogInformation("Avaliador ID: {AvaliadorId}", avaliadorId);
                    logger.LogInformation("Estagiário ID selecionado: {EstagiarioId}", viewModel.EstagiarioId);

                    // Verificar se o estagiário existe
                    var estagiario = await context.Estagiarios
                        .FirstOrDefaultAsync(e => e.Id == viewModel.EstagiarioId && e.Ativo);

                    if (estagiario == null)
                    {
                        logger.LogError("Estagiário com ID {EstagiarioId} não encontrado ou inativo", viewModel.EstagiarioId);
                        ModelState.AddModelError("EstagiarioId", "Estagiário não encontrado ou inativo.");
                        return View(viewModel);
                    }

                    // Criar a avaliação
                    var avaliacao = new Avaliacao
                    {
                        EstagiarioId = viewModel.EstagiarioId,
                        AvaliadorId = avaliadorId,
                        DataAvaliacao = DateTime.Now,
                        ComentariosGerais = viewModel.ComentariosGerais,
                        MediaNotas = 0 // Será calculada abaixo
                    };

                    // Adicionar as competências avaliadas
                    decimal somaNotas = 0;
                    int totalCompetencias = 0;

                    logger.LogInformation("Processando {TotalCompetencias} competências", viewModel.Categorias.Sum(c => c.Competencias.Count));

                    foreach (var categoria in viewModel.Categorias)
                    {
                        foreach (var competencia in categoria.Competencias)
                        {
                            logger.LogDebug("Competência ID: {CompetenciaId}, Nota: {Nota}", competencia.CompetenciaId, competencia.Nota);

                            var avaliacaoCompetencia = new AvaliacaoCompetencia
                            {
                                CompetenciaId = competencia.CompetenciaId,
                                Nota = competencia.Nota,
                                Comentario = competencia.Comentario
                            };

                            avaliacao.CompetenciasAvaliadas.Add(avaliacaoCompetencia);

                            // Somar notas para cálculo da média
                            somaNotas += competencia.Nota;
                            totalCompetencias++;
                        }
                    }

                    logger.LogInformation("Total de competências processadas: {TotalCompetencias}, Soma das notas: {SomaNotas}", totalCompetencias, somaNotas);

                    // Calcular a média das notas (0-5)
                    if (totalCompetencias > 0)
                    {
                        avaliacao.MediaNotas = Math.Round(somaNotas / totalCompetencias, 2);
                        logger.LogInformation("Média calculada: {MediaNotas}", avaliacao.MediaNotas);
                    }
                    else
                    {
                        logger.LogWarning("Nenhuma competência foi avaliada");
                    }

                    context.Add(avaliacao);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Avaliação salva com sucesso no banco de dados");

                    TempData["SuccessMessage"] = $"Avaliação registrada com sucesso! Média: {avaliacao.MediaNotas}/5";
                    return RedirectToAction(nameof(Index), "Home");
                }
                catch (DbUpdateException dbEx)
                {
                    logger.LogError(dbEx, "Erro de banco de dados ao salvar avaliação");
                    ModelState.AddModelError("", "Erro ao salvar a avaliação no banco de dados. Verifique os dados e tente novamente.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro inesperado ao salvar avaliação");
                    ModelState.AddModelError("", "Ocorreu um erro inesperado ao salvar a avaliação: " + ex.Message);
                }
            }
            else
            {
                logger.LogWarning("ModelState é inválido");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        logger.LogWarning("Erro no campo {FieldName}: {ErrorMessages}",
                            error.Key,
                            string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }
            }

            // Se houver erro, recarregar os dados necessários para a view
            try
            {
                var estagiarios = await context.Estagiarios
                    .Where(e => e.Ativo)
                    .OrderBy(e => e.Nome)
                    .ToListAsync();

                ViewBag.Estagiarios = new SelectList(estagiarios, "Id", "Nome", viewModel.EstagiarioId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao recarregar lista de estagiários");
                TempData["ErrorMessage"] = "Erro ao recarregar o formulário.";
                return RedirectToAction("Index", "Home");
            }

            return View(viewModel);
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Dashboard");
        }
    }
}