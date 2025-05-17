using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityLayer.Concrete
{
    public class Category
    {

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } 

        [StringLength(500)]
        public string Description { get; set; }

        // İlişkiler
        public virtual ICollection<Media>? Medias { get; set; }

    }
}
