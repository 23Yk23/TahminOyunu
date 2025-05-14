using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // Görüntüleme sırası (1 numaralı görsel ilk gösterilir)
        // Her film için 6 adet görsel olacak ve 1-6 arası sıralanacak
        [Required]
        [Range(1, 6)]
        public int OrderNo { get; set; }

    }
}
