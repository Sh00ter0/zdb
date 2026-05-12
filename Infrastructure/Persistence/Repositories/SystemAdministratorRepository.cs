using Application.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    public class BotAdminRepository(
        IDbContextFactory<ApiSecurityDbContext> dbContextFactory,
        ILogger<BotAdminRepository> logger) : ISystemAdministratorRepository
    {
        public async Task<SystemAdministrators?> GetByDiscordIdAsync(ulong discordUserId)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            return await db.SystemAdministrators
                .Include(a => a.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.DiscordUserId == discordUserId);
        }

        public async Task<List<SystemAdministrators>> GetAllAsync()
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            return await db.SystemAdministrators
                .Include(a => a.Role)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> AddAsync(SystemAdministrators entity)
        {
            try
            {
                await using var db = await dbContextFactory.CreateDbContextAsync();
                db.SystemAdministrators.Add(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add bot admin {AdminId}", entity.DiscordUserId);
                return false;
            }
        }

        public async Task<bool> IsActiveAsync(ulong discordUserId)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            var entity = await db.SystemAdministrators
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.DiscordUserId == discordUserId);

            return entity?.IsActive ?? false;
        }

        public async Task<bool> UpdateAsync(SystemAdministrators entity)
        {
            try
            {
                await using var db = await dbContextFactory.CreateDbContextAsync();

                entity.UpdatedAtUtc = DateTime.UtcNow;

                db.SystemAdministrators.Update(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update bot admin {AdminId}", entity.DiscordUserId);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(ulong discordUserId)
        {
            try
            {
                await using var db = await dbContextFactory.CreateDbContextAsync();
                var admin = await db.SystemAdministrators.FirstOrDefaultAsync(a => a.DiscordUserId == discordUserId);
                if (admin == null) return false;

                db.SystemAdministrators.Remove(admin);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove bot admin {AdminId}", discordUserId);
                return false;
            }
        }

        public async Task UpsertSuperAdminAsync(ulong discordUserId, bool isActive = true)
        {
            try
            {
                await using var db = await dbContextFactory.CreateDbContextAsync();

                // Szukamy rekordu po Discord ID
                var superAdmin = await db.SystemAdministrators.FirstOrDefaultAsync(a => a.DiscordUserId == discordUserId);

                if (superAdmin == null)
                {
                    if (!isActive) return;

                    superAdmin = new SystemAdministrators
                    {
                        DiscordUserId = discordUserId,
                        RoleId = 1, // ID 1 to nasz wbudowany (zasiany) Super Administrator
                        IsActive = true,
                        IsSystemManaged = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    };
                    db.SystemAdministrators.Add(superAdmin);
                }
                else
                {
                    superAdmin.RoleId = 1;
                    superAdmin.IsActive = isActive;
                    superAdmin.IsSystemManaged = true;
                    superAdmin.UpdatedAtUtc = DateTime.UtcNow;
                    db.SystemAdministrators.Update(superAdmin);
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upsert SuperAdmin with DiscordId {AdminId}", discordUserId);
                throw;
            }
        }
    }
}
