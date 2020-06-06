using System.Collections.Generic;
using VkNet;
using VkNet.Model;

namespace VKApi.BL.Interfaces
{
    public interface ICommentsService
    {
        List<Comment> GetComments(long ownerId, long postId, VkApi api);
    }
}
