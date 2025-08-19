using EstagioGO.Data;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace EstagioGO.Services
{
    public interface IFirstAccessService
    {
        Task<bool> IsFirstAccessAsync();
    }

    public class FirstAccessService : IFirstAccessService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public FirstAccessService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> IsFirstAccessAsync()
        {
            // Verifica se há pelo menos um usuário no sistema
            var users = await _userManager.GetUsersInRoleAsync("Administrador");
            return !users.Any();
        }
    }
}