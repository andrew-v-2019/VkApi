﻿
using System.Collections.Generic;
using VkNet;
using VkNet.Enums.Filters;
using VKApi.BL.Models;
using VkNet.Model.RequestParams;
using System.Threading.Tasks;

namespace VKApi.BL.Interfaces
{
    public interface IUserService
    {
        List<UserExtended> GetUsersByIds(List<long> userIds, ProfileFields profileFields = null);
        bool HaveCommonFriends(long targetUserId, long sourderUserId, VkApi api);
        bool BanUser(UserExtended userToBan, VkApi api);
        void BanUsers(List<UserExtended> usersToBan);
        List<long> GetBannedIds();
        List<UserExtended> GetFriends(long userId);
        Task<List<UserExtended>> Search(UserSearchParams parameters);
    }
}
