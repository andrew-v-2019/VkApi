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


        public bool HaveCommonFriends(long targetUserId, long sourderUserId, VkApi api)
        {
            var commonFriendsParams = new FriendsGetMutualParams { TargetUid = targetUserId, SourceUid = sourderUserId };
            var commonFriends =
                api.Friends.GetMutual(commonFriendsParams);
            return commonFriends.Any();
        }

    }
}
