using DataAccessLayer.Abstract;
using DataAccessLayer.Concrete;
using DataAccessLayer.Repositories;
using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.EntityFramework
{
    public class EfMediaRepository : GenericRepository<Media>, IMediaDal
    {


        public Media GetByIdWithImages(int id)
        {
            using (var c = new Context()) // Context sınıfınızın adı neyse onu kullanın
            {
                return c.Medias
                        .Include(m => m.MediaImages) // MediaImages koleksiyonunu yükle
                        .FirstOrDefault(m => m.Id == id);
            }
        }

        public List<Media> GetListWithCategory()
        {
            using var c = new Context();
            return c.Medias.Include(m => m.Category).Include(m => m.MediaImages).ToList();
        }

    }
}
