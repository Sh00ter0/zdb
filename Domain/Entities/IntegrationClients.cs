using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(KeyHash), IsUnique = true)]
    public class IntegrationClients
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

        public List<KnownDeliveryTargets> KnownDeliveryTargets { get; set; } = [];
        public ZabbixCredentials? ZabbixCredential { get; set; }
    }
}
