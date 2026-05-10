using Client.Models;
using Microsoft.EntityFrameworkCore;

namespace Client.Data;

public class ApiSecurityDbContext(DbContextOptions<ApiSecurityDbContext> options) : DbContext(options)
{
    public DbSet<IntegrationClientEntity> IntegrationClients => Set<IntegrationClientEntity>();
    public DbSet<KnownDeliveryTargetEntity> KnownDeliveryTargets => Set<KnownDeliveryTargetEntity>();
    public DbSet<ZabbixCredentialEntity> ZabbixCredentials => Set<ZabbixCredentialEntity>();
    public DbSet<SystemAdministratorEntity> SystemAdministrators => Set<SystemAdministratorEntity>();

    public DbSet<SystemRoleEntity> SystemRoles => Set<SystemRoleEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnownDeliveryTargetEntity>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SystemAdministratorEntity>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RolePermissionEntity>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermissionEntity>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermissionEntity>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        // Defining roles with hierarchy weights to establish a clear permission structure
        modelBuilder.Entity<SystemRoleEntity>().HasData(
            new SystemRoleEntity { Id = 1, Name = "Super Administrator", HierarchyWeight = 1000 },
            new SystemRoleEntity { Id = 2, Name = "Administrator", HierarchyWeight = 500 },
            new SystemRoleEntity { Id = 3, Name = "Moderator", HierarchyWeight = 100 }
        );

        // Defining permissions with specific ID ranges for better organization and future expansion
        modelBuilder.Entity<PermissionEntity>().HasData(
            // Range 1-99: Root permissions
            new PermissionEntity { Id = 1, Key = "root", Description = "Root permission" },

            // Range 100-199: System permissions
            new PermissionEntity { Id = 100, Key = "system.admins.read", Description = "Allow to read system administrators" },
            new PermissionEntity { Id = 101, Key = "system.admins.write", Description = "Allow to create and modify system administrators" },

            // Range 200-299: API permissions
            new PermissionEntity { Id = 200, Key = "api.clients.read", Description = "Allow to read API clients" },
            new PermissionEntity { Id = 201, Key = "api.clients.write", Description = "Allow to create and modify API clients" },

            // Range 300-399: Known Targets permissions
            new PermissionEntity { Id = 300, Key = "api.knownTargets.read", Description = "Allow to read known delivery targets" },
            new PermissionEntity { Id = 301, Key = "api.knownTargets.write", Description = "Allow to create and modify known delivery targets" }
            
        );


        modelBuilder.Entity<RolePermissionEntity>().HasData(

            // Root permissions set
            new RolePermissionEntity { RoleId = 1, PermissionId = 1 },

            // System admininistrator permission set
            new RolePermissionEntity { RoleId = 2, PermissionId = 100 },
            new RolePermissionEntity { RoleId = 2, PermissionId = 101 },
            new RolePermissionEntity { RoleId = 2, PermissionId = 200 },
            new RolePermissionEntity { RoleId = 2, PermissionId = 201 },
            new RolePermissionEntity { RoleId = 2, PermissionId = 300 },
            new RolePermissionEntity { RoleId = 2, PermissionId = 301 },

            // System moderator permission set (only read permissions)
            new RolePermissionEntity { RoleId = 3, PermissionId = 100 },
            new RolePermissionEntity { RoleId = 3, PermissionId = 200 },
            new RolePermissionEntity { RoleId = 3, PermissionId = 300 }
        );
    }
}
