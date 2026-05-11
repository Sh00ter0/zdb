using System.ComponentModel.DataAnnotations;

namespace Application.Common.Zabbix
{
    public class ZabbixPayload
    {
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = null!;

        [Required]
        [StringLength(10000)]
        public string Message { get; set; } = null!;

        [Range(0, 10)]
        public int EventSource { get; set; }

        [Range(0, 10)]
        public int EventValue { get; set; }

        [Range(0, 5)]
        public int Severity { get; set; }

        [Required]
        [RegularExpression(@"^\d{1,32}$")]
        public string EventId { get; set; } = null!;

        [Required]
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public List<ZabbixTag> Tags { get; set; } = [];

        [Range(0, 1)]
        public int ControlMenu { get; set; }
    }
}
