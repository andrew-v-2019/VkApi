using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Model;
using VKApi.BL;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Unity;
using VkNet.Model.RequestParams;
using VkNet.Enums.SafetyEnums;
using System.Globalization;
using VKApi.BL.Extensions;
using VKApi.BL.Models.Users;

namespace VKApi.Console.Blacklister
{
    internal static class Program
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

        private static string _phrase;
        private static double _wait;
        private static int[] _cityIds;

        private static string _deletedUserText;
        private static int _secondsToSleepAfterOwnerIdIsIncorrect = 1;
        private static int _secondsToSleepAfterOtherExceptions = 1;
        private static int _hoursToSleepAfterFloodControl = 12;

        private static bool _reverseTotalList;

        private static int _groupSearchCount = 1000;

        private static List<long> _idsToExclude = new List<long>();

        private static bool _excludeDeletedUsers;

        private const int MinAge = 17;
        private const int MaxAge = 90;

        private const int MinutesToSleepInBackgroundWork = 5;
        private static long[] _blacklistMembersOfChatId;
        private static int _backgroundWorkCityId;


        private static LikeClickerStrategy _strategy;
        private static ulong _postsCountToAnalyze;
        private static string _groupName;
        private static string[] _groupNames;

        private static long[] _blackListGroupIds;

        private static readonly List<long> AffectedUserIds = new List<long>();

        private static void FillConfigurations()
        {
            _excludeDeletedUsers = false;
            _phrase = _configurationProvider.GetConfig("SearchPhrase");
            _reverseTotalList = _configurationProvider.GetConfig("reverseTotalList", false);
            _wait = _configurationProvider.GetConfig("Wait", 23);

            _cityIds = _configurationProvider.GetConfig("CityIds", _cityIds);
            _backgroundWorkCityId = _configurationProvider.GetConfig("BackgroundWorkCityId", 73);

            _idsToExclude = _configurationProvider.GetConfig("idsToExclude", _idsToExclude);

            _deletedUserText = "DELETED";
            _secondsToSleepAfterOwnerIdIsIncorrect = 1;
            _secondsToSleepAfterOtherExceptions = 1;
            _hoursToSleepAfterFloodControl = 4;
            _blacklistMembersOfChatId =
                _configurationProvider.GetConfig("BlacklistMembersOfChatId", _blacklistMembersOfChatId);

            _blackListGroupIds =
               _configurationProvider.GetConfig("BlackListGroupIds", _blackListGroupIds);

            _groupSearchCount = 1000;


            _strategy = _configurationProvider.GetConfig("Strategy ", _strategy);
            _postsCountToAnalyze = (ulong)_configurationProvider.GetConfig("PostsCountToAnalyze", 100);
            _groupName = _configurationProvider.GetConfig("GroupName");
            _groupNames = _configurationProvider.GetConfig("reverseTotalList", _groupNames);
        }

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

        private static string _primaryCacheKey = "PrimaryBlackListUsers";

        private static List<UserExtended> GetCachedBlacklistUsers()
        {
            var cachedUsers = _cacheService.Get<List<long>>(_primaryCacheKey);
            cachedUsers = cachedUsers ?? new List<long>();
            System.Console.WriteLine($"cachedUsers count is {cachedUsers.Count}.");
            var result = cachedUsers.Select(x => new UserExtended {Id = x}).ToList();
            return result;
        }

        private static async Task Main()
        {
            var ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            ServiceInjector.ConfigureServices();

            InjectServices();

            FillConfigurations();

            System.Console.Clear();

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            var blackListedUserIds = _userService.GetBannedIds().Distinct().ToList();

            //var badUsers = GetGroupsMembersByGroupIds(_blackListGroupIds, blackListedUserIds);
            //var chatUsers = _messagesService.GeChatUsers(_blacklistMembersOfChatId.ToList(), true);
            //System.Console.WriteLine($"chatUsers count is {chatUsers.Count}");
            //badUsers.AddRange(chatUsers);

            var badUsers = GetCachedBlacklistUsers();

            System.Console.WriteLine($"About to prepare list badUsers.Count is {badUsers.Count}.");
            var cities = _citiesService.GetCities(_cityIds);
            var totalUsersList = badUsers.Where(u => !blackListedUserIds.Contains(u.Id)).Select(x => x).ToList();//PrepareUserList(badUsers, blackListedUserIds, cities);
            System.Console.WriteLine($"List has been prepared, totalUsersList.Count is {totalUsersList.Count}.");

            BlackListUserList(totalUsersList);

            System.Console.WriteLine("Start collecting users for background work");
            var usersForBackgroundWork = await GetUsersForBackgroundWork(cities);

            using (var api = _apiFactory.CreateVkApi())
            {
                foreach (var f in usersForBackgroundWork)
                {

                    var needBlackList = SatisfyBySecondAlgorithm(f);
                    if (!needBlackList)
                    {
                        continue;
                    }

                    var message = string.Empty;

                    TimeSpan sleep;
                    try
                    {
                        var blackListResult = BlackListUser(f, api);
                        sleep = blackListResult
                            ? TimeSpan.FromMinutes(MinutesToSleepInBackgroundWork)
                            : TimeSpan.FromSeconds(0);
                        message = ActionResultLogMessage(f, blackListResult);
                    }
                    catch (Exception e)
                    {
                        sleep = HandleException(e, f, null, null, ref message);
                    }

                    System.Console.WriteLine(message);
                    System.Threading.Thread.Sleep(sleep);
                }
            }
        }

