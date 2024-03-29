﻿

namespace ServiceInjector
{
    using Inst.Api.Services;
    using Inst.Api.Services.Interfaces;
    using Unity;
    using Unity.Lifetime;
    using VKApi.BL;
    using VKApi.BL.Interfaces;
    using VKApi.BL.Services;
    
    public static class ServiceInjector
    {
        private static readonly UnityContainer UnityContainer = new UnityContainer();
        public static void Register<I, T>() where T : I
        {
            UnityContainer.RegisterType<I, T>(new ContainerControlledLifetimeManager());
        }
 
        public static T Retrieve<T>()
        {
            return UnityContainer.Resolve<T>();
        }

        public static void ConfigureServices()
        {
            Register<IGroupSerice, GroupService>();
            Register<IConfigurationProvider, ConfigurationProvider>();
            Register<IVkApiFactory, VkApiFactory>();
            Register<ILikesService, LikesService>();
            Register<IUserService, UserService>();
            Register<IPhotosService, PhotosService>();
            Register<ICommentsService, CommentsService>();
            Register<IMessagesService, MessagesService>();
            Register<ICitiesService, CitiesService>();
            Register<ILikeClickerService, LikeClickerService>();
            Register<ICacheService, CacheService>();
            Register<ICacheService, CacheService>();
            Register<IInstApiFactory, InstApiFactory>();
        }
    }
}