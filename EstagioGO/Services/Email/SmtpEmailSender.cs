// Em Services/Email/SmtpEmailSender.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EstagioGO.Services.Email
{
    public class SmtpEmailSender(IOptions<EmailSettings> emailSettings, ILogger<SmtpEmailSender> logger) : IEmailSender
    {
        private readonly EmailSettings _emailSettings = emailSettings.Value;

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                mimeMessage.To.Add(new MailboxAddress("", email));
                mimeMessage.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlMessage };
                mimeMessage.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);

                logger.LogInformation("Email para {To} enviado com sucesso.", email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar email para {To}", email);
                // Lançar a exceção pode ser útil para saber que o envio falhou.
                throw;
            }
        }
    }
}