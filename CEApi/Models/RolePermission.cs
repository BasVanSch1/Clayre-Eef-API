using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CEApi.Models
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(Name), IsUnique = true)]
    public class RolePermission
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}
