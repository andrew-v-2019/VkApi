
using System;
using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VKApi.BL;
using VKApi.BL.Interfaces;

namespace VKApi.Console
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

        private static void Main()
        {
            ConfigureServices();
            InjectServices();
            System.Console.Clear();

            var users = new List<User>();
            const string groupName = "poisk_krsk";
            const ulong postsTotalCount = 300;//1000;


            var posts = _groupService.GetPosts(groupName, postsTotalCount);
            var likedPosts = posts.Where(p => p.Likes.Count > 0).Select(p => p).ToList();
            var id = posts.First()?.OwnerId;

            if (id == null)
            {
                return;
            }

            var ownerId = id.Value;
            var postIds = likedPosts.Where(x => x.Id.HasValue).Select(x => x.Id.Value).ToList();
            var likerIds = _likesService.GetUsersWhoLiked(ownerId, postIds, LikeObjectType.Post);

            var chunk = _userService.GetUsersByIds(likerIds);
            users.AddRange(chunk);


            System.Console.Clear();
            using (var api = _apiFactory.CreateVkApi())
            {
                var filteredUsers = users.OrderByLsatActivityDateDesc()
                                            .Where(u => ShouldLike(u, api, _userService))
                                            .Select(x => x)
                                            .ToList();
                var counter = 0;
                var count = filteredUsers.Count - 1;
                System.Console.Clear();
                do
                {
                    var wait = (counter % 2 > 0) ? 10 : 15;
                    var user = filteredUsers[counter];
                    var photoId = user.GetPhotoId();
                    try
                    {
                        var result = _likesService.AddLike(user.Id, photoId, LikeObjectType.Photo, api);
                        counter++;
                        var message =
                            $"vk.com/{user.Domain} - {(result ? "liked" : "passed")}. Time {DateTime.Now}. {counter} out of {count}";
                        System.Console.WriteLine(message);
                        if (result)
                        {

                            System.Threading.Thread.Sleep(TimeSpan.FromMinutes(wait));
                            
                        }
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine("Exception:" + e.Message);
                        System.Threading.Thread.Sleep(TimeSpan.FromMinutes(20));
                    }
                } while (counter < count);
            }

            System.Console.ReadLine();
        }


        private static bool ShouldLike(User user, VkApi api, IUserService userService)
        {
            if (!user.IsAgeBetween(18, 35))
            {
                return false;
            }

            if (user.HasBeenOfflineMoreThanDays(5))
            {
                return false;
            }

            if (!user.FromCity("krasnoyarsk"))
            {
                return false;
            }
            if (user.Sex != Sex.Female)
            {
                return false;
            }
            if (user.BlackListed())
            {
                return false;
            }

            if (user.IsFriend.HasValue && user.IsFriend == true)
            {
                return false;
            }
            if (!user.IsSingle())
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(user.PhotoId))
            {
                return false;
            }
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
            return !userService.HaveCommonFriends(user.Id, api.UserId.Value, api);
        }
    }
}
