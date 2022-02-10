using System;
using System.Collections.Generic;
using System.Linq;
using VKApi.BL.Extensions;
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

        private readonly GroupsSort _getMembersSort = GroupsSort.IdDesc;
        private const int Step = 1000;

        private readonly IVkApiFactory _apiFactory;
        private readonly ILikesService _likeService;
        private readonly ICommentsService _commentService;


        public GroupService(IVkApiFactory apiFactory, ILikesService likeService, ICommentsService commentService)
        {
            _apiFactory = apiFactory;
            _likeService = likeService;
            _commentService = commentService;
        }

        public List<Post> GetPostsByGroupId(long groupId, VkApi api, DateTime minDate)
        {
            var posts = new List<Post>();
            const ulong step = 100;
            ulong offset = 0;
            ulong totalCount = 0;
            DateTime? lastPostDate = null;
            do
            {
                try
                {
                    var param = new WallGetParams
                    {
                        OwnerId = -groupId,
                        Filter = WallFilter.Owner,
                        Count = step,
                        Offset = offset,
                    };
                    var getResult = api.Wall.Get(param);
                    var postsChunk = getResult.WallPosts.Select(p => p).ToList();
                    posts.AddRange(postsChunk);
                    offset += step;
                    param.Offset = offset;
                    totalCount = getResult.TotalCount;
                    Console.Write("\r{0}  ", $"Total posts count {posts.Count}.");
                    var lastPost = posts.OrderByDescending(p => p.Date).Last();
                    lastPostDate = lastPost.Date;
                }
                catch
                {
                    offset += 1;
                }
            } while (offset < totalCount && lastPostDate > minDate);

            var orderedPosts = posts
                .OrderByDescending(p => p.Date)
                .ToList();
            return orderedPosts;
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
            using (var api = _apiFactory.CreateVkApi(true))
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

            using (var api = _apiFactory.CreateVkApi(true))
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
                Sort = _getMembersSort,
                Fields = fields
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
                    Sort = _getMembersSort,
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
            catch (Exception e)
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

        public List<UserExtended> GetGroupPostsLickers(long groupId, List<long> blackListedUserIds, DateTime minPostDate)
        {
            var users = new List<UserExtended>();

            try
            {
                using (var api = _apiFactory.CreateVkApi(true))
                {
                    var wallPosts = GetPostsByGroupId(groupId, api, minPostDate)
                        .Where(x => x.Date >= minPostDate)
                        .Select(x => x);

                    foreach (var wallPost in wallPosts)
                    {
                        var usersChunk = new List<UserExtended>();
                        var affectedUserIds = users.Select(x => x.Id).Distinct().ToList();

                        if (!wallPost.OwnerId.HasValue || !wallPost.Id.HasValue)
                        {
                            continue;
                        }

                        Console.WriteLine($"Get information for {wallPost.GetPostUrl()} {wallPost.Date}");

                        var likerIds = _likeService.GetUsersWhoLiked(wallPost.OwnerId.Value, wallPost.Id.Value,
                           LikeObjectType.Post, api);

                        likerIds = likerIds.Where(id => !affectedUserIds.Contains(id) && !blackListedUserIds.Contains(id))
                            .Select(id => id)
                            .Distinct()
                            .ToList();

                        Console.WriteLine($"likerIds count is {likerIds.Count}");
                        var usersToAdd = likerIds.Select(x => new UserExtended(new User { Id = x })).ToList();
                        usersChunk.AddRange(usersToAdd);

                        var ownerId = -wallPost.OwnerId.Value;
                        var profiles = new List<User>();
                        var commentsForPost =
                            _commentService.GetComments(0 - ownerId, wallPost.Id.Value, api, ref profiles);

                        Console.WriteLine($"comments count is {commentsForPost.Count}");

                        var totalCommentsLikersCount = 0;
                        foreach (var comment in commentsForPost)
                        {

                            if (comment.FromId.HasValue && comment.FromId.Value > 0)
                            {
                                var commentOwnerAffected = usersChunk.Select(x => x.Id).Contains(comment.FromId.Value) ||
                                     affectedUserIds.Contains(comment.FromId.Value) ||
                                     blackListedUserIds.Contains(comment.FromId.Value);

                                if (comment.Thread.Items.Any())
                                {

                                }

                                if (!commentOwnerAffected)
                                {
                                    var profile = profiles.FirstOrDefault(x => x.Id == comment.FromId);
                                    if (profile != null)
                                    {
                                        usersChunk.Add(profile.ToExtendedModel());
                                    }
                                    else
                                    {
                                        usersChunk.Add(new UserExtended(new User { Id = comment.FromId.Value }));
                                    }
                                }
                            }

                            var commentLikerIds = _likeService.GetUsersWhoLiked(wallPost.OwnerId.Value, comment.Id,
                                LikeObjectType.Comment, api);

                            totalCommentsLikersCount += commentLikerIds.Count;

                            var usersChunkIds = usersChunk.Select(x => x.Id).Distinct().ToList();
                            commentLikerIds = commentLikerIds
                                            .Where(id => !usersChunkIds.Contains(id)
                                                           && !affectedUserIds.Contains(id)
                                                           && !blackListedUserIds.Contains(id))
                                            .Distinct()
                                            .ToList();

                            usersChunk.AddRange(commentLikerIds.Select(x => new UserExtended(new User { Id = x })).ToList());
                        }

                        Console.WriteLine($"totalCommentsLikersCount count is {totalCommentsLikersCount}");

                        var newUsers = usersChunk.Where(u => !blackListedUserIds.Contains(u.Id)).Select(u => u).ToList();

                        var newUsersMessage = string.Join(", ", newUsers.Select(x => x.GetDomainForUser()));
                        Console.WriteLine($"newUsers count is {newUsers.Count}");
                        Console.WriteLine($"newUsers are {newUsersMessage}");
                        Console.WriteLine($"Total new users count is {users.Count}");

                        users.AddRange(usersChunk);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
            }

            users = users
                       .Where(u => !blackListedUserIds.Contains(u.Id))
                       .Select(u => u)
                       .ToList();
            return users;
        }
    }
}
