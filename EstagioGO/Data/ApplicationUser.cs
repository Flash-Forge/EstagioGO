using Microsoft.AspNetCore.Identity;

namespace EstagioGO.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string NomeCompleto { get; set; }
        public string Cargo { get; set; }
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public bool Ativo { get; set; } = true;

        // Relacionamentos
        // public ICollection<Estagiario> Estagiarios { get; set; }
        // public ICollection<Avaliacao> AvaliacoesRealizadas { get; set; }
    }
}
