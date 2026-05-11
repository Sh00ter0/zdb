using System.ComponentModel.DataAnnotations;

namespace Application.Common.Zabbix
{
    public class ZabbixTag
    {
        [Required]
        [StringLength(64)]
        public string Tag { get; set; } = null!;

        [Required]
        [StringLength(256)]
        public string Value { get; set; } = null!;
    }
}
