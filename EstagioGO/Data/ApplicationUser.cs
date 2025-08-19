using EstagioGO.Models.Domain;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string NomeCompleto { get; set; }
    public string Cargo { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public bool Ativo { get; set; } = true;

    // 👇 ADICIONE ESTA LINHA 👇
    public bool PrimeiroAcessoConcluido { get; set; } = false;

    // Relacionamentos
    public ICollection<Estagiario> Estagiarios { get; set; }
    public ICollection<Avaliacao> AvaliacoesRealizadas { get; set; }
}