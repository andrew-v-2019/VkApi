using System.Collections.Generic;
using System.Linq;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;

namespace VKApi.BL.Services
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

        public List<Photo> GetProfilePhotos(long ownerId, int count = 1)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                var param = new PhotoGetParams
                {
                    Extended = true,
                    AlbumId = PhotoAlbumType.Profile,
                    Count = (ulong?) count,
                    OwnerId = ownerId,
                    Reversed = true,
                };
                var getResult = api.Photo.Get(param).ToList();
                return getResult;
            }
        }
    }
}
