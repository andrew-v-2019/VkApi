using System;
using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VKApi.BL;
using VKApi.BL.Interfaces;

namespace VKApi.Grabber
{
    class Program
    {
        private static void ConfigureServices()
        {
            ServiceInjector.Register<IGroupSerice, GroupService>();
            ServiceInjector.Register<IConfigurationProvider, ConfigurationProvider>();
            ServiceInjector.Register<IVkApiFactory, VkApiFactory>();
            ServiceInjector.Register<ILikesService, LikesService>();
            ServiceInjector.Register<IUserService, UserService>();
            ServiceInjector.Register<IPhotosService, PhotosService>();

        }

        private static IGroupSerice _groupService;
        private static ILikesService _likesService;
        private static IUserService _userService;
        private static IVkApiFactory _apiFactory;

        private static void InjectServices()
        {
            _groupService = ServiceInjector.Retrieve<IGroupSerice>();
            _likesService = ServiceInjector.Retrieve<ILikesService>();
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
        }


        static void Main(string[] args)
        {
            ConfigureServices();
            InjectServices();
            var posts=  _groupService.GetPosts("freeabakan", 1);


            //var target
        }
    }
}
