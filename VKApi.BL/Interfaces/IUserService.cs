
using System.Collections.Generic;
using VkNet;
using VKApi.BL.Models;

namespace VKApi.BL.Interfaces
{
    public interface IUserService
    {
        List<UserExtended> GetUsersByIds(List<long> userIds);
        bool HaveCommonFriends(long targetUserId, long sourderUserId, VkApi api);
        bool BanUser(UserExtended userToBan, VkApi api);
        void BanUsers(List<UserExtended> usersToBan);
        List<long> GetBannedIds();
        List<UserExtended> GetFriends(long userId);

    }
}
