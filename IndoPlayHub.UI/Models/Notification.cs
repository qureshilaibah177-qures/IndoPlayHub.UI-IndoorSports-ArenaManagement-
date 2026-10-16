using System.ComponentModel.DataAnnotations;

namespace IndoPlayHub.UI.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        public Customer Customer { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}