using Microsoft.AspNetCore.Mvc;

namespace EstagioGO.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Desculpe, a página que você procurava não foi encontrada.";
                    return View("NotFound");
            }

            // Para qualquer outro código de erro, você pode ter uma view genérica
            return View("Error");
        }
    }
}