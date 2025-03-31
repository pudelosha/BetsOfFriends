using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class Language
    {
        [Key]
        public int LanguageId { get; set; }

        [MaxLength(10)]
        public string ShortName { get; set; } = string.Empty; // e.g. "en"

        [MaxLength(100)]
        public string LongName { get; set; } = string.Empty;  // e.g. "English"

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}
