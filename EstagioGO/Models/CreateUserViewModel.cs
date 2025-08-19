using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EstagioGO.Models
{
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
}