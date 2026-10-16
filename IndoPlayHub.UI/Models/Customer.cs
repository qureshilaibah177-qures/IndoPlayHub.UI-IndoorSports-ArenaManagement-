using System.ComponentModel.DataAnnotations;

namespace IndoPlayHub.UI.Models
{
    public class Customer
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int CustomerID { get; set; }


        // =========================================================
        // FULL NAME
        // =========================================================

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;


        // =========================================================
        // EMAIL
        // =========================================================

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        // =========================================================
        // PHONE
        // =========================================================

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;


        // =========================================================
        // PASSWORD
        // =========================================================

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;


        // =========================================================
        // PROFILE IMAGE
        // =========================================================

        [MaxLength(255)]
        public string? ProfileImageUrl { get; set; }


        // =========================================================
        // ACCOUNT STATUS
        // =========================================================

        public bool IsActive { get; set; } = true;


        // =========================================================
        // CREATED DATE
        // =========================================================

        public DateTime CreatedAt { get; set; } = DateTime.Now;


        // =========================================================
        // TERMS & CONDITIONS
        // =========================================================

        public bool TermsAccepted { get; set; } = false;


        // =========================================================
        // TERMS ACCEPTANCE DATE
        // =========================================================

        public DateTime? TermsAcceptedAt { get; set; }
    }
}