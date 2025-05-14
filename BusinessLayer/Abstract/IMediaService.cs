using DataAccessLayer.Abstract;
using EntityLayer.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Abstract
{
    public interface IMediaService : IGenericService<Media>
    {
        //kategori idsine göre bütün mediaları getiriyor
        public List<Media> GetMediaByCategoryId(int categoryId);

        // YENİ EKLENECEK METOT:
        Media TGetByIdWithImages(int id);
    }
}
