﻿using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Enums;
using VkNet.Enums.SafetyEnums;
using VKApi.BL;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Unity;

namespace VKApi.ChicksLiker
{
    public static class Program
    {
        private static IGroupSerice _groupService;
        private static ILikesService _likesService;
        private static IUserService _userService;
        private static IVkApiFactory _apiFactory;
        private static IPhotosService _photoService;

        private static void InjectServices()
        {
            _groupService = ServiceInjector.Retrieve<IGroupSerice>();
            _likesService = ServiceInjector.Retrieve<ILikesService>();
            _userService = ServiceInjector.Retrieve<IUserService>();
            _apiFactory = ServiceInjector.Retrieve<IVkApiFactory>();
            _photoService = ServiceInjector.Retrieve<IPhotosService>();
        }

        private const string GroupName = "poisk_krsk";
        private const ulong PostsCountToAnalyze = 1000;
        private const string City = "krasnoyarsk";
        private const int ProfilePhotosToLike = 2;

        private const int minAge = 21;
        private const int maxAge = 27;

        private static void Main()
        {
            ServiceInjector.ConfigureServices();
            InjectServices();
            Console.Clear();

            var users = new List<UserExtended>();
           
            var posts = _groupService.GetPosts(GroupName, PostsCountToAnalyze);
            var id = posts.First()?.OwnerId;

            if (id == null)
            {
                return;
            }

            var ownerId = id.Value;
            var postIds = posts.Where(x => x.Id.HasValue).Select(x => x.Id.Value).ToList();
            var likerIds = _likesService.GetUsersWhoLiked(ownerId, postIds, LikeObjectType.Post);

            var chunk = _userService.GetUsersByIds(likerIds);           
            users.AddRange(chunk);

            Console.Clear();
            using (var api = _apiFactory.CreateVkApi())
            {
                var filteredUsers = users
                    .Where(ShouldLike)
                    .Select(x => x)
                    .OrderBy(x => x.HasChildrens)
                    .ThenBy(u => u.Age ?? 99)
                    .ThenByDescending(x => x.LastActivityDate)
                    .ToList();

                var counter = 0;
                var count = filteredUsers.Count - 1;
                Console.Clear();
                do
                {                                        
                    var user = filteredUsers[counter];

                    var wait = (counter % 2 > 0) ? 5 : 6;
                    if (user.Age % 2 > 0)
                    {
                        wait = (counter % 2 > 0) ? 4 : 7;
                    }

                    var photoId = user.GetPhotoId();
                    var profilePhotos = _photoService.GetProfilePhotos(user.Id, ProfilePhotosToLike);
                    try
                    {
                        var result = false;

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
                            }
                        }

                        counter++;

                        var message =
                            $"vk.com/{user.Domain} - {(result ? "liked" : "passed")}.{Environment.NewLine}Last activity date: {user.LastActivityDate}, age: {user.Age}, has child: {user.HasChildrens.ToString().ToLower()}.{Environment.NewLine}Time {DateTime.Now}. {counter} out of {count}";

                        Console.WriteLine(message);

                        if (result)
                        {
                            System.Threading.Thread.Sleep(TimeSpan.FromMinutes(wait));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception:" + e.Message);
                        System.Threading.Thread.Sleep(TimeSpan.FromMinutes(20));
                    }

                } while (counter < count);
            }

            Console.ReadLine();
        }

        private static bool ShouldLike(UserExtended user)
        {
            if (!user.IsAgeBetween(minAge, maxAge))
            {
                return false;
            }

            if (user.HasBeenOfflineMoreThanDays(5))
            {
                return false;
            }

            if (!user.FromCity(City))
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
            return user.CommonCount == 0 || !user.CommonCount.HasValue;
        }
    }
}
