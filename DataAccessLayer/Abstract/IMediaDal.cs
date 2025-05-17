using EntityLayer.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Abstract
{
    public interface IMediaDal : IGenericDal<Media>
    {
        Media GetByIdWithImages(int id);
        public List<Media> GetListWithCategory();


    }
}
