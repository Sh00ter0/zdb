using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class RolePermissions
    {
        public int RoleId { get; set; }
        public SystemRoles Role { get; set; } = null!;

        public int PermissionId { get; set; }
        public Permissions Permission { get; set; } = null!;
    }
}
