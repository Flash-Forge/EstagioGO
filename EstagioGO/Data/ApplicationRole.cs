using Microsoft.AspNetCore.Identity;

namespace EstagioGO.Data
{
    public class ApplicationRole : IdentityRole
    {
        public string Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
