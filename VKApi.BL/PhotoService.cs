using System.Collections.Generic;
using System.Linq;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;

namespace VKApi.BL
{
    public class PhotosService: IPhotosService
    {
        private readonly IVkApiFactory _apiFactory;


        public PhotosService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        public List<PhotoAlbum> GetAlbums(long ownerId)
        {
            var alb = new List<PhotoAlbum>();
            using (var api = _apiFactory.CreateVkApi())
            {
                var p = new PhotoGetAlbumsParams()
                {
                    OwnerId = ownerId,
                    NeedSystem = true,
                };
                var albums = api.Photo.GetAlbums(p);
                alb.AddRange(albums.ToList());
            }
            return alb;
        }
    }
}
