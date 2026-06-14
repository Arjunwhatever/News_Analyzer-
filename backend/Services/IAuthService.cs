using Vector.Server.Entities;
using Vector.Server.Models;

namespace Vector.Server.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(UserDto request);
        Task<string?> LoginAsync(UserDto request);
        Task<string?> GetPreferencesAsync(Guid userId);
        Task UpdatePreferencesAsync(Guid userId, string topics);
    }
}
