
using System.Collections.Generic;
using VkNet;
using VkNet.Enums.SafetyEnums;

namespace VKApi.BL.Interfaces
{
    public interface ILikesService
    {
        bool AddLike(long ownerId, long itemId, LikeObjectType type, VkApi api);
        void AddLike(long ownerId, List<long> itemIds, LikeObjectType type);
        List<long> GetUsersWhoLiked(long ownerId, long itemId, LikeObjectType type);

        List<long> GetUsersWhoLiked(long ownerId, long itemId, LikeObjectType type, VkApi api);
    }
}
