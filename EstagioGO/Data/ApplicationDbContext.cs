using EstagioGO.Models.Analise;
using EstagioGO.Models.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EstagioGO.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
    {

        // DbSets existentes
        public DbSet<Estagiario> Estagiarios { get; set; }
        public DbSet<Frequencia> Frequencias { get; set; }
        public DbSet<Justificativa> Justificativas { get; set; }
        public DbSet<Avaliacao> Avaliacoes { get; set; }

        // Novos DbSets para o sistema de avaliação por categorias
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Competencia> Competencias { get; set; }
        public DbSet<AvaliacaoCompetencia> AvaliacaoCompetencias { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .EnableDetailedErrors();
        }

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

            // Configurar relacionamentos existentes
            builder.Entity<Estagiario>()
                .HasOne(e => e.Supervisor)
                .WithMany(u => u.EstagiariosSupervisionados)
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Estagiario>()
                .HasOne(e => e.User)
                .WithMany(u => u.EstagiariosComoUsuario)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Estagiario>()
                .HasIndex(e => e.UserId)
                .IsUnique();

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
                .WithMany(u => u.FrequenciasRegistradas)
                .HasForeignKey(f => f.RegistradoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Avaliacao>()
                .HasOne(a => a.Estagiario)
                .WithMany()
                .HasForeignKey(a => a.EstagiarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Avaliacao>()
                .HasOne(a => a.Avaliador)
                .WithMany(u => u.AvaliacoesRealizadas)
                .HasForeignKey(a => a.AvaliadorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Justificativa>()
                .HasOne(j => j.UsuarioRegistro)
                .WithMany(u => u.JustificativasRegistradas)
                .HasForeignKey(j => j.UsuarioRegistroId)
                .OnDelete(DeleteBehavior.Restrict);

            // CONFIGURAÇÕES PARA O SISTEMA DE AVALIAÇÃO POR CATEGORIAS
            // Configurações para Categoria
            builder.Entity<Categoria>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Nome).HasMaxLength(100).IsRequired();
                entity.Property(c => c.Descricao).HasMaxLength(500);
                entity.Property(c => c.Ativo).HasDefaultValue(true);
                entity.HasIndex(c => c.OrdemExibicao);

                // Dados iniciais para categorias
                entity.HasData(
                    new Categoria { Id = 1, Nome = "Conhecimento Técnico", Descricao = "Avaliação dos conhecimentos técnicos específicos", OrdemExibicao = 1, Ativo = true },
                    new Categoria { Id = 2, Nome = "Comunicação", Descricao = "Habilidades de comunicação e expressão", OrdemExibicao = 2, Ativo = true },
                    new Categoria { Id = 3, Nome = "Trabalho em Equipe", Descricao = "Capacidade de colaboração e trabalho em grupo", OrdemExibicao = 3, Ativo = true },
                    new Categoria { Id = 4, Nome = "Proatividade", Descricao = "Iniciativa e capacidade de antecipação", OrdemExibicao = 4, Ativo = true },
                    new Categoria { Id = 5, Nome = "Qualidade do Trabalho", Descricao = "Qualidade e precisão nas entregas", OrdemExibicao = 5, Ativo = true }
                );
            });

            // Configurações para Competencia
            builder.Entity<Competencia>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Descricao).HasMaxLength(200).IsRequired();
                entity.Property(c => c.Ativo).HasDefaultValue(true);
                entity.HasIndex(c => c.CategoriaId);
                entity.HasIndex(c => c.OrdemExibicao);

                // Relacionamento com Categoria
                entity.HasOne(c => c.Categoria)
                    .WithMany(cat => cat.Competencias)
                    .HasForeignKey(c => c.CategoriaId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Dados iniciais para competências
                entity.HasData(
                    // Conhecimento Técnico
                    new Competencia { Id = 1, Descricao = "Domínio das ferramentas e tecnologias", CategoriaId = 1, OrdemExibicao = 1, Ativo = true },
                    new Competencia { Id = 2, Descricao = "Capacidade de resolver problemas técnicos", CategoriaId = 1, OrdemExibicao = 2, Ativo = true },
                    new Competencia { Id = 3, Descricao = "Qualidade do código/documentação", CategoriaId = 1, OrdemExibicao = 3, Ativo = true },
                    new Competencia { Id = 4, Descricao = "Capacidade de aprendizado de novas tecnologias", CategoriaId = 1, OrdemExibicao = 4, Ativo = true },

                    // Comunicação
                    new Competencia { Id = 5, Descricao = "Clareza na expressão oral", CategoriaId = 2, OrdemExibicao = 1, Ativo = true },
                    new Competencia { Id = 6, Descricao = "Clareza na expressão escrita", CategoriaId = 2, OrdemExibicao = 2, Ativo = true },
                    new Competencia { Id = 7, Descricao = "Capacidade de apresentação", CategoriaId = 2, OrdemExibicao = 3, Ativo = true },
                    new Competencia { Id = 8, Descricao = "Escuta ativa e compreensão", CategoriaId = 2, OrdemExibicao = 4, Ativo = true },

                    // Trabalho em Equipe
                    new Competencia { Id = 9, Descricao = "Colaboração e apoio aos colegas", CategoriaId = 3, OrdemExibicao = 1, Ativo = true },
                    new Competencia { Id = 10, Descricao = "Respeito às opiniões divergentes", CategoriaId = 3, OrdemExibicao = 2, Ativo = true },
                    new Competencia { Id = 11, Descricao = "Contribuição para decisões coletivas", CategoriaId = 3, OrdemExibicao = 3, Ativo = true },
                    new Competencia { Id = 12, Descricao = "Flexibilidade e adaptabilidade", CategoriaId = 3, OrdemExibicao = 4, Ativo = true },

                    // Proatividade
                    new Competencia { Id = 13, Descricao = "Iniciativa para assumir responsabilidades", CategoriaId = 4, OrdemExibicao = 1, Ativo = true },
                    new Competencia { Id = 14, Descricao = "Antecipação de problemas e soluções", CategoriaId = 4, OrdemExibicao = 2, Ativo = true },
                    new Competencia { Id = 15, Descricao = "Busca por melhorias contínuas", CategoriaId = 4, OrdemExibicao = 3, Ativo = true },
                    new Competencia { Id = 16, Descricao = "Autonomia na execução de tarefas", CategoriaId = 4, OrdemExibicao = 4, Ativo = true },

                    // Qualidade do Trabalho
                    new Competencia { Id = 17, Descricao = "Precisão e atenção aos detalhes", CategoriaId = 5, OrdemExibicao = 1, Ativo = true },
                    new Competencia { Id = 18, Descricao = "Cumprimento de prazos", CategoriaId = 5, OrdemExibicao = 2, Ativo = true },
                    new Competencia { Id = 19, Descricao = "Organização e documentação", CategoriaId = 5, OrdemExibicao = 3, Ativo = true },
                    new Competencia { Id = 20, Descricao = "Consistência nas entregas", CategoriaId = 5, OrdemExibicao = 4, Ativo = true }
                );
            });

            // Configurações para AvaliacaoCompetencia
            builder.Entity<AvaliacaoCompetencia>(entity =>
            {
                entity.HasKey(ac => ac.Id);

                // Configurar tamanho máximo para o comentário
                entity.Property(ac => ac.Comentario).HasMaxLength(500);

                // Relacionamento com Avaliacao
                entity.HasOne(ac => ac.Avaliacao)
                    .WithMany(a => a.CompetenciasAvaliadas)
                    .HasForeignKey(ac => ac.AvaliacaoId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relacionamento com Competencia
                entity.HasOne(ac => ac.Competencia)
                    .WithMany(c => c.AvaliacoesCompetencia)
                    .HasForeignKey(ac => ac.CompetenciaId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Garantir que cada competência seja avaliada apenas uma vez por avaliação
                entity.HasIndex(ac => new { ac.AvaliacaoId, ac.CompetenciaId }).IsUnique();
            });
        }
    }
}