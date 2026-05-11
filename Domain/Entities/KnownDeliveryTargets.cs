using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Xml.Linq;

namespace Domain.Entities
{
    [Index(nameof(IntegrationClientId), nameof(TargetId), IsUnique = true)]
    [Index(nameof(IntegrationClientId), nameof(Name), IsUnique = true)]
    [Index(nameof(AssociatedGuildId))]
    public class KnownDeliveryTargets
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
        public IntegrationClients IntegrationClient { get; set; } = null!;

        [ForeignKey(nameof(CreatedById))]
        public SystemAdministrators CreatedBy { get; set; } = null!;
    }
}
