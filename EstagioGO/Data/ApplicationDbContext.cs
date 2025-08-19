using EstagioGO.Models.Domain;
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

        // Adicione estes DbSets
        public DbSet<Estagiario> Estagiarios { get; set; }
        public DbSet<Frequencia> Frequencias { get; set; }
        public DbSet<Justificativa> Justificativas { get; set; }
        public DbSet<Avaliacao> Avaliacoes { get; set; }
        public DbSet<ItemAvaliacao> ItensAvaliacao { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .Property(u => u.PrimeiroAcessoConcluido)
                .HasDefaultValue(false);

            // Configurações adicionais do Identity
            builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = "1",
                    Name = "Administrador",
                    NormalizedName = "ADMINISTRADOR",
                    Descricao = "Acesso total ao sistema",
                },
                new ApplicationRole
                {
                    Id = "2",
                    Name = "Coordenador",
                    NormalizedName = "COORDENADOR",
                    Descricao = "Gestão completa dos estagiários",
                },
                new ApplicationRole
                {
                    Id = "3",
                    Name = "Supervisor",
                    NormalizedName = "SUPERVISOR",
                    Descricao = "Avaliação e acompanhamento dos estagiários",
                },
                new ApplicationRole
                {
                    Id = "4",
                    Name = "Estagiario",
                    NormalizedName = "ESTAGIARIO",
                    Descricao = "Visualização do próprio perfil e registros",
                }
            );

            // Configurar relacionamentos
            builder.Entity<Estagiario>()
                .HasOne(e => e.Supervisor)
                .WithMany(u => u.Estagiarios)
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Estagiario>()
                .HasOne(e => e.Coordenador)
                .WithMany()
                .HasForeignKey(e => e.CoordenadorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Estagiario>()
                .HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<Estagiario>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Frequencia>()
                .HasOne(f => f.Estagiario)
                .WithMany(e => e.Frequencias)
                .HasForeignKey(f => f.EstagiarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Frequencia>()
                .HasOne(f => f.Justificativa)
                .WithMany(j => j.Frequencias)
                .HasForeignKey(f => f.JustificativaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Frequencia>()
                .HasOne(f => f.RegistradoPor)
                .WithMany()
                .HasForeignKey(f => f.RegistradoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Avaliacao>()
                .HasOne(a => a.Estagiario)
                .WithMany(e => e.Avaliacoes)
                .HasForeignKey(a => a.EstagiarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Avaliacao>()
                .HasOne(a => a.Avaliador)
                .WithMany()
                .HasForeignKey(a => a.AvaliadorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
