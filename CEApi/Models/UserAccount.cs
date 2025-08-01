using Microsoft.EntityFrameworkCore;

namespace CEApi.Models
{
    [PrimaryKey(nameof(userId))]
    [Index(nameof(userName), IsUnique = true)]
    [Index(nameof(email), IsUnique = true)]
    public class UserAccount
    {
        public string? userId { get; set; }
        public string userName { get; set; }
        public string? displayName { get; set; }
        public string email { get; set; }
        public string passwordHash { get; set; }
        public UserRole[]? Roles { get; set; }
    }
}
