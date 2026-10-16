using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndoPlayHub.UI.Models
{
    public class CourtOrMenuItem
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int ItemID { get; set; }

        // =========================================================
        // ARENA
        // =========================================================

        [Required]
        public int ArenaID { get; set; }

        public Arena Arena { get; set; } = null!;

        // =========================================================
        // CATEGORY
        // =========================================================

        [Required]
        public int CategoryID { get; set; }

        public Category Category { get; set; } = null!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        // =========================================================
        // ITEM / COURT NAME
        // =========================================================

        [Required]
        [MaxLength(100)]
        public string ItemName { get; set; } = string.Empty;

        // =========================================================
        // PRICE
        // =========================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerHour { get; set; }

        // =========================================================
        // AVAILABILITY
        // =========================================================

        public bool IsAvailable { get; set; } = true;

        // =========================================================
        // IMAGE
        // =========================================================

        [MaxLength(255)]
        public string? ImageUrl { get; set; }
    }
}