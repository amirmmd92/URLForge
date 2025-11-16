using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Distributed_URL_Shortener_with_Analytics.Models
{
    // Analytics model to track clicks on short links
    public class ClickAnalytics
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShortLinkId { get; set; }

        [ForeignKey("ShortLinkId")]
        public virtual ShortLink ShortLink { get; set; } = null!;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(500)]
        public string? Referer { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
    }
}

