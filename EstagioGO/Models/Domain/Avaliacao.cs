using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstagioGO.Models.Domain
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

        [Required(ErrorMessage = "A nota é obrigatória")]
        [Range(0, 10, ErrorMessage = "A nota deve estar entre 0 e 10")]
        public int Nota { get; set; }

        [StringLength(2000, ErrorMessage = "Os comentários não podem ter mais de 2000 caracteres")]
        public string Comentarios { get; set; }

        // Relacionamentos com itens de avaliação
        public ICollection<ItemAvaliacao> ItensAvaliacao { get; set; } = new List<ItemAvaliacao>();

        // Propriedade de navegação para o período de avaliação (se aplicável)
        public int? PeriodoAvaliacaoId { get; set; }

        [ForeignKey("PeriodoAvaliacaoId")]
        public PeriodoAvaliacao PeriodoAvaliacao { get; set; }
    }
}