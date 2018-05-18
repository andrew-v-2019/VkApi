
using System;
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
        private static void UnityConfig()
        {
            ServiceInjector.Register<IGroupSerice, GroupService>();
            ServiceInjector.Register<IConfigurationProvider, ConfigurationProvider>();
            ServiceInjector.Register<IVkApiFactory, VkApiFactory>();
            ServiceInjector.Register<ILikesService, LikesService>();
            ServiceInjector.Register<IUserService, UserService>();
            ServiceInjector.Register<IPhotosService, PhotosService>();

        }

        private static void Main(string[] args)
        {
            UnityConfig();
            var groupService = ServiceInjector.Retrieve<IGroupSerice>();
            var likesService = ServiceInjector.Retrieve<ILikesService>();
            var userService = ServiceInjector.Retrieve<IUserService>();
            var photoService = ServiceInjector.Retrieve<IPhotosService>();
            var apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
            

            var posts = groupService.GetPosts("poisk_krsk"); //poisk_krsk
            var likedPosts = posts.Where(p => p.Likes.Count > 0).Select(p => p).ToList();

            var ownerId = posts.First().OwnerId.Value;
            var postIds = likedPosts.Select(x => x.Id.Value).ToList();

            var likerIds = likesService.GetUsersWhoLiked(ownerId, postIds, LikeObjectType.Post);
            var users = userService.GetUsersByIds(likerIds);


            using (var api = apiFactory.CreateVkApi())
            {
                var filteredUsers = users.Where(u => ShouldLike(u, api, userService))
                    .Select(x => x)
                    .OrderByDescending(u => u.Online)
                    .ThenByDescending(u => u.LastSeen.Time)
                    .ThenByDescending(u => u.Id)
                    .ToList();
                var counter = 0;
                var count = filteredUsers.Count;
                do
                {
                    var user = filteredUsers[counter];
                    var photoId = user.GetPhotoId();
                    try
                    {
                        var result = likesService.AddLike(user.Id, photoId, LikeObjectType.Photo, api);
                        counter++;
                        var message =
                            $"vk.com/{user.Domain} - {(result ? "liked" : "passed")}. Time {DateTime.Now}. {counter} out of {count}";
                        System.Console.WriteLine(message);
                        if (result)
                        {
                            System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1));
                        }
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine("Exception:" + e.Message);
                        System.Threading.Thread.Sleep(TimeSpan.FromMinutes(15));
                    }
                } while (counter < count);
            }
        }

        private static bool ShouldLike(User user, VkApi api, IUserService userService)
        {
            if (!user.IsAgeBetween(16, 35))
            {
                return false;
            }

            if (user.HasBeenOfflineMoreThanDays(5))
            {
                return false;
            }

            if (!user.FromCity("красноярск"))
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
            return !userService.HaveCommonFriends(user.Id, api.UserId.Value, api);
        }
    }
}
