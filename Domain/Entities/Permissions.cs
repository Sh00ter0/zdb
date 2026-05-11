using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    [Index(nameof(Key), IsUnique = true)]
    public class Permissions
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = null!;

        public List<RolePermissions> RolePermissions { get; set; } = [];
    }
}
