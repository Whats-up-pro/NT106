namespace MessagingApp.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Avatar { get; set; }
        public string Status { get; set; } = "Offline";
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
