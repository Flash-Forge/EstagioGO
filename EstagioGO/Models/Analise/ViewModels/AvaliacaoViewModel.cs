using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Models.Analise.ViewModels
{
    public class AvaliacaoViewModel
    {
        public int AvaliacaoId { get; set; }
        public string? EstagiarioNome { get; set; }
        [Required(ErrorMessage = "O estagiário é obrigatório")]
        [Display(Name = "Estagiário")]
        public int EstagiarioId { get; set; }

        [StringLength(2000, ErrorMessage = "Os comentários não podem ter mais de 2000 caracteres")]
        [Display(Name = "Comentários Gerais")]
        public string? ComentariosGerais { get; set; }

        public List<CategoriaAvaliacaoViewModel> Categorias { get; set; } = [];
    }

    public class CategoriaAvaliacaoViewModel
    {
        public int CategoriaId { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public List<CompetenciaAvaliacaoViewModel> Competencias { get; set; } = [];
    }

    public class CompetenciaAvaliacaoViewModel
    {
        public int CompetenciaId { get; set; }
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "A nota é obrigatória")]
        [Range(0, 5, ErrorMessage = "A nota deve estar entre 0 e 5")]
        [Display(Name = "Nota")]
        public int Nota { get; set; }

        [StringLength(500, ErrorMessage = "O comentário não pode ter mais de 500 caracteres")]
        [Display(Name = "Comentário")]
        public string? Comentario { get; set; }
    }
}