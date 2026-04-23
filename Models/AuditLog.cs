using System.ComponentModel.DataAnnotations;
namespace Airport.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [StringLength(20)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ActionDescription { get; set; } = string.Empty;

        [StringLength(100)]
        public string Controller { get; set; } = string.Empty;

        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(50)]
        public string IpAddress { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
