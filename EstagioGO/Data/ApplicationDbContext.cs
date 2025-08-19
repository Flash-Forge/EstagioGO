using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Seus outros DbSets - PARA FAZER: CRIAR MODELOS ABAIXO
        // public DbSet<Estagiario> Estagiarios { get; set; }
        // public DbSet<Avaliacao> Avaliacoes { get; set; }
        // public DbSet<Frequencia> Frequencias { get; set; }
        // public DbSet<Relatorio> Relatorios { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurações adicionais do Identity
            builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = "1",
                    Name = "Administrador",
                    NormalizedName = "ADMINISTRADOR",
                    Descricao = "Acesso total ao sistema",
                    DataCriacao = DateTime.Now
                },
                new ApplicationRole
                {
                    Id = "2",
                    Name = "Coordenador",
                    NormalizedName = "COORDENADOR",
                    Descricao = "Gestão completa dos estagiários",
                    DataCriacao = DateTime.Now
                },
                new ApplicationRole
                {
                    Id = "3",
                    Name = "Supervisor",
                    NormalizedName = "SUPERVISOR",
                    Descricao = "Avaliação e acompanhamento dos estagiários",
                    DataCriacao = DateTime.Now
                },
                new ApplicationRole
                {
                    Id = "4",
                    Name = "Estagiario",
                    NormalizedName = "ESTAGIARIO",
                    Descricao = "Visualização do próprio perfil e registros",
                    DataCriacao = DateTime.Now
                }
            );
        }
    }
}
