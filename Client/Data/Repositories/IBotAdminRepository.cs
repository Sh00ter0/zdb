using Client.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Data.Repositories;

public interface IBotAdminRepository
{
    Task<SystemAdministratorEntity?> GetByDiscordIdAsync(ulong discordUserId);
    Task<List<SystemAdministratorEntity>> GetAllAsync();
    Task<bool> AddAsync(SystemAdministratorEntity entity);
    Task<bool> IsActiveAsync(ulong discordUserId);
    Task<bool> UpdateAsync(SystemAdministratorEntity entity);
    Task<bool> DeleteAsync(ulong discordUserId);
    Task UpsertSuperAdminAsync(ulong discordUserId, bool isActive = true);
}

public class BotAdminRepository(
    IDbContextFactory<ApiSecurityDbContext> dbContextFactory,
    ILogger<BotAdminRepository> logger) : IBotAdminRepository
{
    public async Task<SystemAdministratorEntity?> GetByDiscordIdAsync(ulong discordUserId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        return await db.SystemAdministrators
            .Include(a => a.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DiscordUserId == discordUserId);
    }

    public async Task<List<SystemAdministratorEntity>> GetAllAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        return await db.SystemAdministrators
            .Include(a => a.Role)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> AddAsync(SystemAdministratorEntity entity)
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

    public async Task<bool> UpdateAsync(SystemAdministratorEntity entity)
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

                superAdmin = new SystemAdministratorEntity
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
