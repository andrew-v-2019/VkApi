using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using VKApi.BL;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Services;
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

        private static List<long> _idsToExclude;


        private static void FillConfigurations()
        {
            _phrase = _configurationProvider.GetConfig("SearchPhrase");

            _reverseTotalList = Convert.ToBoolean(_configurationProvider.GetConfig("reverseTotalList"));

            var waitStr = _configurationProvider.GetConfig("Wait");
            _wait = !string.IsNullOrWhiteSpace(waitStr) ? Convert.ToDouble(waitStr) : 23;
            _city = _configurationProvider.GetConfig("City");

            var idsToExcludeStr = _configurationProvider.GetConfig("idsToExclude");
            _idsToExclude = !string.IsNullOrWhiteSpace(idsToExcludeStr)
                ? JsonConvert.DeserializeObject<List<long>>(idsToExcludeStr)
                : new List<long>();

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

            var badUsers = GetGroupsMembersByPhrase();
                
            BlackListUserList(badUsers);
        }
        
        
        private static void BlackListUserList(List<UserExtended> badUsers)
        {
            var totalUsersList = PrepareUserList(badUsers);
            var count = totalUsersList.Count;
            var counter = 0;
            using (var api = _apiFactory.CreateVkApi())
            {
                System.Console.Clear();
                foreach (var u in totalUsersList)
                {
                    var sleep = TimeSpan.FromSeconds(_wait);
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


        private static List<UserExtended> PrepareUserList(List<UserExtended> badUsers)
        {
            var blackListedUserIds = _userService.GetBannedIds().ToList().Distinct();
            System.Console.Clear();

            var badUsersFiltered = badUsers.Where(u => !blackListedUserIds.Contains(u.Id))
                .Where(u => !_idsToExclude.Contains(u.Id)).ToList();

            var badUsersOrdered = badUsersFiltered.OrderByDescending(x => x.LastActivityDate);

            badUsersOrdered = badUsersOrdered.ThenBy(u => u.IsDeactivated)
                .ThenBy(u => u.FirstName.Contains(_deletedUserText));

            if (!string.IsNullOrWhiteSpace(_city))
            {
                badUsersOrdered = badUsersOrdered.ThenByDescending(u => u.FromCity(_city));
            }

            var totalUsersList = badUsersOrdered.ToList();

            if (!_reverseTotalList)
            {
                return totalUsersList;
            }

            var reversedList = totalUsersList.Where(u => !u.FirstName.Contains(_deletedUserText)).Select(u => u)
                .ToList();

            reversedList.Reverse();

            var deleted = totalUsersList.Where(u => u.FirstName.Contains(_deletedUserText)).Select(u => u)
                .ToList();
            reversedList.AddRange(deleted);
            return reversedList;

        }


    }
}