        private static void RefreshCache()
        {
            var userIdsBefore = GetCachedBlacklistUsers();

            var after = new List<long>();
            foreach (var userIdBefore in userIdsBefore)
            {
                if (!AffectedUserIds.Contains(userIdBefore.Id))
                {
                    after.Add(userIdBefore.Id);
                }
            }

            _cacheService.Create(after, _primaryCacheKey);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            RefreshCache();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            RefreshCache();
        }

        private static async Task<List<UserExtended>> GetUsersFromCityForBackgroundWork()
        {
            var cityId = _backgroundWorkCityId;
            var param = new UserSearchParams
            {
                Sex = Sex.Male,
                City = cityId,
                Sort = UserSort.ByRegDate,
                Fields = GetProfileFields(),
                Count = 1000,
                AgeFrom = MinAge,
                AgeTo = MaxAge
            };
            var res = await _userService.Search(param);
            res = res.Where(x => !x.BlackListed()).Select(x => x).ToList();
            return res;
        }

        private static async Task<List<UserExtended>> GetUsersForBackgroundWork(List<City> cities)
        {
            var likeClickerUsers = await _likeClickerService.GetUserIdsByStrategyAsync(_strategy, _postsCountToAnalyze,
                _groupNames, _groupName, new AgeRange(17, 30), _backgroundWorkCityId, _cityIds);

            var res = new List<UserExtended>();
            var cityNames = cities.Select(x => x.Title).Distinct().ToArray();
            foreach (var user in likeClickerUsers)
            {
                var friends = _userService.GetFriends(user.Id);

                foreach (var friend in friends)
                {
                    if (!SatisfyBySecondAlgorithm(friend))
                        continue;

                    if (!user.FromCity(cities.Select(x => x.Id.Value).ToArray()))
                        continue;

                    res.Add(friend);
                }
            }

            return res;
        }

        private static ProfileFields GetProfileFields()
        {
            return ProfileFields.BirthDate | ProfileFields.LastSeen | ProfileFields.City | ProfileFields.Sex |
                   ProfileFields.Blacklisted | ProfileFields.BlacklistedByMe | ProfileFields.IsFriend |
                   ProfileFields.PhotoId | ProfileFields.CommonCount | ProfileFields.Relatives |
                   ProfileFields.Relation | ProfileFields.Relatives | ProfileFields.Domain;
        }

        private static bool SatisfyBySecondAlgorithm(UserExtended user)
        {
            if (user.IsFriend.HasValue && user.IsFriend.Value)
            {
                return false;
            }

            //if (user.Sex == Sex.Female && user.AgeVisible() && user.IsAgeBetween(MinAge, MaxAge)) 
            //{
            //    return true;
            //}
            return user.Sex == Sex.Male;
        }

        private static UsersFields GetFields()
        {
            return UsersFields.LastSeen | UsersFields.City | UsersFields.Domain;
        }

        private static List<UserExtended> PrepareUserList(IEnumerable<UserExtended> badUsers,
            IEnumerable<long> blackListedUserIds, List<City> cities)
        {

            System.Console.Clear();

            var badUsersFiltered = badUsers.Where(u => !blackListedUserIds.Contains(u.Id))
                .Where(u => !_idsToExclude.Contains(u.Id))
                .ToList();

            var badUsersOrdered = badUsersFiltered.OrderByDescending(x => x.LastActivityDate);

            badUsersOrdered = badUsersOrdered.ThenBy(u => u.IsDeactivated)
                .ThenBy(u => !string.IsNullOrEmpty(u.FirstName) && u.FirstName.Contains(_deletedUserText));

            var cityIds = cities.Select(x => x.Id.Value).Distinct().ToArray();
            if (cityIds.Any())
            {
                badUsersOrdered = badUsersOrdered.ThenByDescending(u => u.FromCity(cityIds));
            }

            var totalUsersList = badUsersOrdered.ToList();

            if (_excludeDeletedUsers)
            {
                totalUsersList = ExcludeDeletedUsers(totalUsersList);
            }

            if (!_reverseTotalList)
            {
                return totalUsersList.ToList();
            }

            var reversedList = ExcludeDeletedUsers(totalUsersList);

            reversedList.Reverse();

            var deleted = totalUsersList.Where(u => u.FirstName.Contains(_deletedUserText))
                .SuperSelect(u => u)
                .ToList();
            reversedList.AddRange(deleted);
            return reversedList;
        }


