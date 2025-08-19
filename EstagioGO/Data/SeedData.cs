using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EstagioGO.Data;

namespace EstagioGO.Data
{
    public static class SeedData
    {
        private class SeedDataLogger { }

        public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedDataLogger>>();

                // Verificar e atualizar as descrições dos papéis (se necessário)
                string[] roleNames = { "Administrador", "Coordenador", "Supervisor", "Estagiario" };
                foreach (var roleName in roleNames)
                {
                    var role = await roleManager.FindByNameAsync(roleName);
                    if (role != null && string.IsNullOrEmpty(role.Descricao))
                    {
                        role.Descricao = ObterDescricaoRole(roleName);
                        role.DataCriacao = role.DataCriacao == default ? DateTime.Now : role.DataCriacao;
                        await roleManager.UpdateAsync(role);
                        logger.LogInformation($"Papel '{roleName}' atualizado com descrição.");
                    }
                }

                // Criar administrador padrão
                string adminEmail = configuration.GetValue<string>("AdminSettings:Email") ?? "admin@estagio.com";
                string adminPassword = configuration.GetValue<string>("AdminSettings:Password") ?? "Admin@123";

                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        NomeCompleto = "Administrador do Sistema",
                        Ativo = true,
                        DataCadastro = DateTime.Now
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Administrador");
                        logger.LogInformation("Administrador padrão criado com sucesso.");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            logger.LogError($"Erro ao criar administrador: {error.Description}");
                        }
                    }
                }
            }
        }

        private static string ObterDescricaoRole(string roleName)
        {
            switch (roleName)
            {
                case "Administrador":
                    return "Acesso total ao sistema";
                case "Coordenador":
                    return "Gestão completa dos estagiários";
                case "Supervisor":
                    return "Avaliação e acompanhamento dos estagiários";
                case "Estagiario":
                    return "Visualização do próprio perfil e registros";
                default:
                    return "Papel do sistema";
            }
        }
    }
}