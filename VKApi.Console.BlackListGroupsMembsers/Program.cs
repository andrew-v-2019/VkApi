using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKApi.BL;
using VKApi.BL.Interfaces;

namespace VKApi.Console.BlackListGroupsMembsers
{
    class Program
    {
        private static void ConfigureServices()
        {
            ServiceInjector.Register<IGroupSerice, GroupService>();
            ServiceInjector.Register<IConfigurationProvider, ConfigurationProvider>();
            ServiceInjector.Register<IVkApiFactory, VkApiFactory>();
            ServiceInjector.Register<IUserService, UserService>();
        }

        static void Main(string[] args)
        {
            ConfigureServices();

            var configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();
            var groupService = ServiceInjector.Retrieve<IGroupSerice>();

            var phrase = configurationProvider.GetConfig("SearchPhrase");
            var waitStr = configurationProvider.GetConfig("Wait");
            var city = configurationProvider.GetConfig("City");

            double wait = Convert.ToDouble(waitStr);
            System.Console.Clear();
            groupService.BlackListGroupMembsersByGroupName(phrase, wait, city);
        }
    }
}
