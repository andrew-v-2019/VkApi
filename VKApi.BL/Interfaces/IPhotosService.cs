using System.Collections.Generic;
using VkNet.Model;
using VkNet.Model.Attachments;

namespace VKApi.BL.Interfaces
{
    public interface IPhotosService
    {
        List<PhotoAlbum> GetAlbums(long ownerId);
        List<Photo> GetProfilePhotos(long ownerId);
    }
}
