using System.ComponentModel.DataAnnotations;

namespace RoyalD.Web.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Detail { get; set; } = string.Empty;

        [MaxLength(100)]
        public string District { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        public int? DurationMinutes { get; set; }

        [MaxLength(50)]
        public string IPAddress { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Latitude { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Longitude { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Area { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
