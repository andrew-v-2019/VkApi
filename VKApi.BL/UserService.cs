using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;

namespace VKApi.BL
{
    public class UserService:IUserService
    {
        private readonly IVkApiFactory _apiFactory;


        public UserService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        public bool BanUser(User userToBan, VkApi api)
        {
            var r = api.Account.BanUser(userToBan.Id);
            return r;
        }

      

        public void BanUsers(List<User> usersToBan)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                foreach (User u in usersToBan)
                {
                    BanUser(u, api);
                }
            }
        }

        public List<User> GetUsersByIds(List<long> userIds)
        {
            var users = new List<User>();
            const int step = 100;
            using (var api = _apiFactory.CreateVkApi())
            {
                var count = userIds.Count;
                for (var offset = 0; offset <= count; offset = offset + step)
                {
                    var idsChunk = userIds.Skip(offset).Take(step);
                    var chunk = api.Users.Get(idsChunk,ProfileFields.All);
                    users.AddRange(chunk);
                }
            }
            return users;
        }

        public List<long> GetBannedIds()
        {
            var users = new List<User>();
            const int step = 200;
            var offset = 0;
            var chunkCount = 0;
            using (var api = _apiFactory.CreateVkApi())
            {
                do
                {
                    var chunk = api.Account.GetBanned(offset, step);
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
            var commonFriendsParams = new FriendsGetMutualParams { TargetUid = targetUserId, SourceUid = sourderUserId };
            var commonFriends =
                api.Friends.GetMutual(commonFriendsParams);
            return commonFriends.Any();
        }

    }
}
