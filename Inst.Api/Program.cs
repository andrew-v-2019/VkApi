

using System;
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
            // var blockedUsers = await instApi.UserProcessor.GetBlockedUsersAsync(PaginationParameters.MaxPagesToLoad(1));

            var bannedIds = _userService.GetBannedIds();
            var bannedUsers = _userService.GetUsersByIds(bannedIds, ProfileFields.Connections);

            var bannedUserInsts = bannedUsers.Where(x => x.Connections != null).Select(x => x).ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x.Connections.Instagram)).Select(x => x).ToList()
                .Select(x => x.Connections.Instagram).ToList();

            foreach (var insUserName in bannedUserInsts)
            {
                var userInfo = await instApi.UserProcessor.GetUserAsync(insUserName);
                await instApi.UserProcessor.BlockUserAsync(userInfo.Value.Pk);

                var sleep = TimeSpan.FromMinutes(1);
                System.Threading.Thread.Sleep(sleep);

            }
        }
    }
}