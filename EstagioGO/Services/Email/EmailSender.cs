using Microsoft.AspNetCore.Identity.UI.Services;

namespace EstagioGO.Services.Email
{
    public class EmailSender(ILogger<EmailSender> logger) : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            logger.LogInformation($"Email simulado: Para: {email}, Assunto: {subject}");
            logger.LogInformation($"Mensagem: {htmlMessage}");
            // Em desenvolvimento, apenas registramos no log
            return Task.CompletedTask;
        }
    }
}