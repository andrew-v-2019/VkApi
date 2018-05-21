
using System.Collections.Generic;
using VkNet;
using VkNet.Model;

namespace VKApi.BL.Interfaces
{
    public interface IUserService
    {
        List<User> GetUsersByIds(List<long> userIds);
        bool HaveCommonFriends(long targetUserId, long sourderUserId, VkApi api);
        bool BanUser(User userToBan, VkApi api);
        void BanUsers(List<User> usersToBan);
        List<long> GetBannedIds();

    }
}
