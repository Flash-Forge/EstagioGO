using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EstagioGO.Controllers
{
    [AllowAnonymous] // Permite que qualquer usuário veja as páginas de erro
    public class ErrorController(ILogger<ErrorController> logger) : Controller
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


            ViewBag.StatusCode = statusCode;
            return View("GenericError");
        }

        [Route("Error/500")] // Rota para o ExceptionHandler
        public IActionResult Error500()
        {
            // Detalhe de execeção
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionHandlerPathFeature != null)
            {
                // Loga o erro com detalhes importantes
                logger.LogError(exceptionHandlerPathFeature.Error, "Erro não tratado na rota {Path}", exceptionHandlerPathFeature.Path);

                // Passa os detalhes do erro para a View
                ViewBag.Exception = exceptionHandlerPathFeature.Error;
                ViewBag.Path = exceptionHandlerPathFeature.Path;
            }

            // Exibe o RequestId
            ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            return View("Error");
        }
    }
}