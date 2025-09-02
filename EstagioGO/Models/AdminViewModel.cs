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
        public string Role { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; }
        public bool PrimeiroAcessoConcluido { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "O nome completo é obrigatório")]
        [Display(Name = "Nome Completo")]
        public required string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public required string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O papel é obrigatório")]
        [Display(Name = "Papel")]
        public required string Role { get; set; } = string.Empty;

        public bool SendEmail { get; set; } = true;

        public required List<SelectListItem> Roles { get; set; } = [];
    }

    public class EditUserViewModel
    {
        public required string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome completo é obrigatório")]
        [Display(Name = "Nome Completo")]
        public required string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public required string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O papel é obrigatório")]
        [Display(Name = "Papel")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        public List<SelectListItem> Roles { get; set; } = []; // ← Inicialização
    }

    public class UserDetailViewModel
    {
        public required string Id { get; set; } = string.Empty; // ← Inicialização
        public required string NomeCompleto { get; set; } = string.Empty; // ← Inicialização
        public required string Email { get; set; } = string.Empty; // ← Inicialização
        public required string Cargo { get; set; } = string.Empty; // ← Inicialização
        public required string Role { get; set; } = string.Empty; // ← Inicialização
        public DateTime DataCadastro { get; set; }
        public bool Ativo { get; set; }
        public bool PrimeiroAcessoConcluido { get; set; }
    }
}