using EstagioGO.Models.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstagioGO.Models.Analise
{
    public class Avaliacao
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O estagiário é obrigatório")]
        public int EstagiarioId { get; set; }

        [ForeignKey("EstagiarioId")]
        public Estagiario Estagiario { get; set; }

        [Required(ErrorMessage = "O avaliador é obrigatório")]
        [Display(Name = "Avaliador")]
        public string AvaliadorId { get; set; }

        [ForeignKey("AvaliadorId")]
        public ApplicationUser Avaliador { get; set; }

        [Required(ErrorMessage = "A data da avaliação é obrigatória")]
        [Display(Name = "Data da Avaliação")]
        [DataType(DataType.Date)]
        public DateTime DataAvaliacao { get; set; } = DateTime.Now;

        [StringLength(2000, ErrorMessage = "Os comentários não podem ter mais de 2000 caracteres")]
        public string? ComentariosGerais { get; set; }

        // Média das notas (0-5)
        [Range(0, 5)]
        [Column(TypeName = "decimal(3,2)")]
        public decimal MediaNotas { get; set; }

        // Relacionamento com as competências avaliadas
        public virtual ICollection<AvaliacaoCompetencia> CompetenciasAvaliadas { get; set; } = new HashSet<AvaliacaoCompetencia>();
    }
}