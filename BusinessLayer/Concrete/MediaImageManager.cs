using BusinessLayer.Abstract;
using DataAccessLayer.Abstract;
using EntityLayer.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Concrete
{
    public class MediaImageManager : IMediaImageService
    {
        IMediaImageDal _mediaimageDal;

        public MediaImageManager(IMediaImageDal mediaimageDal)
        {
            _mediaimageDal = mediaimageDal;
        }

        public List<MediaImage> GetList()
        {
            return _mediaimageDal.GetListAll();
        }

        public void TAdd(MediaImage t)
        {
            _mediaimageDal.Insert(t);
        }

        public void TDelete(MediaImage t)
        {
            _mediaimageDal.Delete(t);
        }

        public MediaImage TGetById(int id)
        {
            return _mediaimageDal.GetByID(id);
        }

        public void TUpdate(MediaImage t)
        {
            _mediaimageDal.Update(t);
        }
    }
}
