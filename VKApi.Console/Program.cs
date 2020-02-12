using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VKApi.BL;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Unity;
using VkNet.Model.Attachments;
using VkNet;
using VkNet.Model.RequestParams;
using System.Threading.Tasks;
using VkNet.Model;

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

        private const string GroupName = "online_krsk24";// "vpispatrol";//"poisk_krsk";// "znakomstva_krasnoyarsk124";sexykrsk seksznakomstvadivnogorsk kras.znakomstva  rmes_krs  krasn25  len_oblstroy  krsk_vyb  krasnoyarsk_krk

        private static string[] _groupNames = new[] {
            "online_krsk24", 
            "poisk_krsk", 
            "kras.znakomstva", 
            "znakomstva_krasnoyarsk124", 
            "krasn25", 
            "krsk_vyb", 
            "krasnoyarsk_krk", 
            "motoparakrsk",
            "dosuganet", 
            "vpispatrol", 
            "sprintkrsk", 
            "znakomstvokrasnoyarsk", 
            "krasznak0mstva", 
            "your_krsk", 
            "vpiskikrsk_24", 
            "hidiv",
            "public162869193",
            "club181811049",
            "znakomstva_dg",
            "24kuni"
            
        };

        private const ulong PostsCountToAnalyze = 1000;
        private static readonly string[] Cities = { "krasnoyarsk", "divnogorsk" };
        private const int ProfilePhotosToLike = 2;

        private const int MinAge = 17;
        private const int MaxAge = 29;
        private const int SkipRecentlyLikedProfilesPhotosCount = 1;

        private const Strategy Strategy = ChicksLiker.Strategy.PostsLikers;


        private static async Task<List<UserExtended>> GetUserIdsByStrategyAsync()
        {
            switch (Strategy)
            {
                case Strategy.PostsLikers:
                    var posts = new List<Post>();

                    _groupNames = _groupNames.Distinct().ToArray();
                    var likerIds = new List<long>();

                    foreach (var name in _groupNames)
                    {
                        var groupPosts = _groupService.GetPosts(name, PostsCountToAnalyze);
                        posts.AddRange(groupPosts);
                        foreach (var post in groupPosts)
                        {
                            var ownerId = post.OwnerId;
                            var postId = post.Id;

                            if (!ownerId.HasValue || !postId.HasValue) 
                                continue;

                            var likerIdsChunk = _likesService.GetUsersWhoLiked(ownerId.Value, post.Id.Value, LikeObjectType.Post);
                            likerIds.AddRange(likerIdsChunk.Distinct());
                            Console.Clear();
                            Console.WriteLine($"user ids count: {likerIds.Count}");
                        }
                    }

                    likerIds = likerIds.Distinct().ToList();
                    Console.WriteLine("Get user by ids");
                    var chunk = _userService.GetUsersByIds(likerIds);
                    return chunk;
                case Strategy.GroupMembers:
                    var group = _groupService.GetByName(GroupName);
                    var members = _groupService.GetGroupMembers(group.Id.ToString(), UsersFields.Domain);
                    var fields = GetFields();
                    members = _userService.GetUsersByIds(members.Select(x => x.Id).ToList(), fields).Distinct().ToList();
                    return members;

                case Strategy.SearchResults:
                    var searchResults = await SearchUsers();
                    return searchResults;
            }

            return new List<UserExtended>();
        }

        private static async Task<List<UserExtended>> SearchUsers()
        {
            var searchParams = new UserSearchParams()
            {
                AgeFrom = MinAge,
                AgeTo = MaxAge,
                Status = MaritalStatus.Single,
                Sex = Sex.Female,
                HasPhoto = true,
                Sort = UserSort.ByRegDate,
                Fields = GetFields(),
                Count = 1000,
                Country = 1,
                City = 73 //641
                //Online = true
            };

            var users = await _userService.Search(searchParams);
            return users;
        }


        private static async Task Main()
        {
            var ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            ServiceInjector.ConfigureServices();
            InjectServices();
            Console.Clear();

            Console.WriteLine("Get user ids...");

            var users = await GetUserIdsByStrategyAsync();
            Console.WriteLine($"User ids count is {users.Count}.");

            using (var api = _apiFactory.CreateVkApi())
            {
                var filteredUsers = users
                    .Where(ShouldLike)
                    .Select(x => x)
                    .OrderBy(x => x.HasChildrens)
                    .ThenBy(u => u.Age ?? 99)
                    .ThenByDescending(x => x.LastActivityDate)
                    .ToList();

                Console.WriteLine($"Filtered users count is {filteredUsers.Count}.");

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
            var recentliLikedCount = 0;

            if (SkipRecentlyLikedProfilesPhotosCount <= 0)
            {
                return false;
            }

            var skip = false;
            foreach (var photo in profilePhotos)
            {
                if (photo.Likes.UserLikes)
                {
                    recentliLikedCount++;
                }
                if (recentliLikedCount >= SkipRecentlyLikedProfilesPhotosCount)
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
                if (likedPhotosCounter >= ProfilePhotosToLike)
                {
                    break;
                }
            }

            return result;
        }

        private static ProfileFields GetFields()
        {
            return ProfileFields.BirthDate | ProfileFields.LastSeen | ProfileFields.City | ProfileFields.Sex |
                   ProfileFields.Blacklisted | ProfileFields.BlacklistedByMe | ProfileFields.IsFriend |
                   ProfileFields.PhotoId | ProfileFields.CommonCount | ProfileFields.Relatives |
                   ProfileFields.Relation | ProfileFields.Relatives | ProfileFields.Domain;
        }

        private static bool ShouldLike(UserExtended user)
        {
            if (!user.IsAgeBetween(MinAge, MaxAge))
            {
                return false;
            }

            if (user.HasBeenOfflineMoreThanDays(2))
            {
                return false;
            }

            if (!user.FromCity(Cities))
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
