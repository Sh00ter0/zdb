using Application.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Client.Data.Repositories
{

    public class ApiTargetRepository : IKnownDeliveryTargetRepository
    {
        private readonly IDbContextFactory<ApiSecurityDbContext> _dbContextFactory;
        private readonly ILogger<ApiTargetRepository> _logger;

        public ApiTargetRepository(IDbContextFactory<ApiSecurityDbContext> dbContextFactory, ILogger<ApiTargetRepository> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<KnownDeliveryTargets?> GetByIdAsync(long clientId, long id)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.KnownDeliveryTargets
                .AsNoTracking()
                .Include(x => x.IntegrationClient)
                .FirstOrDefaultAsync(t => t.IntegrationClientId == clientId && t.Id == id);
        }

        public async Task<KnownDeliveryTargets?> GetByDiscordIdAsync(long clientId, ulong discordTargetId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.KnownDeliveryTargets
                .AsNoTracking()
                .Include(x => x.IntegrationClient)
                .FirstOrDefaultAsync(t => t.IntegrationClientId == clientId && t.TargetId == discordTargetId);
        }

        public async Task<KnownDeliveryTargets?> GetByNameAsync(long clientId, string name)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.KnownDeliveryTargets
                .AsNoTracking()
                .Include(x => x.IntegrationClient)
                .FirstOrDefaultAsync(t => t.IntegrationClientId == clientId && t.Name == name);
        }

        public async Task<List<KnownDeliveryTargets>> GetAllByClientIdAsync(long clientId)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();
            return await db.KnownDeliveryTargets
                .AsNoTracking()
                .Include(x => x.IntegrationClient)
                .Where(t => t.IntegrationClientId == clientId)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<bool> AddAsync(KnownDeliveryTargets entity)
        {
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                db.KnownDeliveryTargets.Add(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error adding target {TargetId}", entity.TargetId);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(KnownDeliveryTargets entity)
        {
            try
            {
                var updateTime = DateTime.UtcNow;
                entity.UpdatedAtUtc = updateTime;

                await using var db = await _dbContextFactory.CreateDbContextAsync();
                db.KnownDeliveryTargets.Update(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error updating target {TargetId}", entity.TargetId);
                return false;
            }
        }

        public async Task<bool> DeleteByIdAsync(long clientId, long targetRecordId)
        {
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var target = await db.KnownDeliveryTargets
                    .FirstOrDefaultAsync(t => t.IntegrationClientId == clientId && t.Id == targetRecordId);

                if (target == null) return false;

                db.KnownDeliveryTargets.Remove(target);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove target record {TargetRecordId}", targetRecordId);
                return false;
            }
        }
    }
}
