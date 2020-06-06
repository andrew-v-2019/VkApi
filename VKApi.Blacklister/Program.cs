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
using VKApi.Console.Blacklister.Extensions;
using VkNet.Model.RequestParams;
using VKApi.BL.Services;
using VkNet.Enums.SafetyEnums;
using System.Globalization;

namespace VKApi.Console.Blacklister
{
    internal static class Program
    {
        private static IVkApiFactory _apiFactory;

        private static IConfigurationProvider _configurationProvider;

        private static IUserService _userService;

        private static IGroupSerice _groupService;

        private static ILikesService _likeService;

        private static string _phrase;
        private static double _wait;
        private static string _city;

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

        private static void FillConfigurations()
        {
            _excludeDeletedUsers = true;
            _phrase = _configurationProvider.GetConfig("SearchPhrase");
            _reverseTotalList = _configurationProvider.GetConfig("reverseTotalList", false);
            _wait = _configurationProvider.GetConfig("Wait", 23);
            _city = _configurationProvider.GetConfig("City");
            _idsToExclude = _configurationProvider.GetConfig("idsToExclude", _idsToExclude);

            _deletedUserText = "DELETED";
            _secondsToSleepAfterOwnerIdIsIncorrect = 1;
            _secondsToSleepAfterOtherExceptions = 1;
            _hoursToSleepAfterFloodControl = 4;

            _groupSearchCount = 1000;
        }

        private static async Task Main()
        {
            var ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            ServiceInjector.ConfigureServices();

            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();

            FillConfigurations();

            _groupService = ServiceInjector.Retrieve<IGroupSerice>();
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
            _likeService = ServiceInjector.Retrieve<ILikesService>();

            System.Console.Clear();

            System.Console.WriteLine("Start collecting group members...");
            var badUsers = GetGroupsMembersByPhrase();


            System.Console.WriteLine($"About to prepare list badUsers.Count is {badUsers.Count}.");
            var totalUsersList = PrepareUserList(badUsers);
            System.Console.WriteLine($"List has been prepared, totalUsersList.Count is {totalUsersList.Count}.");

            BlackListUserList(totalUsersList);

            System.Console.WriteLine("Start collecting users for background work");
            var usersForBackgroundWork = await GetUsersForBackgroundWork();

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

        private static async Task<List<UserExtended>> GetUsersForBackgroundWork()
        {
            const int cityId = 73; //ToDo: refactor this shit
            var param = new UserSearchParams()
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

            if (user.Sex == Sex.Female && user.AgeVisible() && user.IsAgeBetween(MinAge, MaxAge))
            {
                return true;
            }
            return user.Sex == Sex.Male;
        }

        private static UsersFields GetFields()
        {
            return UsersFields.LastSeen | UsersFields.City | UsersFields.Domain;
        }

        private static List<UserExtended> PrepareUserList(IEnumerable<UserExtended> badUsers)
        {
            var blackListedUserIds = _userService.GetBannedIds().ToList().Distinct();
            System.Console.Clear();

            var badUsersFiltered = badUsers.Where(u => !blackListedUserIds.Contains(u.Id))
                .Where(u => !_idsToExclude.Contains(u.Id))
                .ToList();

            var badUsersOrdered = badUsersFiltered.OrderByDescending(x => x.LastActivityDate);

            badUsersOrdered = badUsersOrdered.ThenBy(u => u.IsDeactivated)
                .ThenBy(u => u.FirstName.Contains(_deletedUserText));

            if (!string.IsNullOrWhiteSpace(_city))
            {
                badUsersOrdered = badUsersOrdered.ThenByDescending(u => u.FromCity(_city));
            }

            var totalUsersList = badUsersOrdered.ToList();

            if (_excludeDeletedUsers)
            {
                totalUsersList = ExcludeDeletedUsers(totalUsersList);
            }

            if (!_reverseTotalList)
            {
                return totalUsersList;
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
            var listWithoutDeleted = listWithDeleted.Where(u => !u.FirstName.Contains(_deletedUserText))
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
            }
            else
            {
                if (e.IsFloodControl())
                {
                    message = "Flood control. Sleeping...";
                    timeToSleepAfterError = TimeSpan.FromHours(_hoursToSleepAfterFloodControl);
                }
                else
                {
                    var domain = userThrowsException.GetDomainForUser();
                    message = domain + " - " + e.Message.Trim();
                    timeToSleepAfterError = TimeSpan.FromSeconds(_secondsToSleepAfterOtherExceptions);
                }
            }
            return timeToSleepAfterError;
        }


        private static List<UserExtended> GetGroupsMembersByPhrase()
        {
            var groups = _groupService.GetGroupsBySearchPhrase(_phrase, _groupSearchCount);
            var badUsers = new List<UserExtended>();

            System.Console.WriteLine($"Groups count is {groups.Count}...");

            foreach (var g in groups)
            {
                System.Console.WriteLine($"Getting memebers for group {g.Id} (vk.com/club{g.Id})");
                try
                {
                    var groupBadUsers = _groupService.GetGroupMembers(g.Id.ToString(), GetFields()).ToList();
                    badUsers.AddRange(groupBadUsers);
                }
                catch (Exception e)
                {
                    if (e.DoesGroupHideMembers())
                    {
                        System.Console.WriteLine($"vk.com/club{g.Id} - hides memmbers");
                        GetGroupsMembersInHiddenGroup(g.Id);
                    }

                }

            }

            return badUsers.Distinct().ToList();
        }


        private static List<UserExtended> GetGroupsMembersInHiddenGroup(long groupId)
        {
            var userIds = new List<long>();
            using (var api = _apiFactory.CreateVkApi(true))
            {
               var minDateConfig = _configurationProvider.GetConfig("minDateForPostsInHiddenGroups");
               var minDate = DateTime.ParseExact(minDateConfig, "d", CultureInfo.InvariantCulture);
               var wallPosts = _groupService.GetPostsByGroupId(groupId, api, minDate);

                foreach (var wallPost in wallPosts)
                {
                    var likerIds = _likeService.GetUsersWhoLiked(wallPost.OwnerId.Value, wallPost.Id.Value, LikeObjectType.Post, api);
                    userIds.AddRange(likerIds);
                }
            }


            return new List<UserExtended>();
        }



    }
}