using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Index(nameof(DiscordUserId), IsUnique = true)]
    public class SystemAdministrators
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
        public SystemAdministrators? CreatedBy { get; set; }

        [ForeignKey(nameof(RoleId))]
        public SystemRoles Role { get; set; } = null!;
    }
}
