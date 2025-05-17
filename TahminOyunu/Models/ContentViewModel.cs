using System.ComponentModel.DataAnnotations;

namespace TahminOyunu.Models
{
    public class ContentViewModel
    {
        public int Id { get; set; } // ID özelliğini ekledik

        [Required(ErrorMessage = "Başlık alanı zorunludur.")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Display(Name = "Kategori")]
        public int? CategoryId { get; set; }


        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Yeni Görseller")]
        public List<IFormFile> Images { get; set; }


    }
}
