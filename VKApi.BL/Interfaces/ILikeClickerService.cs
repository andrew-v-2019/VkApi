using System.Collections.Generic;
using System.Threading.Tasks;
using VKApi.BL.Models;
using VKApi.BL.Models.Users;

namespace VKApi.BL.Interfaces
{
    public interface ILikeClickerService
    {
        Task<List<UserExtended>> GetUserIdsByStrategyAsync(LikeClickerStrategy strategy, ulong? postsCountToAnalyze, string[] groupNames, string groupName, AgeRange ageRange, int cityId, int[] cityIds);
    }
}
