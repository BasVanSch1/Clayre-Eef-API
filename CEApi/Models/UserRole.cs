using Microsoft.EntityFrameworkCore;

namespace CEApi.Models
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(Name), IsUnique = true)]
    public class UserRole
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public IList<RolePermission>? Permissions { get; set; }
    }
}
