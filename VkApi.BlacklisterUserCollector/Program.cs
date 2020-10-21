using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using VKApi.BL.Extensions;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Unity;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;

namespace VkApi.BlacklisterUserCollector
{
    class Program
    {
        private static IVkApiFactory _apiFactory;
        private static IConfigurationProvider _configurationProvider;
        private static IUserService _userService;
        private static IGroupSerice _groupService;
        private static ILikesService _likeService;
        private static ICommentsService _commentsService;
        private static IMessagesService _messagesService;
        private static ICitiesService _citiesService;
        private static ILikeClickerService _likeClickerService;
        private static ICacheService _cacheService;

        private static long[] _blacklistMembersOfChatId;
        private static long[] _blackListGroupIds;

        private static string _primaryCacheKey = "PrimaryBlackListUsers";

        private static List<long> _badUserIds = new List<long>();

        private static void InjectServices()
        {
            _groupService = ServiceInjector.Retrieve<IGroupSerice>();
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
            _likeService = ServiceInjector.Retrieve<ILikesService>();
            _commentsService = ServiceInjector.Retrieve<ICommentsService>();
            _messagesService = ServiceInjector.Retrieve<IMessagesService>();
            _citiesService = ServiceInjector.Retrieve<ICitiesService>();
            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();
            _likeClickerService = ServiceInjector.Retrieve<ILikeClickerService>();
            _cacheService = ServiceInjector.Retrieve<ICacheService>();
        }

        private static void FillConfigurations()
        {
            _blacklistMembersOfChatId =
                _configurationProvider.GetConfig("BlacklistMembersOfChatId", _blacklistMembersOfChatId);
            _blackListGroupIds =
               _configurationProvider.GetConfig("BlackListGroupIds", _blackListGroupIds);
        }

        static void Main(string[] args)
        {
            var ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;


            ServiceInjector.ConfigureServices();
            InjectServices();
            FillConfigurations();
            Console.Clear();
            Console.WriteLine("Start collecting group members...");


            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
                var blackListedUserIds = _userService.GetBannedIds().Distinct().ToList();
                var badUsers = GetGroupsMembersByGroupIds(_blackListGroupIds, blackListedUserIds, _badUserIds);
                var chatUsers = _messagesService.GeChatUsers(_blacklistMembersOfChatId.ToList(), true);
                Console.WriteLine($"chatUsers count is {chatUsers.Count}");

                _badUserIds.AddRange(chatUsers.Select(x => x.Id).ToList());
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, EventArgs e)
        {
            if (!_badUserIds.Any())
                return;

            _cacheService.Create(_badUserIds, _primaryCacheKey);
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (!_badUserIds.Any())
                return;

            _cacheService.Create(_badUserIds, _primaryCacheKey);
        }

        private static UsersFields GetFields()
        {
            return UsersFields.LastSeen | UsersFields.City | UsersFields.Domain;
        }

        private static List<UserExtended> GetGroupsMembersByGroupIds(long[] blackListGroupIds, List<long> blackListedUserIds, List<long> result)
        {
            var badUsers = new List<UserExtended>();


            foreach (var groupId in blackListGroupIds)
            {
                Console.WriteLine($"Getting members for group {groupId} (vk.com/club{groupId})");
                try
                {
                    var groupBadUsers = _groupService.GetGroupMembers(groupId.ToString(), GetFields()).ToList();
                    badUsers.AddRange(groupBadUsers);
                    var badUserIds = groupBadUsers.Select(x => x.Id).ToList();
                    result.AddRange(badUserIds);
                }
                catch (Exception e)
                {
                    if (!e.DoesGroupHideMembers()) continue;

                    Console.WriteLine($"vk.com/club{groupId} - hides members");
                    GetGroupsMembersInHiddenGroup(groupId, blackListedUserIds, result);

                }
            }

            return badUsers.Distinct().ToList();
        }

        private static List<UserExtended> GetGroupsMembersInHiddenGroup(long groupId, List<long> blackListedUserIds, List<long> result)
        {
            var users = new List<UserExtended>();
            using (var api = _apiFactory.CreateVkApi(true))
            {
                var minDateConfig = _configurationProvider.GetConfig("minDateForPostsInHiddenGroups");
                var minDate = DateTime.ParseExact(minDateConfig, "d", CultureInfo.InvariantCulture);
                var wallPosts = _groupService.GetPostsByGroupId(groupId, api, minDate).Where(x => x.Date >= minDate).Select(x => x);

                foreach (var wallPost in wallPosts)
                {
                    if (!wallPost.OwnerId.HasValue || !wallPost.Id.HasValue)
                    {
                        continue;
                    }

                    System.Console.WriteLine();
                    System.Console.WriteLine($"Get information for {wallPost.GetPostUrl()}  {wallPost.Date}");

                    var likerIds = _likeService.GetUsersWhoLiked(wallPost.OwnerId.Value, wallPost.Id.Value,
                        LikeObjectType.Post, api);

                    System.Console.WriteLine($"likerIds count is {likerIds.Count}");

                    var usersToAdd = likerIds.Select(x => new UserExtended(new User { Id = x })).ToList();
                    users.AddRange(usersToAdd);
                    var usersToAddIds = usersToAdd.Select(x => x.Id).ToList();
                    result.AddRange(usersToAddIds);

                    var ownerId = -wallPost.OwnerId.Value;
                    var profiles = new List<User>();
                    var commentsForPost =
                        _commentsService.GetComments(0 - ownerId, wallPost.Id.Value, api, ref profiles);

                    System.Console.WriteLine($"comments count is {commentsForPost.Count}");


                    var totalCommentsLikersCount = 0;
                    foreach (var comment in commentsForPost)
                    {
                        if (comment.FromId.HasValue && comment.FromId.Value > 0)
                        {

                            if (comment.Thread.Items.Any())
                            {

                            }

                            var profile = profiles.FirstOrDefault(x => x.Id == comment.FromId);
                            if (profile != null)
                            {
                                users.Add(profile.ToExtendedModel());
                                result.AddRange(users.Select(x => x.Id).ToList());
                            }
                            else
                            {
                                users.Add(new UserExtended(new User { Id = comment.FromId.Value }));
                                result.AddRange(users.Select(x => x.Id).ToList());
                            }
                        }

                        var commentLikerIds = _likeService.GetUsersWhoLiked(wallPost.OwnerId.Value, comment.Id,
                            LikeObjectType.Comment, api);

                        totalCommentsLikersCount += commentLikerIds.Count;
                        users.AddRange(commentLikerIds.Select(x => new UserExtended(new User { Id = x })).ToList());
                        result.AddRange(commentLikerIds);
                    }

                    System.Console.WriteLine($"totalCommentsLikersCount count is {totalCommentsLikersCount}");

                    var newUsers = users.Where(u => !blackListedUserIds.Contains(u.Id)).Select(u => u);
                    System.Console.WriteLine($"newUsers count is {newUsers.Count()}");
                }
            }

            return users.Distinct().ToList();
        }
    }
}