        private static List<UserExtended> ExcludeDeletedUsers(IEnumerable<UserExtended> listWithDeleted)
        {
            var listWithoutDeleted = listWithDeleted
                .Where(u => !string.IsNullOrWhiteSpace(u.FirstName) && !u.FirstName.Contains(_deletedUserText))
                .Select(u => u)
                .ToList();
            return listWithoutDeleted;
        }


        private static string ActionResultLogMessage(User u, bool result, int? counter = null, int? count = null,
            Exception ex = null)
        {
            var message = string.Empty;
            var domain = u.GetDomainForUser();
            var timeInfoString = $" Time {DateTime.Now}.";
            var counterString = string.Empty;
            if (counter.HasValue && count.HasValue)
            {
                counterString = $" {counter} out of {count}. {count - counter} - left.";
            }

            if (result)
            {
                AffectedUserIds.Add(u.Id);
            }

            if (ex == null)
            {
                message =
                    $"{domain} - {(result ? "banned" : "passed")}. {timeInfoString}{counterString}";
            }
            else
            {
                if (ex.IsOwnerIdIncorrect())
                {
                    message = $"{domain} - deleted. {timeInfoString}";
                }
            }

            return message;
        }

        private static bool BlackListUser(UserExtended u, VkApi api)
        {
            api.Account.SetOffline();
            var result = _userService.BanUser(u, api);
            if (result)
            {
                _cacheService.Append(u.Id, CacheKeys.BannedUserIds.ToString());
            }
            return result;
        }

        private static void BlackListUserList(IReadOnlyCollection<UserExtended> badUsers)
        {
            var count = badUsers.Count;
            var counter = 1;
            using (var api = _apiFactory.CreateVkApi())
            {
                System.Console.Clear();
                var sleep = TimeSpan.FromSeconds(_wait);
                foreach (var u in badUsers)
                {
                    var message = string.Empty;
                    try
                    {
                        var blackListResult = BlackListUser(u, api);
                        message = ActionResultLogMessage(u, blackListResult, counter, count);
                    }
                    catch (Exception e)
                    {
                        sleep = HandleException(e, u, counter, count, ref message);
                    }

                    counter++;

                    System.Console.WriteLine(message);
                    System.Threading.Thread.Sleep(sleep);
                }
            }
        }

        private static TimeSpan HandleException(Exception e, User userThrowsException, int? counter, int? count,
            ref string message)
        {
            TimeSpan timeToSleepAfterError;
            if (e.IsOwnerIdIncorrect())
            {
                message = ActionResultLogMessage(userThrowsException, false, counter, count, e);
                timeToSleepAfterError = TimeSpan.FromSeconds(_secondsToSleepAfterOwnerIdIsIncorrect);
                return timeToSleepAfterError;
            }
            else
            {
                if (e.IsFloodControl())
                {
                    message = "Flood control. Sleeping...";
                    timeToSleepAfterError = TimeSpan.FromHours(_hoursToSleepAfterFloodControl);
                    return timeToSleepAfterError;
                }
                else
                {
                    var domain = userThrowsException.GetDomainForUser();
                    message = domain + " - " + e.Message.Trim();
                   
                    //throw e;
                }
            }
            timeToSleepAfterError = TimeSpan.FromSeconds(_secondsToSleepAfterOtherExceptions);

            return timeToSleepAfterError;

        }

        private static List<UserExtended> GetGroupsMembersByGroupIds(long[] blackListGroupIds, List<long> blackListedUserIds)
        {
            var badUsers = new List<UserExtended>();


            foreach (var groupId in blackListGroupIds)
            {
                System.Console.WriteLine($"Getting members for group {groupId} (vk.com/club{groupId})");
                try
                {
                    var groupBadUsers = _groupService.GetGroupMembers(groupId.ToString(), GetFields()).ToList();
                    badUsers.AddRange(groupBadUsers);
                }
                catch (Exception e)
                {
                    if (!e.DoesGroupHideMembers()) continue;

                    System.Console.WriteLine($"vk.com/club{groupId} - hides members");
                    GetGroupsMembersInHiddenGroup(groupId, blackListedUserIds);

                }

            }

            return badUsers.Distinct().ToList();
        }

        private static List<UserExtended> GetGroupsMembersInHiddenGroup(long groupId, List<long> blackListedUserIds)
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
                            }
                            else
                            {
                                users.Add(new UserExtended(new User { Id = comment.FromId.Value }));
                            }
                        }

                        var commentLikerIds = _likeService.GetUsersWhoLiked(wallPost.OwnerId.Value, comment.Id,
                            LikeObjectType.Comment, api);

                        totalCommentsLikersCount += commentLikerIds.Count;
                        users.AddRange(commentLikerIds.Select(x => new UserExtended(new User { Id = x })).ToList());
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