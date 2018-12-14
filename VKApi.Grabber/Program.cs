using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Unity;

namespace VKApi.Grabber
{
    public class Program
    {
        private static IGroupSerice _groupService;
        private static ILikesService _likesService;
        private static IUserService _userService;
        private static IVkApiFactory _apiFactory;
        private static IPhotosService _photoService;
        private static IConfigurationProvider _configurationProvider;

        private static string SourceGroupId { get; set; }

        private static void InjectServices()
        {
            _groupService = ServiceInjector.Retrieve<IGroupSerice>();
            _likesService = ServiceInjector.Retrieve<ILikesService>();
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
            _photoService = ServiceInjector.Retrieve<IPhotosService>();
            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();

        }

        private static void FillConfigurations()
        {
            SourceGroupId = _configurationProvider.GetConfig("SourceGroupId").Replace("club", string.Empty);
        }

        private static void Main(string[] args)
        {
            ServiceInjector.ConfigureServices();
            InjectServices();
            FillConfigurations();

            var spyAccount = GetSpyAccount();

            using (var api = _apiFactory.CreateVkApi(spyAccount))
            {
                try
                {
                    var p = _groupService.GetPostsByGroupId((long) Convert.ToUInt64(SourceGroupId), api, 50);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private static Account GetSpyAccount()
        {
            var appId = _configurationProvider.GetConfig("SpyApplicationId");
            var userName = _configurationProvider.GetConfig("SpyLogin");
            var pass = _configurationProvider.GetConfig("SpyPassword");
            var account = new Account()
            {
                ApplicationId = appId,
                Login = userName,
                Password = pass
            };
            return account;
        }

        private static Account GetPublicAccount()
        {
            var appId = _configurationProvider.GetConfig("PublicApplicationId");
            var userName = _configurationProvider.GetConfig("PublicLogin");
            var pass = _configurationProvider.GetConfig("PublicPassword");
            var account = new Account()
            {
                ApplicationId = appId,
                Login = userName,
                Password = pass
            };
            return account;
        }
    }
}
