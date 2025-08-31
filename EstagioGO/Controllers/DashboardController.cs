using EstagioGO.Data;
using EstagioGO.Models.Analise;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Supervisor,Coordenador,Administrador")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? estagiarioId)
        {
            try
            {
                var viewModel = new DashboardViewModel
                {
                    EstagiarioSelecionadoId = estagiarioId
                };

                // KPIs
                viewModel.TotalEstagiariosAtivos = await _context.Estagiarios.CountAsync(e => e.Ativo);
                viewModel.AvaliacoesPendentes = await CalcularAvaliacoesPendentes();
                viewModel.MediaDesempenhoGeral = await CalcularMediaDesempenhoGeral();
                viewModel.EstagiariosEmRisco = await CalcularEstagiariosEmRisco();

                // Dados para gráficos
                viewModel.MediasCategorias = await ObterMediasPorCategoria();
                viewModel.EvolucaoDesempenho = await ObterEvolucaoDesempenho();
                viewModel.MapeamentoTalentos = await ObterMapeamentoTalentos();
                viewModel.Estagiarios = await ObterListaEstagiarios();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard");
                TempData["ErrorMessage"] = "Erro ao carregar o dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<int> CalcularAvaliacoesPendentes()
        {
            // Lógica para calcular avaliações pendentes
            var umMesAtras = DateTime.Now.AddMonths(-1);
            var estagiariosAtivos = await _context.Estagiarios
                .Where(e => e.Ativo)
                .ToListAsync();

            int pendentes = 0;

            foreach (var estagiario in estagiariosAtivos)
            {
                var ultimaAvaliacao = await _context.Avaliacoes
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
            var media = await _context.Avaliacoes
                .Where(a => a.Estagiario.Ativo)
                .AverageAsync(a => (decimal?)a.MediaNotas) ?? 0;

            return Math.Round(media, 2);
        }

        private async Task<int> CalcularEstagiariosEmRisco()
        {
            // Estagiários com média abaixo de 3
            var estagiariosEmRisco = await _context.Avaliacoes
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
            var medias = await _context.AvaliacaoCompetencias
                .Include(ac => ac.Competencia)
                .ThenInclude(c => c.Categoria)
                .GroupBy(ac => ac.Competencia.Categoria.Nome)
                .Select(g => new MediaCategoriaViewModel
                {
                    Categoria = g.Key,
                    Media = (decimal)g.Average(ac => ac.Nota) // Conversão explícita para decimal
                })
                .ToListAsync();

            return medias;
        }

        private async Task<List<EvolucaoDesempenhoViewModel>> ObterEvolucaoDesempenho()
        {
            var ultimosMeses = await _context.Avaliacoes
                .Where(a => a.DataAvaliacao >= DateTime.Now.AddMonths(-6))
                .GroupBy(a => new { a.DataAvaliacao.Year, a.DataAvaliacao.Month })
                .Select(g => new EvolucaoDesempenhoViewModel
                {
                    Periodo = $"{g.Key.Month}/{g.Key.Year}",
                    Media = (decimal)g.Average(a => a.MediaNotas) // Conversão explícita para decimal
                })
                .OrderBy(x => x.Periodo)
                .ToListAsync();

            return ultimosMeses;
        }

        private async Task<List<MapeamentoTalentoViewModel>> ObterMapeamentoTalentos()
        {
            var talentos = await _context.AvaliacaoCompetencias
                .Include(ac => ac.Avaliacao)
                .ThenInclude(a => a.Estagiario)
                .Include(ac => ac.Competencia)
                .ThenInclude(c => c.Categoria)
                .GroupBy(ac => new { ac.Avaliacao.EstagiarioId, ac.Avaliacao.Estagiario.Nome })
                .Select(g => new MapeamentoTalentoViewModel
                {
                    Estagiario = g.Key.Nome,
                    MediaHabilidadesTecnicas = (decimal)g.Where(ac => ac.Competencia.Categoria.Nome == "Conhecimento Técnico").Average(ac => ac.Nota), // Conversão explícita
                    MediaHabilidadesComportamentais = (decimal)g.Where(ac => ac.Competencia.Categoria.Nome == "Comunicação" ||
                                                                   ac.Competencia.Categoria.Nome == "Trabalho em Equipe").Average(ac => ac.Nota) // Conversão explícita
                })
                .ToListAsync();

            return talentos;
        }

        private async Task<List<EstagiarioResumoViewModel>> ObterListaEstagiarios()
        {
            var estagiarios = await _context.Estagiarios
                .Include(e => e.Supervisor)
                .Include(e => e.Avaliacoes) // Agora esta propriedade existe
                .Where(e => e.Ativo)
                .Select(e => new EstagiarioResumoViewModel
                {
                    Id = e.Id,
                    Nome = e.Nome,
                    Supervisor = e.Supervisor.NomeCompleto,
                    Curso = e.Curso,
                    Instituicao = e.InstituicaoEnsino,
                    UltimaNota = e.Avaliacoes.OrderByDescending(a => a.DataAvaliacao).FirstOrDefault() != null ?
                        e.Avaliacoes.OrderByDescending(a => a.DataAvaliacao).FirstOrDefault().MediaNotas : 0,
                    UltimaAvaliacao = e.Avaliacoes.OrderByDescending(a => a.DataAvaliacao).FirstOrDefault() != null ?
                        e.Avaliacoes.OrderByDescending(a => a.DataAvaliacao).FirstOrDefault().DataAvaliacao : (DateTime?)null,
                    Status = e.Avaliacoes.Any() ?
                        (e.Avaliacoes.OrderByDescending(a => a.DataAvaliacao).FirstOrDefault().MediaNotas >= 3 ? "Estável" : "Em Risco") :
                        "Sem Avaliação"
                })
                .OrderByDescending(e => e.UltimaNota)
                .ToListAsync();

            return estagiarios;
        }
    }
}