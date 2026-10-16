using System.ComponentModel.DataAnnotations;

namespace IndoPlayHub.UI.Models
{
    public class MenuCategory
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        [Key]
        public int MenuCategoryID { get; set; }

        // =========================================================
        // CATEGORY NAME
        // =========================================================

        [Required]
        [MaxLength(50)]
        public string MenuCategoryName { get; set; } = string.Empty;

        // =========================================================
        // MENU ITEMS
        // =========================================================

        public ICollection<ArenaMenuItem> MenuItems { get; set; }
            = new List<ArenaMenuItem>();
    }
}