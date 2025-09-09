using EstagioGO.Data;
using EstagioGO.Models.Analise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Supervisor,Coordenador,Administrador")]
    public class DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger) : Controller
    {
        public async Task<IActionResult> Index(int? estagiarioId)
        {
            try
            {
                var viewModel = new DashboardViewModel
                {
                    EstagiarioSelecionadoId = estagiarioId
                };

                // Carregar estagiários para o dropdown de filtro
                var estagiariosDropdown = await context.Estagiarios
                    .Where(e => e.Ativo)
                    .OrderBy(e => e.Nome)
                    .ToListAsync();

                ViewBag.Estagiarios = new SelectList(estagiariosDropdown, "Id", "Nome", estagiarioId);

                // KPIs
                viewModel.TotalEstagiariosAtivos = estagiariosDropdown.Count;
                viewModel.AvaliacoesPendentes = await CalcularAvaliacoesPendentes();
                viewModel.MediaDesempenhoGeral = await CalcularMediaDesempenhoGeral();
                viewModel.EstagiariosEmRisco = await CalcularEstagiariosEmRisco();

                // Dados para gráficos
                viewModel.MediasCategorias = await ObterMediasPorCategoria();
                viewModel.EvolucaoDesempenho = await ObterEvolucaoDesempenho();
                viewModel.MapeamentoTalentos = await ObterMapeamentoTalentos();
                viewModel.Estagiarios = await ObterListaEstagiarios();
                viewModel.DadosFrequencia = await ObterDadosFrequencia(estagiarioId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao carregar dashboard");
                TempData["ErrorMessage"] = "Erro ao carregar o dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<int> CalcularAvaliacoesPendentes()
        {
            // Lógica para calcular avaliações pendentes
            var umMesAtras = DateTime.Now.AddMonths(-1);
            var estagiariosAtivos = await context.Estagiarios
                .Where(e => e.Ativo)
                .ToListAsync();

            int pendentes = 0;

            foreach (var estagiario in estagiariosAtivos)
            {
                var ultimaAvaliacao = await context.Avaliacoes
                    .Where(a => a.EstagiarioId == estagiario.Id)
                    .OrderByDescending(a => a.DataAvaliacao)
                    .FirstOrDefaultAsync();

                if (ultimaAvaliacao == null || ultimaAvaliacao.DataAvaliacao < umMesAtras)
                {
                    pendentes++;
                }
            }

            return pendentes;
        }

        private async Task<decimal> CalcularMediaDesempenhoGeral()
        {
            var media = await context.Avaliacoes
                .Where(a => a.Estagiario.Ativo)
                .AverageAsync(a => (decimal?)a.MediaNotas) ?? 0;

            return Math.Round(media, 2);
        }

        private async Task<int> CalcularEstagiariosEmRisco()
        {
            // Estagiários com média abaixo de 3
            var estagiariosEmRisco = await context.Avaliacoes
                .Where(a => a.Estagiario.Ativo)
                .GroupBy(a => a.EstagiarioId)
                .Select(g => new
                {
                    EstagiarioId = g.Key,
                    Media = g.Average(a => a.MediaNotas)
                })
                .Where(x => x.Media < 3)
                .CountAsync();

            return estagiariosEmRisco;
        }

        private async Task<List<MediaCategoriaViewModel>> ObterMediasPorCategoria()
        {
            var medias = await context.AvaliacaoCompetencias
                .Include(ac => ac.Competencia)
                .ThenInclude(c => c.Categoria)
                .GroupBy(ac => ac.Competencia.Categoria.Nome)
                .Select(g => new MediaCategoriaViewModel
                {
                    Categoria = g.Key,
                    Media = (decimal)g.Average(ac => ac.Nota)
                })
                .ToListAsync();

            return medias;
        }

        private async Task<List<EvolucaoDesempenhoViewModel>> ObterEvolucaoDesempenho()
        {
            // Primeiro, obtemos os dados agrupados do banco de dados sem a formatação de string
            var dados = await context.Avaliacoes
                .Where(a => a.DataAvaliacao >= DateTime.Now.AddMonths(-6))
                .GroupBy(a => new { a.DataAvaliacao.Year, a.DataAvaliacao.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Media = g.Average(a => a.MediaNotas)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Agora, formatamos os períodos em memória (client-side)
            var ultimosMeses = dados.Select(g => new EvolucaoDesempenhoViewModel
            {
                Periodo = $"{g.Month:00}/{g.Year}",
                Media = (decimal)g.Media
            }).ToList();

            return ultimosMeses;
        }

        private async Task<List<MapeamentoTalentoViewModel>> ObterMapeamentoTalentos()
        {
            var talentos = await context.AvaliacaoCompetencias
                .Include(ac => ac.Avaliacao)
                .ThenInclude(a => a.Estagiario)
                .Include(ac => ac.Competencia)
                .ThenInclude(c => c.Categoria)
                .GroupBy(ac => new { ac.Avaliacao.EstagiarioId, ac.Avaliacao.Estagiario.Nome })
                .Select(g => new
                {
                    g.Key.Nome,
                    MediaHabilidadesTecnicas = g.Where(ac => ac.Competencia.Categoria.Nome == "Conhecimento Técnico")
                                                .Average(ac => (double?)ac.Nota) ?? 0,
                    MediaHabilidadesComportamentais = g.Where(ac => ac.Competencia.Categoria.Nome == "Comunicação" ||
                                                                    ac.Competencia.Categoria.Nome == "Trabalho em Equipe")
                                                       .Average(ac => (double?)ac.Nota) ?? 0
                })
                .ToListAsync();

            return [.. talentos.Select(t => new MapeamentoTalentoViewModel
            {
                Estagiario = t.Nome,
                MediaHabilidadesTecnicas = (decimal)t.MediaHabilidadesTecnicas,
                MediaHabilidadesComportamentais = (decimal)t.MediaHabilidadesComportamentais
            })];
        }

        private async Task<List<EstagiarioResumoViewModel>> ObterListaEstagiarios()
        {
            try
            {
                var estagiarios = await context.Estagiarios
                    .Include(e => e.Supervisor)
                    .Include(e => e.Avaliacoes)
                    .Where(e => e.Ativo)
                    .Select(e => new
                    {
                        Estagiario = e,
                        UltimaAvaliacao = e.Avaliacoes
                            .OrderByDescending(a => a.DataAvaliacao)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var resultado = estagiarios.Select(x => new EstagiarioResumoViewModel
                {
                    Id = x.Estagiario.Id,
                    Nome = x.Estagiario.Nome,
                    Supervisor = x.Estagiario.Supervisor != null ? x.Estagiario.Supervisor.NomeCompleto : "Não definido",
                    Curso = x.Estagiario.Curso,
                    Instituicao = x.Estagiario.InstituicaoEnsino,
                    UltimaNota = x.UltimaAvaliacao != null ? x.UltimaAvaliacao.MediaNotas : 0,
                    UltimaAvaliacao = x.UltimaAvaliacao?.DataAvaliacao,
                    Status = x.UltimaAvaliacao != null ?
                        (x.UltimaAvaliacao.MediaNotas >= 3 ? "Estável" : "Em Risco") :
                        "Sem Avaliação"
                })
                .OrderByDescending(e => e.UltimaNota)
                .ToList();

                logger.LogInformation("Encontrados {Count} estagiários para a lista", resultado.Count);
                return resultado;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao obter lista de estagiários");
                return [];
            }
        }

        public async Task<IActionResult> Historico(int id)
        {
            try
            {
                var estagiario = await context.Estagiarios
                    .Include(e => e.Avaliacoes)
                        .ThenInclude(a => a.Avaliador)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (estagiario == null)
                {
                    logger.LogWarning("Tentativa de acessar histórico para estagiário não encontrado. ID: {Id}", id);
                    return NotFound();
                }

                // Ordena as avaliações da mais recente para a mais antiga para melhor visualização
                estagiario.Avaliacoes = [.. estagiario.Avaliacoes.OrderByDescending(a => a.DataAvaliacao)];

                // Retorna a nova view que iremos criar, passando o estagiário com suas avaliações
                return View(estagiario);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao carregar histórico do estagiário. ID: {Id}", id);
                TempData["ErrorMessage"] = "Erro ao carregar o histórico de avaliações.";
                return RedirectToAction("Index");
            }
        }

        private async Task<List<FrequenciaStatusViewModel>> ObterDadosFrequencia(int? estagiarioId)
        {
            var query = context.Frequencias.AsQueryable();

            if (estagiarioId.HasValue)
            {
                query = query.Where(f => f.EstagiarioId == estagiarioId.Value);
            }

            var dados = await query
                .GroupBy(f => f.Presente)
                .Select(g => new
                {
                    Status = g.Key,
                    Quantidade = g.Count()
                })
                .ToListAsync();

            // Transforma o resultado (true/false) em um formato legível para o gráfico
            var resultado = new List<FrequenciaStatusViewModel>();
            var presencas = dados.FirstOrDefault(d => d.Status);
            var faltas = dados.FirstOrDefault(d => !d.Status);

            resultado.Add(new FrequenciaStatusViewModel { Status = "Presenças", Quantidade = presencas?.Quantidade ?? 0 });
            resultado.Add(new FrequenciaStatusViewModel { Status = "Faltas", Quantidade = faltas?.Quantidade ?? 0 });

            return resultado;
        }
    }
}