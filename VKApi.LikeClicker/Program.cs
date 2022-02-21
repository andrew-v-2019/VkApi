namespace VKApi.LikeClicker
{
    using ServiceInjector;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using BL.Interfaces;
    using BL.Models;
    using BL.Models.Users;
    using BL.Services;
    using VkNet;
    using VkNet.Enums.SafetyEnums;
    using VkNet.Model.Attachments;

    public static class Program
    {
        private static ILikesService _likesService;
        private static IVkApiFactory _apiFactory;
        private static IPhotosService _photoService;
        private static IConfigurationProvider _configurationProvider;
        private static ILikeClickerService _likeClickerService;
        private static IUserService _userService;


        private static void InjectServices()
        {
            _likesService = ServiceInjector.Retrieve<ILikesService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
            _photoService = ServiceInjector.Retrieve<IPhotosService>();
            _configurationProvider = ServiceInjector.Retrieve<IConfigurationProvider>();
            _likeClickerService = ServiceInjector.Retrieve<ILikeClickerService>();

            _userService = ServiceInjector.Retrieve<UserService>();
        }

        private static List<string> _groupNames;
        private static int _profilePhotosToLike;

        private static int _minAge;
        private static int _maxAge;
        private static int _skipRecentlyLikedProfilesPhotosCount = 1;

        private static int[] _cityIds;

        private static LikeClickerStrategy _strategy;

        private static DateTime MinDateForPosts = DateTime.Now.AddMonths(-1);

        private static void FillConfigurations()
        {
            _groupNames = _configurationProvider.GetConfig("GroupNames", _groupNames);
            _profilePhotosToLike = _configurationProvider.GetConfig("ProfilePhotosToLike", 2);
            _minAge = _configurationProvider.GetConfig("MinAge", 17);
            _maxAge = _configurationProvider.GetConfig("MaxAge", 29);
            _skipRecentlyLikedProfilesPhotosCount =
                _configurationProvider.GetConfig("SkipRecentlyLikedProfilesPhotosCount ", 1);
            _cityIds = _configurationProvider.GetConfig("CityIds", _cityIds);
            _strategy = _configurationProvider.GetConfig("Strategy", _strategy);

            var minDateConfig = _configurationProvider.GetConfig("MinDateForPosts");

            if (!string.IsNullOrWhiteSpace(minDateConfig))
            {
                MinDateForPosts = DateTime.ParseExact(minDateConfig, "d", CultureInfo.InvariantCulture);
            }
        }

        private static async Task Main()
        {
            var ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            ServiceInjector.ConfigureServices();
            InjectServices();
            FillConfigurations();

            Console.Clear();
            Console.WriteLine("Get user ids...");

            var blackListedUserIds = _userService.GetBannedIds().Distinct().ToList();

            var filteredUsers = await _likeClickerService.GetUserIdsByStrategyAsync(_strategy,
                _groupNames, new AgeRange(_minAge, _maxAge), _cityIds, MinDateForPosts, blackListedUserIds);

            using (var api = _apiFactory.CreateVkApi())
            {
                var counter = 0;
                var count = filteredUsers.Count - 1;
                do
                {
                    if (!filteredUsers.Any())
                    {
                        break;
                    }

                    var user = filteredUsers[counter];

                    var wait = (counter % 2 > 0) ? 3 : 4;
                    if (user.Age % 2 > 0)
                    {
                        wait = (counter % 2 > 0) ? 2 : new Random().Next(1, 5);
                    }

                    try
                    {
                        var profilePhotos = _photoService.GetProfilePhotos(user.Id);
                        var skip = SkipRecentlyLiked(profilePhotos);
                        var result = false;

                        if (!skip)
                        {
                            result = LikeProfilePhotos(profilePhotos, api, user);
                        }

                        counter++;

                        var message =
                            $"vk.com/{user.Domain} - {(result ? "liked" : "passed")}.{Environment.NewLine}Last activity date: {user.LastActivityDate}, age: {user.Age}, has child: {user.HasChildrens.ToString().ToLower()}.{Environment.NewLine}Time {DateTime.Now}. {counter} out of {count}";

                        Console.WriteLine(message);

                        if (result)
                        {
                            api.Account.SetOffline();
                            System.Threading.Thread.Sleep(TimeSpan.FromMinutes(wait));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception:" + e.Message);
                        System.Threading.Thread.Sleep(TimeSpan.FromMinutes(5));
                    }
                } while (counter < count);
            }

            Console.ReadLine();
        }

        private static bool SkipRecentlyLiked(List<Photo> profilePhotos)
        {
            var recentlyLikedCount = 0;

            if (_skipRecentlyLikedProfilesPhotosCount <= 0)
            {
                return false;
            }

            var skip = false;
            foreach (var photo in profilePhotos)
            {
                if (photo.Likes.UserLikes)
                {
                    recentlyLikedCount++;
                }

                if (recentlyLikedCount >= _skipRecentlyLikedProfilesPhotosCount)
                {
                    skip = true;
                    break;
                }
            }

            return skip;
        }

        private static bool LikeProfilePhotos(List<Photo> profilePhotos, VkApi api, UserExtended user)
        {
            var result = false;

            var likedPhotosCounter = 0;
            foreach (var profilePhoto in profilePhotos)
            {
                if (profilePhoto.Likes.UserLikes)
                {
                    continue;
                }

                if (profilePhoto.Id.HasValue)
                {
                    result = _likesService.AddLike(user.Id, profilePhoto.Id.Value, LikeObjectType.Photo,
                        api);
                    likedPhotosCounter++;
                }

                if (likedPhotosCounter >= _profilePhotosToLike)
                {
                    break;
                }
            }

            return result;
        }
    }
}