using System.ComponentModel.DataAnnotations;

namespace Distributed_URL_Shortener_with_Analytics.Models
{
    // Application settings model
    public class AppSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Value { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

