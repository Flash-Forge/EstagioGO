using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Models.Analise
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da categoria é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode ter mais de 100 caracteres")]
        public string Nome { get; set; }

        [StringLength(500, ErrorMessage = "A descrição não pode ter mais de 500 caracteres")]
        public string Descricao { get; set; }

        public int OrdemExibicao { get; set; }

        public bool Ativo { get; set; } = true;

        // Relacionamento com Competencias
        public virtual ICollection<Competencia> Competencias { get; set; } = new HashSet<Competencia>();
    }
}