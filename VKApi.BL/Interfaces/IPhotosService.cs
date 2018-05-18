using System.Collections.Generic;
using VkNet.Model;

namespace VKApi.BL.Interfaces
{
    public interface IPhotosService
    {
        List<PhotoAlbum> GetAlbums(long ownerId);
    }
}
