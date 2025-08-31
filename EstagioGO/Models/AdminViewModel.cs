using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Models
{
    public class UserManagementViewModel
    {
        public required string Id { get; set; }
        public required string NomeCompleto { get; set; }
        public required string Email { get; set; }
        public required string Cargo { get; set; }
        public string Role { get; set; }
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; }
        public bool PrimeiroAcessoConcluido { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "O nome completo é obrigatório")]
        [Display(Name = "Nome Completo")]
        public string NomeCompleto { get; set; }

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O papel é obrigatório")]
        [Display(Name = "Papel")]
        public string Role { get; set; }

        public bool SendEmail { get; set; } = true;

        public List<SelectListItem> Roles { get; set; }
    }

    public class EditUserViewModel
    {
        public required string Id { get; set; }

        [Required(ErrorMessage = "O nome completo é obrigatório")]
        [Display(Name = "Nome Completo")]
        public required string NomeCompleto { get; set; }

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "O papel é obrigatório")]
        [Display(Name = "Papel")]
        public string Role { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        public List<SelectListItem> Roles { get; set; }
    }

    public class UserDetailViewModel
    {
        public required string Id { get; set; }
        public required string NomeCompleto { get; set; }
        public required string Email { get; set; }
        public required string Cargo { get; set; }
        public required string Role { get; set; }
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; }
        public bool PrimeiroAcessoConcluido { get; set; }
    }
}
