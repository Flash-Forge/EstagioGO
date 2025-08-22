using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Models.Domain
{
    public class PeriodoAvaliacao
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } // "1º Bimestre", "Avaliação Final"

        [Required]
        [DataType(DataType.Date)]
        public DateTime DataInicio { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DataFim { get; set; }

        [StringLength(500)]
        public string Descricao { get; set; }

        public bool Ativo { get; set; } = true;

        // Relacionamento
        public ICollection<Avaliacao> Avaliacoes { get; set; } = new List<Avaliacao>();
    }
}
