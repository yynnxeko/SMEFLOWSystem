using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Core.Entities
{
    public partial class Invite
    {
        public Guid Id { get; set; } = Guid.NewGuid();  // Thêm Id (GUID default)
        public Guid TenantId { get; set; }
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? PositionId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; } = false;
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Thêm CreatedAt
        public DateTime? UpdatedAt { get; set; }  // Thêm UpdatedAt (nullable)
        public bool IsDeleted { get; set; } = false;  // Thêm IsDeleted
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual Department? Department { get; set; }
        public virtual Position? Position { get; set; }
    }
}