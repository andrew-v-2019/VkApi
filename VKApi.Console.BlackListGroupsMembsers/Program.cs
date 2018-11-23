using System;
using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums;
using VKApi.BL;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Unity;

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
        private static int _secondsToSleepAfterOwnerIdIsincorrect = 1;
        private static int _secondsToSleepAfterOtherExceptions = 1;
        private static int _hoursToSleepAfterFloodControl = 12;

        private static bool _reverseTotalList = false;

        private static int _groupSearchCount = 1000;

        private static List<long> _idsToExclude = new List<long>();

        private static bool _excludeDeletedUsers;

        private const int MinAge = 37;
        private const int MaxAge = 90;

        private static int _minutesToSleepInBackgroundWork = 5;

        private static void FillConfigurations()
        {
            _excludeDeletedUsers = true;
            _phrase = _configurationProvider.GetConfig("SearchPhrase");
            _reverseTotalList = _configurationProvider.GetConfig("reverseTotalList", false);
            _wait = _configurationProvider.GetConfig("Wait", 23); 
            _city = _configurationProvider.GetConfig("City");
            _idsToExclude = _configurationProvider.GetConfig("idsToExclude", _idsToExclude);

            _deletedUserText = "DELETED";
            _secondsToSleepAfterOwnerIdIsincorrect = 1;
            _secondsToSleepAfterOtherExceptions = 1;
            _hoursToSleepAfterFloodControl = 4;

            _groupSearchCount = 1000;
        }

        private static void Main()
        {
            ServiceInjector.ConfigureServices();

            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();

            FillConfigurations();

            _groupSerice = ServiceInjector.Retrieve<IGroupSerice>();
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();

            System.Console.Clear();

            System.Console.WriteLine("Start collecting group members");
            var badUsers = GetGroupsMembersByPhrase();


            System.Console.WriteLine($"About to prepare list badUsers.Count is {badUsers.Count}");
            var totalUsersList = PrepareUserList(badUsers);
            System.Console.WriteLine($"List has been prepared, totalUsersList.Count is {totalUsersList.Count}");

            BlackListUserList(totalUsersList);

            System.Console.WriteLine("Start collecting users for background work");
            var blackListedUserIds = _userService.GetBannedIds().ToList().Distinct();
            foreach (var blackListedUserId in blackListedUserIds)
            {
                System.Console.WriteLine($"Get friends for UserId = {blackListedUserId}");

                List<UserExtended> enemyFriends;
                try
                {
                    enemyFriends = _userService.GetFriends(blackListedUserId);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                    continue;
                }

                var sleep = TimeSpan.FromMinutes(_minutesToSleepInBackgroundWork);
                using (var api = _apiFactory.CreateVkApi())
                {
                    var counter = 1;
                    foreach (var f in enemyFriends)
                    {
                        if (f.IsFriend.HasValue && f.IsFriend.Value)
                        {
                            continue;
                        }

                        if (f.Sex == Sex.Female && f.AgeVisible() && f.IsAgeBetween(MinAge, MaxAge))
                        {
                            BlackListUser(f, api, ref counter, ref sleep, enemyFriends.Count);
                        }
                        else
                        {
                            if (f.Sex == Sex.Male)
                            {
                                BlackListUser(f, api, ref counter, ref sleep, enemyFriends.Count);
                            }
                        }
                        counter++;
                        System.Threading.Thread.Sleep(sleep);
                    }
                }
            }
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

        private static void BlackListUser(UserExtended u, VkApi api, ref int counter, ref TimeSpan sleep, int count)
        {
            string message;
            try
            {
                api.Account.SetOffline();
                var r = _userService.BanUser(u, api);
                var domain = $"vk.com/{u.Domain}";
                message =
                    $"{domain} - {(r ? "banned" : "passed")}. Time {DateTime.Now}. {counter} out of {count}. {count - counter} - left.";

                counter++;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("owner_id is incorrect"))
                {
                    message =
                        $"vk.com/{u.Domain} - deleted. Time {DateTime.Now}. {counter} out of {count}. {count - counter} - left.";
                    counter++;
                    sleep = TimeSpan.FromSeconds(_secondsToSleepAfterOwnerIdIsincorrect);
                }
                else
                {
                    if (e.Message.ToLower().Contains("flood"))
                    {
                        message = "flood control. Sleeping...";
                        sleep = TimeSpan.FromHours(_hoursToSleepAfterFloodControl);
                    }
                    else
                    {
                        message = e.Message.Trim();
                        sleep = TimeSpan.FromSeconds(_secondsToSleepAfterOtherExceptions);
                    }
                }
            }

            System.Console.WriteLine(message);
        }

        private static void BlackListUserList(List<UserExtended> badUsers)
        {
            var count = badUsers.Count;
            var counter = 1;
            using (var api = _apiFactory.CreateVkApi())
            {
                System.Console.Clear();
                foreach (var u in badUsers)
                {
                    var sleep = TimeSpan.FromSeconds(_wait);
                    BlackListUser(u, api, ref counter, ref sleep, count);
                    System.Threading.Thread.Sleep(sleep);
                }
            }
        }


        private static List<UserExtended> GetGroupsMembersByPhrase()
        {
            var groups = _groupSerice.GetGroupsBySearchPhrase(_phrase, _groupSearchCount);
            var badUsers = new List<UserExtended>();

            foreach (var g in groups)
            {
                var groupBadUsers = _groupSerice.GetGroupMembers(g.Id.ToString()).ToList();
                badUsers.AddRange(groupBadUsers);
            }

            return badUsers.Distinct().ToList();
        }

     
    }
}