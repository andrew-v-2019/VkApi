using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VKApi.BL.Models;
using VKApi.BL.Models.Users;

namespace VKApi.BL.Interfaces
{
    public interface ILikeClickerService
    {
        Task<List<UserExtended>> GetUserIdsByStrategyAsync(LikeClickerStrategy strategy, List<string> groupNames, 
            AgeRange ageRange, int[] cityIds, DateTime minDateForPosts, List<long> blackListedUserIds);
    }
}
