using Microsoft.AspNetCore.Identity.UI.Services;

namespace EstagioGO.Services.Email
{
    public class EmailSender(ILogger<EmailSender> logger) : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            logger.LogInformation("Email simulado: Para: {Email}, Assunto: {Subject}", email, subject);
            logger.LogInformation("Mensagem: {HtmlMessage}", htmlMessage);
            // Em desenvolvimento, apenas registramos no log
            return Task.CompletedTask;
        }
    }
}