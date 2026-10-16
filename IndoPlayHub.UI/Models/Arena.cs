using System.ComponentModel.DataAnnotations;

namespace IndoPlayHub.UI.Models
{
    public class Arena
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int ArenaID { get; set; }

        // =========================================================
        // OWNER INFORMATION
        // =========================================================

        [Required]
        public int OwnerID { get; set; }

        [Required]
        [MaxLength(100)]
        public string OwnerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string OwnerCNIC { get; set; } = string.Empty;

        // =========================================================
        // ARENA INFORMATION
        // =========================================================

        [Required]
        [MaxLength(100)]
        public string ArenaName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        // =========================================================
        // CONTACT INFORMATION
        // =========================================================

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        // =========================================================
        // BUSINESS HOURS
        // SQL: TIME NOT NULL
        // =========================================================

        public TimeSpan OpeningTime { get; set; } =
            new TimeSpan(8, 0, 0);

        public TimeSpan ClosingTime { get; set; } =
            new TimeSpan(23, 0, 0);

        // =========================================================
        // COVER IMAGE
        // =========================================================

        [MaxLength(255)]
        public string? CoverImageUrl { get; set; }

        // =========================================================
        // LOGIN
        // =========================================================

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        // =========================================================
        // RECORD INFORMATION
        // =========================================================

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // =========================================================
        // SETUP STATUS
        // =========================================================

        public bool IsSetupCompleted { get; set; } = false;

        // =========================================================
        // PHASE 12 — CANCELLATION POLICY
        // =========================================================

        [Range(0, 168)]
        public int CancellationHours { get; set; } = 2;

        // =========================================================
        // NAVIGATION PROPERTIES
        // =========================================================

        public ICollection<CourtOrMenuItem> Items { get; set; }
            = new List<CourtOrMenuItem>();

        public ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();
        
    }
}