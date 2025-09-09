using EstagioGO.Models.Analise;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace EstagioGO.Models.Domain
{
    public class Estagiario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome não pode ter mais de 100 caracteres")]
        public required string Nome { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(14, ErrorMessage = "O CPF deve ter 14 caracteres")]
        [CPF(ErrorMessage = "CPF inválido")]
        [Display(Name = "CPF")]
        public required string CPF { get; set; }

        [Required(ErrorMessage ="A data de nascimento é obrigatória")]
        [Display(Name = "Data de Nascimento")]
        [DataType(DataType.Date)]
        public DateTime DataNascimento { get; set; }

        [Required(ErrorMessage = "O telefone é obrigatório")]
        [StringLength(15, ErrorMessage = "O telefone deve ter 15 caracteres")]
        [Phone(ErrorMessage = "Telefone inválido")]
        [Display(Name = "Telefone")]
        public required string Telefone { get; set; }

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

        // Método para formatar CPF
        public string CPFFormatado => FormatCPF(CPF);

        // Método para formatar Telefone
        public string TelefoneFormatado => FormatTelefone(Telefone);

        private static string FormatCPF(string cpf)
        {
            if (string.IsNullOrEmpty(cpf) || cpf.Length != 11) return cpf;
            return $"{cpf[..3]}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}";
        }

        private static string FormatTelefone(string telefone)
        {
            if (string.IsNullOrEmpty(telefone)) return telefone;

            // Remove todos os caracteres não numéricos
            var numeros = Regex.Replace(telefone, @"[^\d]", "");

            if (numeros.Length == 11)
            {
                return $"({numeros[..2]}) {numeros.Substring(2, 5)}-{numeros.Substring(7, 4)}";
            }
            else if (numeros.Length == 10)
            {
                return $"({numeros[..2]}) {numeros.Substring(2, 4)}-{numeros.Substring(6, 4)}";
            }

            return telefone;
        }
    }
}