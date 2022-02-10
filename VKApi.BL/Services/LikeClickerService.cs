using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VKApi.BL.Extensions;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VKApi.BL.Models.Users;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Model.RequestParams;

namespace VKApi.BL.Services
{
    public class LikeClickerService : ILikeClickerService
    {

        private readonly IUserService _userService;
        private readonly IGroupSerice _groupService;
        private readonly ILikesService _likesService;

        private readonly ICitiesService _citiesService;

        public LikeClickerService(IUserService userService, IGroupSerice groupService, ILikesService likesService, ICitiesService citiesService)
        {
            _userService = userService;
            _groupService = groupService;
            _likesService = likesService;
            _citiesService = citiesService;
        }

        public async Task<List<UserExtended>> GetUserIdsByStrategyAsync(LikeClickerStrategy strategy,
            List<string> groupNames, AgeRange ageRange, int[] cityIds, DateTime minDateForPosts, List<long> blackListedUserIds)
        {
            var users = new List<UserExtended>();
            var fields = GetFields();

            switch (strategy)
            {
                case LikeClickerStrategy.PostsLikers:

                    var likerIds = new List<long>();
                    foreach (var name in groupNames)
                    {
                        var group = _groupService.GetByName(name);
                        var likers = _groupService.GetGroupPostsLickers(group.Id, blackListedUserIds, minDateForPosts);
                        var likerIdsChunk = likers.Select(x => x.Id);
                        likerIds.AddRange(likerIdsChunk);
                    }

                    likerIds = likerIds.Distinct().ToList();
                    users = _userService.GetUsersByIds(likerIds, fields).ToList();
                    Console.WriteLine("Get user by ids");

                    break;
                case LikeClickerStrategy.GroupMembers:
                    var allMemebers = new List<UserExtended>();

                    foreach (var groupName in groupNames)
                    {

                        try
                        {
                            var group = _groupService.GetByName(groupName);
                            var members = _groupService.GetGroupMembers(group.Id.ToString(), UsersFields.Domain);
                            allMemebers.AddRange(members);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    
                    users = _userService.GetUsersByIds(allMemebers.Select(x => x.Id).ToList(), fields).Distinct().ToList();
                    break;

                case LikeClickerStrategy.SearchResults:
                    users = await SearchUsers(ageRange, cityIds.FirstOrDefault());
                    break;
            }

            var cities = _citiesService.GetCities(cityIds, false);

            Console.WriteLine($"Users count is {users.Count}.");
            var filteredUsers = FilterUsers(users, ageRange, cities);
            Console.WriteLine($"Filtered users count is {filteredUsers.Count}.");

            return filteredUsers;
        }

        private static List<UserExtended> FilterUsers(IEnumerable<UserExtended> users, AgeRange ageRange,
            List<CityExtended> cities)
        {
            var filteredUsers = users
                .Where(x => ShouldLike(x, ageRange, cities))
                .Select(x => x)
                .OrderBy(x => x.HasChildrens)
                .ThenBy(u => u.Age ?? 99)
                .ThenByDescending(x => x.LastActivityDate)
                .ToList();

            return filteredUsers;
        }


        private static bool ShouldLike(UserExtended user, AgeRange ageRange, List<CityExtended> cities)
        {
            if (!user.IsAgeBetween(ageRange.Min, ageRange.Max))
            {
                return false;
            }

            if (user.HasBeenOfflineMoreThanDays(2))
            {
                return false;
            }

            //  var cityIds = cities.Select(x => x.Id.Value).Distinct().ToArray();
            if (!user.FromCity(cities))
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


        private static ProfileFields GetFields()
        {
            return ProfileFields.BirthDate | ProfileFields.LastSeen | ProfileFields.City | ProfileFields.Sex |
                   ProfileFields.Blacklisted | ProfileFields.BlacklistedByMe | ProfileFields.IsFriend |
                   ProfileFields.PhotoId | ProfileFields.CommonCount | ProfileFields.Relatives |
                   ProfileFields.Relation | ProfileFields.Relatives | ProfileFields.Domain;
        }

        private async Task<List<UserExtended>> SearchUsers(AgeRange ageRange, int cityId)
        {
            var searchParams = new UserSearchParams
            {
                AgeFrom = (ushort?)ageRange.Min,
                AgeTo = (ushort?)ageRange.Max,
                Status = MaritalStatus.Single,
                Sex = Sex.Female,
                HasPhoto = true,
                Sort = UserSort.ByRegDate,
                Fields = GetFields(),
                Count = 1000,
                Country = 1,
                City = cityId
                //Online = true
            };

            var users = await _userService.Search(searchParams);
            return users;
        }
    }
}
