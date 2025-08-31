using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstagioGO.Models.Analise
{
    public class AvaliacaoCompetencia
    {
        public int Id { get; set; }

        [Required]
        public int AvaliacaoId { get; set; }

        [ForeignKey("AvaliacaoId")]
        public virtual Avaliacao Avaliacao { get; set; }

        [Required]
        public int CompetenciaId { get; set; }

        [ForeignKey("CompetenciaId")]
        public virtual Competencia Competencia { get; set; }

        [Required(ErrorMessage = "A nota é obrigatória")]
        [Range(0, 5, ErrorMessage = "A nota deve estar entre 0 e 5")]
        public int Nota { get; set; }

        [StringLength(500, ErrorMessage = "O comentário não pode ter mais de 500 caracteres")]
        public string? Comentario { get; set; }
    }
}