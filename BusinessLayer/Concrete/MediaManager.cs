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
    public class MediaManager : IMediaService
    {
        IMediaDal _mediaDal;

        public MediaManager(IMediaDal mediaDal)
        {
            _mediaDal = mediaDal;
        }

        public List<Media> GetList()
        {
            return _mediaDal.GetListAll();
        }

        public void TAdd(Media t)
        {
            _mediaDal.Insert(t);
        }

        public void TDelete(Media t)
        {
            _mediaDal.Delete(t);
        }

        public Media TGetById(int id)
        {
            return _mediaDal.GetByID(id);
        }

        public void TUpdate(Media t)
        {
            _mediaDal.Update(t);
        }

        public List<Media> GetMediaByCategoryId(int categoryId)
        {
            return _mediaDal.GetListAll().Where(x => x.CategoryId == categoryId && x.IsActive).ToList();
        }
        public Media TGetByIdWithImages(int id)
        {
            return _mediaDal.GetByIdWithImages(id);
        }
        public List<Media> GetListWithCategoryM()
        {
            return _mediaDal.GetListWithCategory();
        }
    }
}
