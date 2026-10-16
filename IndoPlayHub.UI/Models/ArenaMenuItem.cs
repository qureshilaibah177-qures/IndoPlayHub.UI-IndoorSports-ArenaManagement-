using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndoPlayHub.UI.Models
{
    public class ArenaMenuItem
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int MenuItemID { get; set; }

        // =========================================================
        // ARENA
        // =========================================================

        [Required]
        public int ArenaID { get; set; }

        public Arena Arena { get; set; } = null!;

        // =========================================================
        // MENU CATEGORY
        // =========================================================

        [Required]
        public int MenuCategoryID { get; set; }

        public MenuCategory MenuCategory { get; set; } = null!;

        // =========================================================
        // ITEM NAME
        // =========================================================

        [Required]
        [MaxLength(100)]
        public string ItemName { get; set; } = string.Empty;

        // =========================================================
        // PRICE
        // =========================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

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