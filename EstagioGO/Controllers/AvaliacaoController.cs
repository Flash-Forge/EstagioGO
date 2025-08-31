using EstagioGO.Data;
using EstagioGO.Models.Analise;
using EstagioGO.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Controllers
{
    [Authorize(Roles = "Supervisor,Coordenador,Administrador")]
    public class AvaliacaoController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {

        // GET: Avaliacao/Create
        public async Task<IActionResult> Create()
        {
            // Carregar categorias e competências ativas
            var categorias = await context.Categorias
                .Include(c => c.Competencias)
                .Where(c => c.Ativo)
                .OrderBy(c => c.OrdemExibicao)
                .ToListAsync();

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
            ViewBag.Estagiarios = new SelectList(
                await context.Estagiarios
                    .Where(e => e.Ativo)
                    .OrderBy(e => e.Nome)
                    .ToListAsync(),
                "Id", "Nome");

            return View(viewModel);
        }

        // POST: Avaliacao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AvaliacaoViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Obter o ID do usuário logado
                    var user = await userManager.GetUserAsync(User);
                    var avaliadorId = user.Id;

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

                    foreach (var categoria in viewModel.Categorias)
                    {
                        foreach (var competencia in categoria.Competencias)
                        {
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

                    // Calcular a média das notas (0-5)
                    if (totalCompetencias > 0)
                    {
                        avaliacao.MediaNotas = Math.Round(somaNotas / totalCompetencias, 2);
                    }

                    context.Add(avaliacao);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Avaliação registrada com sucesso! Média: {avaliacao.MediaNotas}/5";
                    return RedirectToAction(nameof(Index), "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocorreu um erro ao salvar a avaliação: " + ex.Message);
                }
            }

            // Se houver erro, recarregar os dados necessários para a view
            ViewBag.Estagiarios = new SelectList(
                await context.Estagiarios
                    .Where(e => e.Ativo)
                    .OrderBy(e => e.Nome)
                    .ToListAsync(),
                "Id", "Nome", viewModel.EstagiarioId);

            return View(viewModel);
        }
    }
}