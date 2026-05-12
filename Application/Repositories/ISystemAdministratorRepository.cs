using Domain.Entities;

namespace Application.Repositories
{
    public interface ISystemAdministratorRepository
    {
        Task<SystemAdministrators?> GetByDiscordIdAsync(ulong discordUserId);
        Task<List<SystemAdministrators>> GetAllAsync();
        Task<bool> AddAsync(SystemAdministrators entity);
        Task<bool> IsActiveAsync(ulong discordUserId);
        Task<bool> UpdateAsync(SystemAdministrators entity);
        Task<bool> DeleteAsync(ulong discordUserId);
        Task UpsertSuperAdminAsync(ulong discordUserId, bool isActive = true);
    }
}
