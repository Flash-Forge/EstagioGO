using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using EstagioGO.Data;

namespace EstagioGO.Data
{
    public static class SeedData
    {
        // Classe interna para uso com ILogger (resolve o erro CS0718)
        private class SeedDataLogger { }

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<SeedDataLogger>>();

            using (var scope = serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                // Criar os papéis (roles) se não existirem
                string[] roleNames = { "Administrador", "Coordenador", "Supervisor", "Estagiario" };
                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        await roleManager.CreateAsync(new ApplicationRole
                        {
                            Name = roleName,
                            NormalizedName = roleName.ToUpper(),
                            Descricao = ObterDescricaoRole(roleName),
                            DataCriacao = DateTime.Now
                        });
                        logger.LogInformation($"Papel '{roleName}' criado com sucesso.");
                    }
                }

                // Criar administrador padrão
                string adminEmail = "admin@estagio.com";
                string adminPassword = "Admin@123";

                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        NomeCompleto = "Administrador Padrão",
                        Ativo = true,
                        DataCadastro = DateTime.Now,
                        PrimeiroAcessoConcluido = false
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Administrador");

                        // 👇 CORREÇÃO: Use GenerateEmailConfirmationTokenAsync, NÃO GetEmailConfirmationTokenAsync 👇
                        var token = await userManager.GenerateEmailConfirmationTokenAsync(adminUser);
                        var confirmResult = await userManager.ConfirmEmailAsync(adminUser, token);

                        if (confirmResult.Succeeded)
                        {
                            logger.LogInformation("Administrador padrão criado e email confirmado com sucesso.");
                        }
                        else
                        {
                            logger.LogError("Falha ao confirmar email do administrador padrão.");
                            foreach (var error in confirmResult.Errors)
                            {
                                logger.LogError($"Erro de confirmação: {error.Description}");
                            }
                        }
                    }
                    else
                    {
                        logger.LogError("Falha ao criar administrador padrão.");
                        foreach (var error in result.Errors)
                        {
                            logger.LogError($"Erro de criação: {error.Description}");
                        }
                    }
                }
                else
                {
                    // Se o usuário já existe, garantir que ele tem o papel de Administrador
                    if (!await userManager.IsInRoleAsync(adminUser, "Administrador"))
                    {
                        await userManager.AddToRoleAsync(adminUser, "Administrador");
                        logger.LogInformation("Administrador padrão atualizado com o papel de Administrador.");
                    }

                    // Garantir que o email está confirmado
                    // 👇 CORREÇÃO: Verifique se o email já está confirmado 👇
                    if (!await userManager.IsEmailConfirmedAsync(adminUser))
                    {
                        var token = await userManager.GenerateEmailConfirmationTokenAsync(adminUser);
                        await userManager.ConfirmEmailAsync(adminUser, token);
                        logger.LogInformation("Email do administrador padrão foi confirmado.");
                    }

                    // Garantir que a flag PrimeiroAcessoConcluido está correta
                    if (adminUser.PrimeiroAcessoConcluido)
                    {
                        adminUser.PrimeiroAcessoConcluido = false;
                        await userManager.UpdateAsync(adminUser);
                        logger.LogInformation("Flag PrimeiroAcessoConcluido foi redefinida para o administrador padrão.");
                    }
                }
            }
        }

        private static string ObterDescricaoRole(string roleName)
        {
            switch (roleName)
            {
                case "Administrador": return "Acesso total ao sistema";
                case "Coordenador": return "Gestão completa dos estagiários";
                case "Supervisor": return "Avaliação e acompanhamento dos estagiários";
                case "Estagiario": return "Visualização do próprio perfil e registros";
                default: return "Papel do sistema";
            }
        }
    }
}