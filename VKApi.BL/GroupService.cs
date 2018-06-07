using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;
using System;
using System.Linq.Dynamic;
using System.Runtime.InteropServices;

namespace VKApi.BL
{
    public class GroupService : IGroupSerice
    {

        private readonly GroupsSort _getMemebersSort = GroupsSort.IdDesc;
        private const int Step = 1000;

        private readonly IVkApiFactory _apiFactory;
        private readonly IUserService _userService;

        public GroupService(IVkApiFactory apiFactory, IUserService userService)
        {
            _userService = userService;
            _apiFactory = apiFactory;
        }

        public List<Post> GetPosts(string groupName, ulong? count = null)
        {
            const ulong step = 100;
            ulong offset = 0;
            using (var api = _apiFactory.CreateVkApi())
            {
                var group = GetByName(groupName, api);
                var posts = new List<Post>();
                ulong totalCount;
                do
                {
                    var param = new WallGetParams()
                    {
                        OwnerId = -group.Id,
                        Filter = WallFilter.All,
                        Count = step,
                        Offset = offset
                    }; 
                    var getResult = api.Wall.Get(param);
                    var postsChunk = getResult.WallPosts.Select(p => p).ToList();
                    posts.AddRange(postsChunk);
                    offset = offset + step;
                    param.Offset = offset;
                    totalCount = getResult.TotalCount;
                } while (!count.HasValue ? offset < totalCount : offset < count.Value);

                var orderredPosts = posts.Where(p => p.Likes != null && p.Likes.Count > 0)
                    .OrderByDescending(p => p.Date)
                    .ThenByDescending(p => p.Likes.Count)
                    .ToList();
                return orderredPosts;
            }
        }

        public void BlackListGroupMembsersByGroupName(string searchPhrase, double wait = 1.5, string city = "", bool olderFirst = false)
        {
            List<Group> groups;
            using (var api = _apiFactory.CreateVkApi())
            {
                var p = new GroupsSearchParams() { Query = searchPhrase, Count = 1000 };
                var searchRes = api.Groups.Search(p);
                groups = searchRes.ToList();
            }
            BlackListGroupsMembsers(groups, wait, city);
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


        private void BlackListGroupsMembsers(List<Group> groups, double wait = 1.5, string city = "")
        {
            var blackListedUserIds = _userService.GetBannedIds().ToList();
            foreach (var g in groups)
            {
                BlackListGroupMembsers(g.Id.ToString(), blackListedUserIds, wait, city);
            }
        }

        public void BlackListGroupMembsers(string groupId, List<long> blackListedUserIds, double wait = 1.5,
            string city = "", bool olderFirst = false)
        {
            const string deletedUserText = "DELETED";
            const int secondsToSleepAfterOwnerIdIsincorrect = 1;
            const int secondsToSleepAfterOtherExceptions = 1;
            const int hoursToSleepAfterFloodControl = 12;
            var badUsers = GetGroupMembers(groupId).ToList();
            var idsToExclude = new List<long>() {458689189};
            Console.Clear();

            var badUsersFiltered = badUsers.Where(u => !blackListedUserIds.Contains(u.Id))
                .Where(u => !idsToExclude.Contains(u.Id)).ToList();

            var badUsersOrdered = badUsersFiltered
                .OrderByLsatActivityDateDesc();

            badUsersOrdered = badUsersOrdered.ThenBy(u => u.IsDeactivated)
                .ThenBy(u => u.FirstName.Contains(deletedUserText));

            if (!string.IsNullOrWhiteSpace(city))
            {
                badUsersOrdered = badUsersOrdered.ThenByDescending(u => u.FromCity(city));
            }

            badUsersOrdered = olderFirst
                ? badUsersOrdered.ThenBy(u => u.Id)
                : badUsersOrdered.ThenByDescending(u => u.Id);

            var totalUsersList = badUsersOrdered.ToList();

            var count = totalUsersList.Count;
            var counter = 0;
            using (var api = _apiFactory.CreateVkApi())
            {
                Console.Clear();
                foreach (var u in totalUsersList)
                {
                    var sleep = TimeSpan.FromSeconds(wait);
                    string message;
                    try
                    {
                        var r = _userService.BanUser(u, api);
                        message =
                            $"vk.com/{u.Domain} - {(r ? "banned" : "passed")}. Time {DateTime.Now}. {counter} out of {count}. {count - counter} - left.";

                        counter++;
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Contains("owner_id is incorrect"))
                        {
                            message =
                                $"vk.com/{u.Domain} - deleted. Time {DateTime.Now}. {counter} out of {count}. {count - counter} - left.";
                            counter++;
                            sleep = TimeSpan.FromSeconds(secondsToSleepAfterOwnerIdIsincorrect);
                        }
                        else
                        {
                            if (e.Message.ToLower().Contains("flood"))
                            {
                                message = "flood control. Sleeping...";
                                sleep = TimeSpan.FromHours(hoursToSleepAfterFloodControl);
                            }
                            else
                            {
                                message = e.Message.Trim();
                                sleep = TimeSpan.FromSeconds(secondsToSleepAfterOtherExceptions);
                            }
                        }
                    }

                    Console.WriteLine(message);
                    System.Threading.Thread.Sleep(sleep);
                }
            }
        }

        public List<User> GetGroupMembers(string groupName, UsersFields fields = null, int? count = null)
        {
            if (fields == null)
            {
                fields = UsersFields.All;
            }
            using (var api = _apiFactory.CreateVkApi())
            {
                var count2 = count ?? GetGroupMembersCount(groupName, api);
                var users = new List<User>();
                for (var offset = 0; offset < count2; offset = offset + Step)
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

        private static Group GetByName(string groupName, VkApi api)
        {
            var groupId = new List<string> { groupName };
            var res = api.Groups.GetById(groupId, groupName, GroupsFields.MembersCount);
            return res.FirstOrDefault();
        }

        private Group GetById(string groupId, VkApi api)
        {
            var groupIdList = new List<string> { groupId };
            var res = api.Groups.GetById(groupIdList, groupId, GroupsFields.All);
            return res.FirstOrDefault();
        }
    }
}
