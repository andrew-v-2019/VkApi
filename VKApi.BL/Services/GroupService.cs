using System;
using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VkNet.Model.Attachments;

namespace VKApi.BL.Services
{
    public class GroupService : IGroupSerice
    {

        private readonly GroupsSort _getMemebersSort = GroupsSort.IdDesc;
        private const int Step = 1000;

        private readonly IVkApiFactory _apiFactory;

        public GroupService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        public List<Post> GetPostsByGroupId(long groupId, VkApi api, ulong? count = null)
        {
            var posts = new List<Post>();
            ulong step = 100;
            if (count < step)
            {
                step = count.Value;
            }
            ulong offset = 0;
            ulong totalCount = 0;
            do
            {
                try
                {
                    var param = new WallGetParams()
                    {
                        OwnerId = -groupId,
                        Filter = WallFilter.Owner,
                        Count = step,
                        Offset = offset,
                    };
                    var getResult = api.Wall.Get(param);
                    var postsChunk = getResult.WallPosts.Select(p => p).ToList();
                    posts.AddRange(postsChunk);
                    offset = offset + step;
                    param.Offset = offset;
                    totalCount = getResult.TotalCount;
                    Console.Clear();
                    Console.WriteLine($"Total posts count {posts.Count}.");
                }
                catch
                {
                    offset = offset + 1;
                }
            } while (!count.HasValue ? offset < totalCount : offset < count.Value);

            var orderredPosts = posts.Where(p => p.Likes != null && p.Likes.Count > 0)
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.Likes.Count)
                .ToList();
            return orderredPosts;
        }

        public List<Post> GetPosts(string groupName, ulong? count = null)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                var group = GetByName(groupName, api);
                var posts = GetPostsByGroupId(group.Id, api, count);
                return posts;
            }
        }

        public List<Group> GetGroupsBySearchPhrase(string searchPhrase, int count = 1000)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                var p = new GroupsSearchParams() { Query = searchPhrase, Count = count };
                var searchRes = api.Groups.Search(p);
                var groups = searchRes.ToList();
                return groups;
            }
        }


        public List<UserExtended> GetGroupMembers(string groupName, UsersFields fields = null, int? count = null)
        {
            if (fields == null)
            {
                fields = UsersFields.All;
            }

            using (var api = _apiFactory.CreateVkApi())
            {
                var count2 = count ?? GetGroupMembersCount(groupName, api);
                var users = new List<UserExtended>();
                for (var offset = 0; offset < count2; offset = offset + Step)
                {
                    var usersChunk = GetGroupMembersOffset(offset, groupName, api, fields);
                    users.AddRange(usersChunk);
                    Console.Write($"\rGot {users.Count} users...");
                }
                Console.WriteLine();
                return users;
            }
        }

        private List<UserExtended> GetGroupMembersOffset(int offset, string groupName, VkApi api, UsersFields fields)
        {
            List<User> usersChunk;
            var param = new GroupsGetMembersParams()
            {
                Offset = offset,
                GroupId = groupName,
                Sort = _getMemebersSort,
                Fields = fields,
            };
            usersChunk = api.Groups.GetMembers(param).ToList();

            var model = usersChunk.Select(x => x.ToExtendedModel()).ToList();
            return model;
        }

        private List<UserExtended> GetGroupMembersOneByOne(int startPosition, int endPoition, string groupName, VkApi api, UsersFields fields)
        {
            var usersChunk = new List<User>();

            for (var i = startPosition; i <= endPoition; i++)
            {
                var param = new GroupsGetMembersParams()
                {
                    Offset = i,
                    GroupId = groupName,
                    Sort = _getMemebersSort,
                    Fields = fields,
                    Count = 1
                };
                try
                {
                    var u = api.Groups.GetMembers(param).FirstOrDefault();
                    if (u != null)
                    {
                        usersChunk.Add(u);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            var extendedModels = usersChunk.Select(x => new UserExtended(x)).ToList();
            return extendedModels;
        }

        private static int GetGroupMembersCount(string groupName, VkApi api)
        {
            try
            {
                var res = GetByName(groupName, api);

                return res.MembersCount.GetValueOrDefault();
            }
            catch(Exception e)
            {
                return 0;
            }
        }

        private static Group GetByName(string groupName, VkApi api)
        {
            try
            {
                var groupId = new List<string> { groupName.ToLower().Replace("public", string.Empty) };
                var res = api.Groups.GetById(groupId, null, GroupsFields.MembersCount);
                return res.FirstOrDefault();
            }
            catch
            {
                var res = api.Groups.GetById(null, groupName, GroupsFields.MembersCount);
                return res.FirstOrDefault();
            }

        }

        public Group GetByName(string groupName)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                return GetByName(groupName, api);
            }
        }
    }
}
