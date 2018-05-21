using System.Collections.Generic;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace VKApi.BL.Interfaces
{
    public interface IGroupSerice
    {
        List<User> GetGroupMembers(string groupName, UsersFields fields = null, int? count = null);
        List<Post> GetPosts(string groupName);
        void BlackListGroupMembsers(string groupId, List<long> blackListedUserIds, double wait = 1.5, string city = "");
        void BlackListGroupMembsersByGroupName(string searchPhrase, double wait = 1.5, string city = "");
        List<Group> GetGroupsBySearchPhrase(string searchPhrase, int count = 1000);
    }
}
