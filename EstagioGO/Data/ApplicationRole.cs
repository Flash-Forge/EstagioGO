using Microsoft.AspNetCore.Identity;

namespace EstagioGO.Data
{
    // Em EstagioGO/Data/ApplicationRole.cs
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() { }

        // Adicione este construtor para evitar problemas futuros
        public ApplicationRole(string roleName) : base(roleName) => DataCriacao = DateTime.Now;

        public string Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
