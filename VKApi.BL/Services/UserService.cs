using System;
using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using System.Threading.Tasks;
using VKApi.BL.Extensions;

namespace VKApi.BL.Services
{
    public class UserService : IUserService
    {
        private readonly IVkApiFactory _apiFactory;
        private readonly ICacheService _cache;

        public UserService(IVkApiFactory apiFactory, ICacheService cache)
        {
            _apiFactory = apiFactory;
            _cache = cache;
        }

        public List<UserExtended> GetFriends(long userId)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                var param = new FriendsGetParams()
                {
                    UserId = userId,
                    Fields = ProfileFields.All
                };
                var friends = api.Friends.Get(param);
                var result = friends.Select(x => new UserExtended(x)).ToList();
                return result;
            }
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

        public List<UserExtended> GetUsersByIds(List<long> userIds, ProfileFields profileFields = null)
        {
            if (profileFields == null)
            {
                profileFields = ProfileFields.All;
            }
            var users = new List<UserExtended>();
            const int step = 100;
            using (var api = _apiFactory.CreateVkApi())
            {
                var count = userIds.Count;
                for (var offset = 0; offset <= count; offset = offset + step)
                {
                    var idsChunk = userIds.Skip(offset).Take(step);
                    try
                    {
                        var chunk = api.Users.Get(idsChunk, profileFields).Select(x => x.ToExtendedModel());
                        users.AddRange(chunk);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;

                    }
                    Console.Clear();
                    Console.WriteLine($"Users total count: {users.Count}");
                }
            }
            return users;
        }

        public List<long> GetBannedIds()
        {
            var cacheData = _cache.Get<List<long>>(CacheKeys.BannedUserIds.ToString());
            if (cacheData != null && cacheData.Any())
            {
                return cacheData;
            }

            var users = new List<UserExtended>();
            const int step = 200;
            var offset = 0;
            Console.WriteLine("Getting user ids that already blacklisted...");
            using (var api = _apiFactory.CreateVkApi())
            {
                int chunkCount;
                do
                {
                    var chunk = api.Account.GetBanned(offset, step).Profiles.Select(x => x.ToExtendedModel()).ToList();
                    users.AddRange(chunk);
                    offset = offset + step;
                    chunkCount = chunk.Count;
                    Console.Write($"\rGot {users.Count} blackListed users...");
                } while (chunkCount == step);
            }
            var ids = users.Select(u => u.Id).ToList();
            _cache.Create(ids, CacheKeys.BannedUserIds.ToString());
            return ids;
        }

        public bool HaveCommonFriends(long targetUserId, long sourderUserId, VkApi api)
        {
            var commonFriendsParams = new FriendsGetMutualParams { TargetUid = targetUserId, SourceUid = sourderUserId };
            var commonFriends =
                api.Friends.GetMutual(commonFriendsParams);

            return commonFriends.Any();
        }

        public async Task<List<UserExtended>> Search(UserSearchParams parameters)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                var searchResult = await api.Users.SearchAsync(parameters);
                return searchResult.Select(x => new UserExtended(x)).ToList();
            }
        }
    }
}
