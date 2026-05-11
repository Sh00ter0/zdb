using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Index(nameof(AssociatedIntegrationClientId), IsUnique = true)]
    public class ZabbixCredentials
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
        public IntegrationClients AssociatedIntegrationClient { get; set; } = null!;
    }
}
