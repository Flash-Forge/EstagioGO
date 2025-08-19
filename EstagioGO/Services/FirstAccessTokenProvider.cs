using Microsoft.AspNetCore.Identity;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EstagioGO.Services
{
    public interface IFirstAccessTokenProvider
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
        Task<ApplicationUser> ValidateTokenAsync(string token, UserManager<ApplicationUser> userManager);
    }

    public class FirstAccessTokenProvider : IFirstAccessTokenProvider
    {
        public async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            // Criar um token seguro que contém o UserId
            string payload = $"{user.Id}|{DateTime.UtcNow.AddHours(24).Ticks}";

            // Assinar o token com HMAC
            byte[] key = Encoding.UTF8.GetBytes("SUA_CHAVE_SECRETA_PARA_PRIMEIRO_ACESSO");
            using (var hmac = new HMACSHA256(key))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                string signature = Convert.ToBase64String(hash);

                return $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))}.{signature}";
            }
        }

        public async Task<ApplicationUser> ValidateTokenAsync(string token, UserManager<ApplicationUser> userManager)
        {
            try
            {
                // Verificar a assinatura
                string[] parts = token.Split('.');
                if (parts.Length != 2) return null;

                string payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                string signature = parts[1];

                // Verificar a assinatura
                byte[] key = Encoding.UTF8.GetBytes("SUA_CHAVE_SECRETA_PARA_PRIMEIRO_ACESSO");
                using (var hmac = new HMACSHA256(key))
                {
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                    string expectedSignature = Convert.ToBase64String(hash);

                    if (signature != expectedSignature) return null;
                }

                // Decodificar o payload
                string[] payloadParts = payload.Split('|');
                if (payloadParts.Length != 2) return null;

                string userId = payloadParts[0];
                long expirationTicks = long.Parse(payloadParts[1]);

                // Verificar expiração
                if (DateTime.UtcNow.Ticks > expirationTicks) return null;

                // Obter o usuário
                return await userManager.FindByIdAsync(userId);
            }
            catch
            {
                return null;
            }
        }
    }
}