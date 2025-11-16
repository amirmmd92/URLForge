using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Distributed_URL_Shortener_with_Analytics.Models
{
    // Short link model to store shortened URLs
    public class ShortLink
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string ShortCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        public string OriginalUrl { get; set; } = string.Empty;

        // Nullable - if null, link was created without login
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property for analytics
        public virtual ICollection<ClickAnalytics> ClickAnalytics { get; set; } = new List<ClickAnalytics>();

        // Check if analytics are available (only if created by logged in user)
        public bool HasAnalytics => UserId.HasValue;
    }
}

