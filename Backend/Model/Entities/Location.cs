using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(2)]
        public string? ISOCode { get; set; } // Optional: "US", "PL", etc.

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}
