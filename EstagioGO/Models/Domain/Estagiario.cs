using EstagioGO.Models.Analise;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstagioGO.Models.Domain
{
    public class Estagiario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode ter mais de 100 caracteres")]
        public required string Nome { get; set; }

        [Required(ErrorMessage = "A matrícula é obrigatória")]
        [StringLength(20, ErrorMessage = "A matrícula não pode ter mais de 20 caracteres")]
        public required string Matricula { get; set; }

        [Required(ErrorMessage = "O curso é obrigatório")]
        [StringLength(100, ErrorMessage = "O curso não pode ter mais de 100 caracteres")]
        public required string Curso { get; set; }

        [Required(ErrorMessage = "A instituição de ensino é obrigatória")]
        [Display(Name = "Instituição de Ensino")]
        [StringLength(150, ErrorMessage = "A instituição de ensino não pode ter mais de 150 caracteres")]
        public required string InstituicaoEnsino { get; set; }

        [Required(ErrorMessage = "A data de início é obrigatória")]
        [Display(Name = "Data de Início")]
        [DataType(DataType.Date)]
        public DateTime DataInicio { get; set; }

        [Display(Name = "Data de Término")]
        [DataType(DataType.Date)]
        public DateTime? DataTermino { get; set; }

        [Required(ErrorMessage = "O supervisor é obrigatório")]
        [Display(Name = "Supervisor")]
        public required string SupervisorId { get; set; }

        [ForeignKey("SupervisorId")]
        public required ApplicationUser Supervisor { get; set; }

        [Required(ErrorMessage = "O usuário é obrigatório")]
        [Display(Name = "Usuário do Sistema")]
        public required string UserId { get; set; }

        [ForeignKey("UserId")]
        public required ApplicationUser User { get; set; }

        public bool Ativo { get; set; } = true;

        [Display(Name = "Data de Cadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // Relacionamentos - inicializar como listas vazias para evitar null reference
        public ICollection<Frequencia> Frequencias { get; set; } = [];

        public ICollection<Avaliacao> Avaliacoes { get; set; } = [];
    }
}