using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstagioGO.Models.Analise
{
    public class Competencia
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A descrição da competência é obrigatória")]
        [StringLength(200, ErrorMessage = "A descrição não pode ter mais de 200 caracteres")]
        public string? Descricao { get; set; }

        [Required]
        public int CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public virtual Categoria? Categoria { get; set; }

        public int OrdemExibicao { get; set; }

        public bool Ativo { get; set; } = true;

        public virtual ICollection<AvaliacaoCompetencia> AvaliacoesCompetencia { get; set; } = [];
    }
}