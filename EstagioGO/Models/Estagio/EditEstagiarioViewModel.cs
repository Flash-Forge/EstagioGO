using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Models.Estagio
{
    public class EditEstagiarioViewModel
    {
        public int Id { get; set; }

        // Propriedades do Estagiário (iguais ao CreateViewModel)
        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        public string CPF { get; set; }

        [Required(ErrorMessage = "A data de nascimento é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data de Nascimento")]
        public DateTime DataNascimento { get; set; }

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        public string Telefone { get; set; }

        [Required(ErrorMessage = "A matrícula é obrigatória.")]
        public string Matricula { get; set; }

        [Required(ErrorMessage = "O curso é obrigatório.")]
        public string Curso { get; set; }

        [Required(ErrorMessage = "A instituição de ensino é obrigatória.")]
        [Display(Name = "Instituição de Ensino")]
        public string InstituicaoEnsino { get; set; }

        [Required(ErrorMessage = "A data de início é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data de Início")]
        public DateTime DataInicio { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data de Término")]
        public DateTime? DataTermino { get; set; }

        [Required(ErrorMessage = "O supervisor é obrigatório.")]
        [Display(Name = "Supervisor")]
        public string SupervisorId { get; set; }

        public bool Ativo { get; set; }

        // Propriedade para a conta de usuário
        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "O formato do e-mail é inválido.")]
        [Display(Name = "E-mail de Acesso")]
        public string Email { get; set; }
    }
}