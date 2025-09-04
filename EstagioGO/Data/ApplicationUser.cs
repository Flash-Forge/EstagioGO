using EstagioGO.Models.Analise;
using EstagioGO.Models.Domain;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string NomeCompleto { get; set; }
    public string Cargo { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public bool Ativo { get; set; } = true;
    public bool PrimeiroAcessoConcluido { get; set; } = false;

    // Relacionamentos
    public ICollection<Estagiario> EstagiariosSupervisionados { get; set; }
    public ICollection<Estagiario> EstagiariosComoUsuario { get; set; }
    public ICollection<Avaliacao> AvaliacoesRealizadas { get; set; }
    public ICollection<Frequencia> FrequenciasRegistradas { get; set; }
}