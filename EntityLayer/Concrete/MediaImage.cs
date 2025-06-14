using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityLayer.Concrete
{
    public class MediaImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MediaId { get; set; }

        [ForeignKey("MediaId")]
        public virtual Media Media { get; set; }

        [Required]
        [StringLength(500)]
        public string ImagePath { get; set; }

        // Her medya için 1-6 arasında sıralı görseller tutulur
        [Required]
        [Range(1, 6)]
        public int OrderNo { get; set; }
    }
}
