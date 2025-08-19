using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace EstagioGO.Services.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation($"Email simulado: Para: {email}, Assunto: {subject}");
            _logger.LogInformation($"Mensagem: {htmlMessage}");
            // Em desenvolvimento, apenas registramos no log
            return Task.CompletedTask;
        }
    }
}