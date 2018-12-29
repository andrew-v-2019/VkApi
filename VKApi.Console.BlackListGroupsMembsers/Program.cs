using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Model;
using VKApi.BL;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Unity;
using VKApi.Console.Blacklister.Extensions;

namespace VKApi.Console.Blacklister
{
    internal static class Program
    {
        private static IVkApiFactory _apiFactory;

        private static IConfigurationProvider _configurationProvider;

        private static IUserService _userService;

        private static IGroupSerice _groupSerice;

        private static string _phrase;
        private static double _wait;
        private static string _city;

        private static string _deletedUserText;
        private static int _secondsToSleepAfterOwnerIdIsIncorrect = 1;
        private static int _secondsToSleepAfterOtherExceptions = 1;
        private static int _hoursToSleepAfterFloodControl = 12;

        private static bool _reverseTotalList = false;

        private static int _groupSearchCount = 1000;

        private static List<long> _idsToExclude = new List<long>();

        private static bool _excludeDeletedUsers;

        private const int MinAge = 37;
        private const int MaxAge = 90;

        private static readonly int _minutesToSleepInBackgroundWork = 5;

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

        private static void Main()
        {
            var ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            ServiceInjector.ConfigureServices();

            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();

            FillConfigurations();

            _groupSerice = ServiceInjector.Retrieve<IGroupSerice>();
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();

            System.Console.Clear();

            System.Console.WriteLine("Start collecting group members...");
            var badUsers = GetGroupsMembersByPhrase();


            System.Console.WriteLine($"About to prepare list badUsers.Count is {badUsers.Count}.");
            var totalUsersList = PrepareUserList(badUsers);
            System.Console.WriteLine($"List has been prepared, totalUsersList.Count is {totalUsersList.Count}.");

            BlackListUserList(totalUsersList);

            System.Console.WriteLine("Start collecting users for background work");
            var blackListedUserIds = _userService.GetBannedIds().ToList().Distinct();
            foreach (var blackListedUserId in blackListedUserIds)
            {
                System.Console.WriteLine($"Get friends for UserId = {blackListedUserId}");

                List<UserExtended> enemyFriends;
                try
                {
                    enemyFriends = _userService.GetFriends(blackListedUserId)
                        .Where(x => x.FromCity(_city))
                        .ToList();
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                    continue;
                }

                using (var api = _apiFactory.CreateVkApi())
                {
                    var counter = 1;
                    foreach (var f in enemyFriends)
                    {

                        var needBlackList = SatisfyBySecondAlgoritm(f);
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
                                ? TimeSpan.FromMinutes(_minutesToSleepInBackgroundWork)
                                : TimeSpan.FromSeconds(0);
                            message = ActionResultLogMessage(f, blackListResult);
                        }
                        catch (Exception e)
                        {
                            sleep= HandleException(e,f,null,null, ref message);
                        }

                        counter++;

                        System.Console.WriteLine(message);
                        System.Threading.Thread.Sleep(sleep);
                    }
                }
            }
        }

        private static bool SatisfyBySecondAlgoritm(UserExtended user)
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
            return UsersFields.LastSeen | UsersFields.City | UsersFields.Domain ;
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
                .Select(u => u)
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
            var domain = $"vk.com/{u.Domain}";
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
                    message = e.Message.Trim();
                    timeToSleepAfterError = TimeSpan.FromSeconds(_secondsToSleepAfterOtherExceptions);
                }
            }
            return timeToSleepAfterError;
        }


        private static List<UserExtended> GetGroupsMembersByPhrase()
        {
            var groups = _groupSerice.GetGroupsBySearchPhrase(_phrase, _groupSearchCount);
            var badUsers = new List<UserExtended>();

            System.Console.WriteLine($"Groups count is {groups.Count}...");

            foreach (var g in groups)
            {
                System.Console.WriteLine($"Getting memebers for group {g.Id} (vk.com/club{g.Id})");
                var groupBadUsers = _groupSerice.GetGroupMembers(g.Id.ToString(), GetFields()).ToList();
                badUsers.AddRange(groupBadUsers);
            }

            return badUsers.Distinct().ToList();
        }

     
    }
}