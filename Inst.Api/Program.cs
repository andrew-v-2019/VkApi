

using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Enums.Filters;

namespace Inst.Api
{
    using System.Threading.Tasks;
    using Services;
    using InstagramApiSharp;
    using ServiceInjector;
    using VKApi.BL.Interfaces;
    using Services.Interfaces;

    internal static class Program
    {
        private static IVkApiFactory _apiFactory;
        private static IConfigurationProvider _configurationProvider;
        private static IUserService _userService;
        private static IInstApiFactory _instApiFactory;

        private static void InjectServices()
        {
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();
            _instApiFactory = ServiceInjector.Retrieve<IInstApiFactory>();
        }

        public static async Task Main(string[] args)
        {
            ServiceInjector.ConfigureServices();
            InjectServices();

            var instApi = await _instApiFactory.Login();
            var blockedUsersResult = await instApi.UserProcessor.GetBlockedUsersAsync(PaginationParameters.Empty);

            var alreadyBlockedUsers = new List<string>();

            if (blockedUsersResult.Succeeded && blockedUsersResult.Value?.BlockedList != null)
            {
                alreadyBlockedUsers =
                    blockedUsersResult.Value.BlockedList.Select(x => x.UserName).Select(x => x).ToList();
            }


            var bannedIds = _userService.GetBannedIds();
            var bannedUsers = _userService.GetUsersByIds(bannedIds, ProfileFields.All);

            var bannedUserInsts = bannedUsers.Where(x => x.Connections != null).Select(x => x).ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x.Connections.Instagram)).Select(x => x).ToList()
                .Select(x => x.Connections.Instagram).ToList();

            bannedUserInsts = bannedUserInsts.Where(x => !alreadyBlockedUsers.Contains(x)).Select(x => x).ToList();
            

            // var insUserName = "tuckercarlsontonight";
            // var userInfo = await instApi.UserProcessor.GetUserAsync(insUserName);
            // var blockRes = await instApi.UserProcessor.BlockUserAsync(userInfo.Value.Pk);

            var count = bannedUserInsts.Count;
            var counter = 1;
            foreach (var insUserName in bannedUserInsts)
            {
                try
                {
                    var userInfo = await instApi.UserProcessor.GetUserAsync(insUserName);
                    if (userInfo.Succeeded && userInfo?.Value?.Pk != null)
                    {
                        var blockRes = await instApi.UserProcessor.BlockUserAsync(userInfo.Value.Pk);

                        if (blockRes.Succeeded)
                        {
                            Console.WriteLine(
                                $"https://www.instagram.com/{insUserName} has been blocked; {counter} out of {count}");

                            counter++;
                        }
                        else
                        {
                            Console.WriteLine($"https://www.instagram.com/{insUserName}  {blockRes.Info}");
                        }

                    }

                    var sleep = TimeSpan.FromSeconds(30);
                    System.Threading.Thread.Sleep(sleep);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}