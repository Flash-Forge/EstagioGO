using EstagioGO.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace EstagioGO.Controllers
{
    public class FirstAccessController : Controller
    {
        private readonly IFirstAccessService _firstAccessService;

        public FirstAccessController(IFirstAccessService firstAccessService)
        {
            _firstAccessService = firstAccessService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfigureAdmin(AdminSetupViewModel model)
        {
            if (!await _firstAccessService.IsFirstAccessAsync())
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Aqui você chamaria um serviço para criar o administrador
            // Por simplicidade, vamos apenas redirecionar para login após criação
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }
    }

    public class AdminSetupViewModel
    {
        [Required(ErrorMessage = "O nome completo é obrigatório")]
        [Display(Name = "Nome Completo")]
        public string NomeCompleto { get; set; }

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória")]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirme a senha")]
        [Compare("Password", ErrorMessage = "A senha e a confirmação não coincidem.")]
        public string ConfirmPassword { get; set; }
    }
}