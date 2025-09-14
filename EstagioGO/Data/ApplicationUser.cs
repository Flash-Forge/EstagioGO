using EstagioGO.Models.Analise;
using EstagioGO.Models.Domain;
using EstagioGO.Models.Estagio;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(100)]
    public required string NomeCompleto { get; set; }

    [Required(ErrorMessage = "O cargo é obrigatório.")]
    [StringLength(50)]
    public required string Cargo { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public bool Ativo { get; set; } = true;
    public bool PrimeiroAcessoConcluido { get; set; } = false;

    // --- RELACIONAMENTOS ---
    public Estagiario? EstagiarioProfile { get; set; }
    public ICollection<Estagiario> EstagiariosSupervisionados { get; set; } = [];
    public ICollection<Avaliacao> AvaliacoesRealizadas { get; set; } = [];
    public ICollection<Frequencia> FrequenciasRegistradas { get; set; } = [];
    public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = [];
}