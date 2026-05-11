using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class SystemRoles
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [Required]
        public int HierarchyWeight { get; set; }

        public List<SystemAdministrators> Administrators { get; set; } = [];
        public List<RolePermissions> RolePermissions { get; set; } = [];
    }
}
