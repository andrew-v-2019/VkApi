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

        private static IMessagesService _messagesService;
        private static ICitiesService _citiesService;
        private static ILikeClickerService _likeClickerService;

        private static ICacheService _cacheService;

        private static double _wait;
        private static int[] _cityIds;

        private static string _deletedUserText;
        private static int _secondsToSleepAfterOwnerIdIsIncorrect = 1;
        private static int _secondsToSleepAfterOtherExceptions = 1;
        private static int _hoursToSleepAfterFloodControl = 12;

        private static bool _reverseTotalList;


        private static List<long> _idsToExclude = new List<long>();

        private static bool _excludeDeletedUsers;

        private const int MinAge = 17;
        private const int MaxAge = 90;

        private const int MinutesToSleepInBackgroundWork = 2;
        private static long[] _blacklistMembersOfChatId;


        private static LikeClickerStrategy _strategy;

        private static List<string> _groupNames;

        private static long[] _blackListGroupIds;


        private static long[] _blacklistFriendsOfUserId;

        private static readonly List<long> AffectedUserIds = new List<long>();

        private static DateTime MinDateForPostsBackGroundWork = DateTime.Now.AddMonths(-1);

        private static void FillConfigurations()
        {
            _excludeDeletedUsers = true;
            _reverseTotalList = _configurationProvider.GetConfig("reverseTotalList", false);
            _wait = _configurationProvider.GetConfig("Wait", 23);

            _cityIds = _configurationProvider.GetConfig("CityIds", _cityIds);

            _idsToExclude = _configurationProvider.GetConfig("idsToExclude", _idsToExclude);

            _deletedUserText = "DELETED";
            _secondsToSleepAfterOwnerIdIsIncorrect = 1;
            _secondsToSleepAfterOtherExceptions = 1;
            _hoursToSleepAfterFloodControl = 4;
            _blacklistMembersOfChatId =
                _configurationProvider.GetConfig("BlacklistMembersOfChatId", _blacklistMembersOfChatId);

            _blacklistFriendsOfUserId = _configurationProvider.GetConfig("BlacklistFriendsOfUserId", _blacklistFriendsOfUserId);

            _blackListGroupIds =
               _configurationProvider.GetConfig("BlackListGroupIds", _blackListGroupIds);


            _strategy = _configurationProvider.GetConfig("Strategy", _strategy);
            _groupNames = _configurationProvider.GetConfig("GroupNames", _groupNames) ?? new List<string>();

            var minDateConfig = _configurationProvider.GetConfig("MinDateForPostsInBackgroundWork");

            if (!string.IsNullOrWhiteSpace(minDateConfig))
            {
                MinDateForPostsBackGroundWork = DateTime.ParseExact(minDateConfig, "d", CultureInfo.InvariantCulture);
            }
        }

        private static void InjectServices()
        {
            _groupService = ServiceInjector.Retrieve<IGroupSerice>();
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
            _messagesService = ServiceInjector.Retrieve<IMessagesService>();
            _citiesService = ServiceInjector.Retrieve<ICitiesService>();
            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();
            _likeClickerService = ServiceInjector.Retrieve<ILikeClickerService>();
            _cacheService = ServiceInjector.Retrieve<ICacheService>();
        }

        private static async Task Main()
        {
            var ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            ServiceInjector.ConfigureServices();

            InjectServices();

            FillConfigurations();

            System.Console.Clear();

            var blackListedUserIds = _userService.GetBannedIds().Distinct().ToList();

            var badUsers = GetGroupsMembersByGroupIds(_blackListGroupIds, blackListedUserIds);

            if (_blacklistMembersOfChatId != null)
            {
                var chatUsers = _messagesService.GeChatUsers(_blacklistMembersOfChatId.ToList(), true);
                System.Console.WriteLine($"chatUsers count is {chatUsers.Count}");
                badUsers.AddRange(chatUsers);
            }

            //var badUsers = GetCachedBlacklistUsers();

            System.Console.WriteLine($"About to prepare list badUsers.Count is {badUsers.Count}.");
            var cities = _citiesService.GetCities(_cityIds, false);
            var totalUsersList = PrepareUserList(badUsers, blackListedUserIds, cities);
            System.Console.WriteLine($"List has been prepared, totalUsersList.Count is {totalUsersList.Count}.");

            BlackListUserList(totalUsersList);

            System.Console.WriteLine("Start collecting users for background work");
            var usersForBackgroundWork = await GetUsersForBackgroundWork(cities, blackListedUserIds, 1000);


            var friedsOfUserIds = new List<UserExtended>();
            if (_blacklistFriendsOfUserId.Any())
            {
                foreach (var userId in _blacklistFriendsOfUserId)
                {
                    var friends = _userService.GetFriends(userId);
                    friedsOfUserIds.AddRange(friends);
                }
            }

            friedsOfUserIds = friedsOfUserIds.Where(f => blackListedUserIds.Contains(f.Id)).Select(f => f).ToList();

            using (var api = _apiFactory.CreateVkApi())
            {
                foreach (var f in friedsOfUserIds)
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

        private static async Task<List<UserExtended>> GetUsersFromCityForBackgroundWork()
        {
            var cityId = _cityIds.FirstOrDefault();
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

        private static async Task<List<UserExtended>> GetUsersForBackgroundWork(List<CityExtended> cities, List<long> blackListedUserIds, int? count = null)
        {
            var likeClickerUsers = await _likeClickerService.GetUserIdsByStrategyAsync(_strategy,
                _groupNames, new AgeRange(17, 30), _cityIds,
                MinDateForPostsBackGroundWork, blackListedUserIds);

            var res = new List<UserExtended>();
            var cityNames = cities.Select(x => x.Title).Distinct().ToArray();

            likeClickerUsers = ExcludeDeletedUsers(likeClickerUsers);

            if (count.HasValue)
            {
                likeClickerUsers = likeClickerUsers.Take(count.Value).ToList();
            }

            foreach (var user in likeClickerUsers)
            {
                var friends = _userService.GetFriends(user.Id);

                System.Console.WriteLine($"Friends count for {user.GetDomainForUser()} is {friends.Count}");

                foreach (var friend in friends)
                {
                    if (!SatisfyBySecondAlgorithm(friend))
                        continue;

                    if (!friend.FromCity(cities))
                        continue;

                    res.Add(friend);
                }
            }

            res = res.Where(u => !blackListedUserIds.Contains(u.Id)).Select(x => x).ToList();
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

            var ageBetween = user.AgeVisible() && user.IsAgeBetween(37, 90);
            return user.Sex == Sex.Male || ageBetween;
        }

        private static UsersFields GetFields()
        {
            return UsersFields.LastSeen | UsersFields.City | UsersFields.Domain;
        }

        private static List<UserExtended> PrepareUserList(IEnumerable<UserExtended> badUsers,
            IEnumerable<long> blackListedUserIds, List<CityExtended> cities)
        {
            System.Console.Clear();

            var badUsersFiltered = badUsers.Where(u => !blackListedUserIds.Contains(u.Id))
                .Where(u => !_idsToExclude.Contains(u.Id))
                .ToList();

            var badUsersOrdered = badUsersFiltered.OrderByDescending(u => u.FromCity(cities));
            badUsersOrdered = badUsersOrdered.ThenByDescending(x => x.LastActivityDate);
            badUsersOrdered = badUsersOrdered.ThenBy(u => u.IsDeactivated)
                .ThenBy(u => !string.IsNullOrEmpty(u.FirstName) && u.FirstName.Contains(_deletedUserText));


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
                .Where(u => u.IsDeactivated == false)
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
                    var hideGroupUsers = GetGroupsMembersInHiddenGroup(groupId, blackListedUserIds).Distinct();
                    badUsers.AddRange(hideGroupUsers);

                }

            }

            return badUsers.Distinct().ToList();
        }

        private static List<UserExtended> GetGroupsMembersInHiddenGroup(long groupId, List<long> blackListedUserIds)
        {
            var minDateConfig = _configurationProvider.GetConfig("minDateForPostsInHiddenGroups");
            var minDate = DateTime.ParseExact(minDateConfig, "d", CultureInfo.InvariantCulture);

            var users = _groupService.GetGroupPostsLickers(groupId, blackListedUserIds, minDate);
            return users;
        }

    }
}