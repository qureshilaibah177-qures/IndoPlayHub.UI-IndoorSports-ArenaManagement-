using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndoPlayHub.UI.Models
{
    public class Booking
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int BookingID { get; set; }


        // =========================================================
        // ARENA
        // =========================================================

        [Required]
        public int ArenaID { get; set; }

        public Arena Arena { get; set; } = null!;


        // =========================================================
        // COURT / ITEM
        // =========================================================

        [Required]
        public int ItemID { get; set; }

        public CourtOrMenuItem CourtOrMenuItem { get; set; } = null!;


        // =========================================================
        // CUSTOMER
        // =========================================================

        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;


        // =========================================================
        // DATE & TIME
        // =========================================================

        [Required]
        public DateOnly BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }


        // =========================================================
        // PAYMENT
        // =========================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(30)]
        public string BookingStatus { get; set; } = "Confirmed";

        [MaxLength(30)]
        public string PaymentStatus { get; set; } = "Pending";
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(250)]
        public string? CancellationReason { get; set; }

        public DateTime? CancelledAt { get; set; }

        [MaxLength(30)]
        public string RefundStatus { get; set; } = "Not Required";
    }
}