using Client.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.Data; // C# File-scoped namespace

[Table("IntegrationClients")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(KeyHash), IsUnique = true)]
public class IntegrationClientEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string KeyHash { get; set; } = null!;

    [Required]
    public string KeyPreview { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public List<KnownDeliveryTargetEntity> KnownDeliveryTargets { get; set; } = [];
    public ZabbixCredentialEntity? ZabbixCredential { get; set; }
}

[Table("ZabbixCredentials")]
[Index(nameof(AssociatedIntegrationClientId), IsUnique = true)]
public class ZabbixCredentialEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long AssociatedIntegrationClientId { get; set; }

    [Required]
    public string ApiUrl { get; set; } = null!;

    [Required]
    public string EncryptedApiToken { get; set; } = null!;

    [Required]
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    [ForeignKey(nameof(AssociatedIntegrationClientId))]
    public IntegrationClientEntity AssociatedIntegrationClient { get; set; } = null!;
}

[Table("KnownDeliveryTargets")]
[Index(nameof(IntegrationClientId), nameof(TargetId), IsUnique = true)]
[Index(nameof(IntegrationClientId), nameof(Name), IsUnique = true)]
[Index(nameof(AssociatedGuildId))]
public class KnownDeliveryTargetEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long IntegrationClientId { get; set; }

    [Required]
    public long CreatedById { get; set; }

    [Required]
    public ulong TargetId { get; set; }

    [Required]
    public TextChannelType ChannelType { get; set; } = 0;

    public ulong? AssociatedGuildId { get; set; }

    public bool AutoCrosspost { get; set; } = false;

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    [ForeignKey(nameof(IntegrationClientId))]
    public IntegrationClientEntity IntegrationClient { get; set; } = null!;

    [ForeignKey(nameof(CreatedById))]
    public SystemAdministratorEntity CreatedBy { get; set; } = null!;
}

[Table("SystemRoles")]
public class SystemRoleEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = null!;

    [Required]
    public int HierarchyWeight { get; set; }

    public List<SystemAdministratorEntity> Administrators { get; set; } = [];
    public List<RolePermissionEntity> RolePermissions { get; set; } = [];
}

[Table("Permissions")]
[Index(nameof(Key), IsUnique = true)]
public class PermissionEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = null!;

    public List<RolePermissionEntity> RolePermissions { get; set; } = [];
}

[Table("RolePermissions")]
public class RolePermissionEntity
{
    public int RoleId { get; set; }
    public SystemRoleEntity Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public PermissionEntity Permission { get; set; } = null!;
}

[Table("SystemAdministrators")]
[Index(nameof(DiscordUserId), IsUnique = true)]
public class SystemAdministratorEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    public ulong DiscordUserId { get; set; }

    public long? CreatedById { get; set; }

    public bool IsActive { get; set; } = false;
    public bool IsSystemManaged { get; set; } = false;

    [Required]
    public int RoleId { get; set; }

    [Required]
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public SystemAdministratorEntity? CreatedBy { get; set; }

    [ForeignKey(nameof(RoleId))]
    public SystemRoleEntity Role { get; set; } = null!;
}
