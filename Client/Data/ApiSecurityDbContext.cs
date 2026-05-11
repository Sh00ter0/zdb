using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Client.Data;

public class ApiSecurityDbContext(DbContextOptions<ApiSecurityDbContext> options) : DbContext(options)
{
    public DbSet<IntegrationClients> IntegrationClients => Set<IntegrationClients>();
    public DbSet<KnownDeliveryTargets> KnownDeliveryTargets => Set<KnownDeliveryTargets>();
    public DbSet<ZabbixCredentials> ZabbixCredentials => Set<ZabbixCredentials>();
    public DbSet<SystemAdministrators> SystemAdministrators => Set<SystemAdministrators>();

    public DbSet<SystemRoles> SystemRoles => Set<SystemRoles>();
    public DbSet<Permissions> Permissions => Set<Permissions>();
    public DbSet<RolePermissions> RolePermissions => Set<RolePermissions>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnownDeliveryTargets>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SystemAdministrators>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RolePermissions>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermissions>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermissions>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        modelBuilder.Entity<SystemRoles>().HasData(
            new SystemRoles { Id = 1, Name = "Super Administrator", HierarchyWeight = 1000 },
            new SystemRoles { Id = 2, Name = "Administrator", HierarchyWeight = 500 },
            new SystemRoles { Id = 3, Name = "Moderator", HierarchyWeight = 100 }
        );

        modelBuilder.Entity<Permissions>().HasData(
            // Range 1-99: Root permissions
            new Permissions { Id = 1, Key = "root", Description = "Root permission" },

            // Range 100-199: System permissions
            new Permissions { Id = 100, Key = "system.admins.read", Description = "Allow to read system administrators" },
            new Permissions { Id = 101, Key = "system.admins.write", Description = "Allow to create and modify system administrators" },

            // Range 200-299: API permissions
            new Permissions { Id = 200, Key = "api.clients.read", Description = "Allow to read API clients" },
            new Permissions { Id = 201, Key = "api.clients.write", Description = "Allow to create and modify API clients" },

            // Range 300-399: Known Targets permissions
            new Permissions { Id = 300, Key = "api.knownTargets.read", Description = "Allow to read known delivery targets" },
            new Permissions { Id = 301, Key = "api.knownTargets.write", Description = "Allow to create and modify known delivery targets" }

        );


        modelBuilder.Entity<RolePermissions>().HasData(

            // Root permissions set
            new RolePermissions { RoleId = 1, PermissionId = 1 },

            // System admininistrator permission set
            new RolePermissions { RoleId = 2, PermissionId = 100 },
            new RolePermissions { RoleId = 2, PermissionId = 101 },
            new RolePermissions { RoleId = 2, PermissionId = 200 },
            new RolePermissions { RoleId = 2, PermissionId = 201 },
            new RolePermissions { RoleId = 2, PermissionId = 300 },
            new RolePermissions { RoleId = 2, PermissionId = 301 },

            // System moderator permission set (only read permissions)
            new RolePermissions { RoleId = 3, PermissionId = 100 },
            new RolePermissions { RoleId = 3, PermissionId = 200 },
            new RolePermissions { RoleId = 3, PermissionId = 300 }
        );
    }
}
