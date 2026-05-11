using Domain.Entities;

namespace Application.Repositories
{
    public interface IIntegrationClientRepository
    {
        Task<IntegrationClients?> GetByIdAsync(long id);
        Task<IntegrationClients?> GetByNameAsync(string name);
        Task<IntegrationClients?> GetByKeyHashAsync(string keyHash);
        Task<bool> IsActiveAsync(long id);
        Task<bool> AddAsync(IntegrationClients entity);
        Task<bool> UpdateAsync(IntegrationClients entity);
        Task<bool> DeleteAsync(long id);
    }
}
