using System.ComponentModel.DataAnnotations;

namespace IndoPlayHub.UI.Models
{
    public class Category
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int CategoryID { get; set; }

        // =========================================================
        // CATEGORY NAME
        // =========================================================

        [Required]
        [MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        // =========================================================
        // NAVIGATION PROPERTY
        // =========================================================

        public ICollection<CourtOrMenuItem> Items { get; set; }
            = new List<CourtOrMenuItem>();
    }
}