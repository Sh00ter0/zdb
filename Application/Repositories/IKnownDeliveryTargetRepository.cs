using Domain.Entities;

namespace Application.Repositories
{
    public interface IKnownDeliveryTargetRepository
    {
        Task<KnownDeliveryTargets?> GetByIdAsync(long clientId, long discordTargetId);
        Task<KnownDeliveryTargets?> GetByDiscordIdAsync(long clientId, ulong discordTargetId);
        Task<KnownDeliveryTargets?> GetByNameAsync(long clientId, string name);
        Task<List<KnownDeliveryTargets>> GetAllByClientIdAsync(long clientId);
        Task<bool> AddAsync(KnownDeliveryTargets entity);
        Task<bool> UpdateAsync(KnownDeliveryTargets entity);
        Task<bool> DeleteByIdAsync(long clientId, long targetRecordId);
    }
}
