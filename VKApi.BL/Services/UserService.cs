using System;
using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;

namespace VKApi.BL.Services
{
    public class UserService : IUserService
    {
        private readonly IVkApiFactory _apiFactory;


        public UserService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        public bool BanUser(UserExtended userToBan, VkApi api)
        {
            var r = api.Account.BanUser(userToBan.Id);
            return r;
        }

        public void BanUsers(List<UserExtended> usersToBan)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                foreach (var u in usersToBan)
                {
                    BanUser(u, api);
                }
            }
        }

        public List<UserExtended> GetUsersByIds(List<long> userIds)
        {
            var users = new List<UserExtended>();
            const int step = 100;
            using (var api = _apiFactory.CreateVkApi())
            {
                var count = userIds.Count;
                for (var offset = 0; offset <= count; offset = offset + step)
                {
                    var idsChunk = userIds.Skip(offset).Take(step);
                    var chunk = api.Users.Get(idsChunk, ProfileFields.All).Select(x => x.ToExtendedModel());
                    users.AddRange(chunk);
                    Console.Clear();
                    Console.WriteLine($"Users total count: {users.Count}");
                }
            }
            return users;
        }

        public List<long> GetBannedIds()
        {
            var users = new List<UserExtended>();
            const int step = 200;
            var offset = 0;
            using (var api = _apiFactory.CreateVkApi())
            {
                int chunkCount;
                do
                {
                    var chunk = api.Account.GetBanned(offset, step).Select(x => x.ToExtendedModel()).ToList();
                    users.AddRange(chunk);
                    offset = offset + step;
                    chunkCount = chunk.Count;
                } while (chunkCount == step);
            }
            var ids = users.Select(u => u.Id).ToList();
            return ids;
        }

        public bool HaveCommonFriends(long targetUserId, long sourderUserId, VkApi api)
        {
            var commonFriendsParams = new FriendsGetMutualParams {TargetUid = targetUserId, SourceUid = sourderUserId};
            var commonFriends =
                api.Friends.GetMutual(commonFriendsParams);
            return commonFriends.Any();
        }
    }
}
