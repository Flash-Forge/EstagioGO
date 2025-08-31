using EstagioGO.Constants;
using EstagioGO.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EstagioGO.Data
{
    public static class SeedData
    {
        private class SeedDataLogger { }

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<SeedDataLogger>();

            logger.LogInformation("Iniciando seed de dados...");

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                try
                {
                    // Garanta que o banco de dados existe
                    await dbContext.Database.EnsureCreatedAsync();
                    logger.LogInformation("Banco de dados garantido como criado.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao garantir criação do banco de dados");
                }

                // Criar os papéis (roles) se não existirem
                string[] roleNames = ["Administrador", "Coordenador", "Supervisor", "Estagiario"];
                foreach (var roleName in roleNames)
                {
                    try
                    {
                        var roleExist = await roleManager.RoleExistsAsync(roleName);
                        if (!roleExist)
                        {
                            await roleManager.CreateAsync(new ApplicationRole(roleName)
                            {
                                Descricao = ObterDescricaoRole(roleName),
                                DataCriacao = DateTime.Now
                            });
                            logger.LogInformation($"Papel '{roleName}' criado com sucesso.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Erro ao criar papel '{roleName}'");
                    }
                }

                // Criar administrador padrão
                string adminEmail = AppConstants.DefaultAdminEmail;
                string adminPassword = "Admin@123";

                try
                {
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    if (adminUser == null)
                    {
                        logger.LogInformation("Criando administrador padrão...");

                        adminUser = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true,
                            NomeCompleto = "Administrador Padrão",
                            Cargo = "Administrador",
                            DataCadastro = DateTime.Now,
                            Ativo = true,
                            PrimeiroAcessoConcluido = false // FORÇAR alteração de senha no primeiro acesso
                        };

                        var result = await userManager.CreateAsync(adminUser, adminPassword);

                        if (!result.Succeeded)
                        {
                            logger.LogError("Falha ao criar administrador padrão:");
                            foreach (var error in result.Errors)
                            {
                                logger.LogError($"- {error.Code}: {error.Description}");
                            }
                        }
                        else
                        {
                            logger.LogInformation("Administrador padrão criado com sucesso.");

                            // Adicione ao papel de Administrador
                            var roleResult = await userManager.AddToRoleAsync(adminUser, "Administrador");
                            if (!roleResult.Succeeded)
                            {
                                logger.LogError("Falha ao adicionar administrador ao papel:");
                                foreach (var error in roleResult.Errors)
                                {
                                    logger.LogError($"- {error.Description}");
                                }
                            }
                            else
                            {
                                logger.LogInformation("Administrador adicionado ao papel com sucesso.");
                            }
                        }
                    }
                    else
                    {
                        logger.LogInformation("Administrador padrão já existe.");

                        // Apenas garantir que o email está confirmado - NÃO alterar PrimeiroAcessoConcluido
                        if (!adminUser.EmailConfirmed)
                        {
                            adminUser.EmailConfirmed = true;
                            await userManager.UpdateAsync(adminUser);
                            logger.LogInformation("Email do administrador padrão foi confirmado.");
                        }

                        // REMOVER esta parte que causa o problema:
                        // adminUser.PrimeiroAcessoConcluido = false;
                        // await userManager.UpdateAsync(adminUser);
                        // logger.LogInformation("Administrador atualizado para forçar alteração de senha no próximo login.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro crítico ao criar/atualizar administrador padrão");
                }
            }

            logger.LogInformation("Seed de dados concluído.");
        }

        private static string ObterDescricaoRole(string roleName)
        {
            return roleName switch
            {
                "Administrador" => "Acesso total ao sistema",
                "Coordenador" => "Gestão completa dos estagiários",
                "Supervisor" => "Avaliação e acompanhamento dos estagiários",
                "Estagiario" => "Visualização do próprio perfil e registros",
                _ => "Papel do sistema",
            };
        }
    }
}