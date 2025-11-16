using System.ComponentModel.DataAnnotations;

namespace Distributed_URL_Shortener_with_Analytics.Models
{
    // User model to store application users
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<ShortLink> ShortLinks { get; set; } = new List<ShortLink>();
        public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    }
}

