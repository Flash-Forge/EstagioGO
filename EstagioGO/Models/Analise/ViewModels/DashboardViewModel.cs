namespace EstagioGO.Models.Analise.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalEstagiariosAtivos { get; set; }
        public int AvaliacoesPendentes { get; set; }
        public decimal MediaDesempenhoGeral { get; set; }
        public int EstagiariosEmRisco { get; set; }

        public List<MediaCategoriaViewModel> MediasCategorias { get; set; } = new List<MediaCategoriaViewModel>();
        public List<EvolucaoDesempenhoViewModel> EvolucaoDesempenho { get; set; } = new List<EvolucaoDesempenhoViewModel>();
        public List<MapeamentoTalentoViewModel> MapeamentoTalentos { get; set; } = new List<MapeamentoTalentoViewModel>();
        public List<EstagiarioResumoViewModel> Estagiarios { get; set; } = new List<EstagiarioResumoViewModel>();

        public int? EstagiarioSelecionadoId { get; set; }
    }

    public class MediaCategoriaViewModel
    {
        public string Categoria { get; set; }
        public decimal Media { get; set; }
    }

    public class EvolucaoDesempenhoViewModel
    {
        public string Periodo { get; set; }
        public decimal Media { get; set; }
    }

    public class MapeamentoTalentoViewModel
    {
        public string Estagiario { get; set; }
        public decimal MediaHabilidadesTecnicas { get; set; }
        public decimal MediaHabilidadesComportamentais { get; set; }
    }

    public class EstagiarioResumoViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Supervisor { get; set; }
        public string Curso { get; set; }
        public string Instituicao { get; set; }
        public decimal UltimaNota { get; set; }
        public DateTime? UltimaAvaliacao { get; set; }
        public string Status { get; set; }
    }
}