using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;

namespace VKApi.BL
{
    public class GroupService:IGroupSerice
    {

        private readonly GroupsSort _getMemebersSort = GroupsSort.IdDesc;
        private const int Step = 1000;

        private readonly IVkApiFactory _apiFactory;


        public GroupService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        public List<Post> GetPosts(string groupName)
        {
            const ulong count = 1000;
            using (var api = _apiFactory.CreateVkApi())
            {
                var group = GetByName(groupName, api);
                var param = new WallGetParams() {OwnerId = -group.Id, Filter = WallFilter.All, Count = count};
                var posts = api.Wall.Get(param);
                var orderedPosts = posts.WallPosts.OrderByDescending(p => p.Date).ToList();
                return orderedPosts;
            }
        }

        public List<User> GetGroupMembers(string groupName, UsersFields fields = null)
        {
            if (fields == null)
            {
                fields = UsersFields.All;
            }
            using (var api = _apiFactory.CreateVkApi())
            {               
                var count = GetGroupMembersCount(groupName, api);
                var users = new List<User>();
                for (var offset = 0; offset <= count; offset = offset + Step)
                {
                    var usersChunk = GetGroupMembersOffset(offset, groupName, api, fields);
                    users.AddRange(usersChunk);
                }
                return users;
            }
        }

        private List<User> GetGroupMembersOffset(int offset, string groupName, VkApi api, UsersFields fields)
        {
            var param = new GroupsGetMembersParams()
            {
                Offset = offset,
                GroupId = groupName,
                Sort = _getMemebersSort,
                Fields = fields
            };
            var usersChunk = api.Groups.GetMembers(param);
            return usersChunk.ToList();
        }

        private int GetGroupMembersCount(string groupName, VkApi api)
        {
            var res = GetByName(groupName, api);
            return res.MembersCount.GetValueOrDefault();
        }

        private Group GetByName(string groupName, VkApi api)
        {
            var groupId = new List<string> { groupName };
            var res = api.Groups.GetById(groupId, groupName, GroupsFields.MembersCount);
            return res.FirstOrDefault();
        }
    }
}
