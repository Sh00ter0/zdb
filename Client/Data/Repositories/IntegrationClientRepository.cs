using Microsoft.EntityFrameworkCore;

namespace Client.Data.Repositories
{
    public interface IntegrationClientRepository
    {
        Task<IntegrationClientEntity?> GetByIdAsync(long id);
        Task<IntegrationClientEntity?> GetByNameAsync(string name);
        Task<IntegrationClientEntity?> GetByKeyHashAsync(string keyHash);
        Task<bool> IsActiveAsync(long id);
        Task<bool> AddAsync(IntegrationClientEntity entity);
        Task<bool> UpdateAsync(IntegrationClientEntity entity);
        Task<bool> DeleteAsync(long id);
    }

    public class ApiClientRepository : IntegrationClientRepository
    {
        private readonly IDbContextFactory<ApiSecurityDbContext> _dbContextFactory;
        private readonly ILogger<ApiClientRepository> _logger;

        public ApiClientRepository(IDbContextFactory<ApiSecurityDbContext> dbContextFactory, ILogger<ApiClientRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<IntegrationClientEntity?> GetByIdAsync(long id)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.IntegrationClients.Include(c => c.ZabbixCredential).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IntegrationClientEntity?> GetByNameAsync(string name)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.IntegrationClients.Include(c => c.ZabbixCredential).AsNoTracking().FirstOrDefaultAsync(c => c.Name == name);
        }

        public async Task<IntegrationClientEntity?> GetByKeyHashAsync(string keyHash)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.IntegrationClients.Include(c => c.ZabbixCredential).AsNoTracking().FirstOrDefaultAsync(c => c.KeyHash == keyHash);
        }

        public async Task<bool> IsActiveAsync(long id)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            var entity = await db.IntegrationClients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

            if (entity == null) return false;
            else return entity.IsActive;
        }

        public async Task<bool> AddAsync(IntegrationClientEntity entity)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            db.IntegrationClients.Add(entity);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(IntegrationClientEntity entity)
        {
            try
            {
                var updateTime = DateTime.UtcNow;
                entity.UpdatedAtUtc = updateTime;

                await using var db = await _dbContextFactory.CreateDbContextAsync();
                db.IntegrationClients.Update(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client ID {ClientId}", entity.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var client = await db.IntegrationClients
                    .Include(c => c.ZabbixCredential)
                    .Include(c => c.KnownDeliveryTargets)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (client == null) return false;

                db.IntegrationClients.Remove(client);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client ID {ClientId}", id);
                return false;
            }
        }
    }
}
