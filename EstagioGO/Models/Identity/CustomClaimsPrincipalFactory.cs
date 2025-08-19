using EstagioGO.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EstagioGO.Models.Identity
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        public CustomClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await base.CreateAsync(user);
            var identity = (ClaimsIdentity)principal.Identity;

            // Adiciona claims específicas do projeto
            var claims = new List<Claim>();

            if (!string.IsNullOrEmpty(user.NomeCompleto))
                claims.Add(new Claim("FullName", user.NomeCompleto));

            // Você pode adicionar mais claims conforme necessário
            // Ex: claims.Add(new Claim("DepartamentoId", user.DepartamentoId.ToString()));

            identity.AddClaims(claims);
            return principal;
        }
    }
}
