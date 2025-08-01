namespace CEApi.Models
{
    public class UserRole
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public RolePermission[]? Permissions { get; set; }
    }
}
