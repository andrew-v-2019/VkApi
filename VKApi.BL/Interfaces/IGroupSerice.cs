using System.Collections.Generic;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace VKApi.BL.Interfaces
{
    public interface IGroupSerice
    {
        List<User> GetGroupMembers(string groupName, UsersFields fields = null, int? count = null);
        List<Post> GetPosts(string groupName, ulong? count = null);
        void BlackListGroupMembsers(string groupId, List<long> blackListedUserIds, double wait = 1.5, string city = "", bool olderFirst = false);
        void BlackListGroupMembsersByGroupName(string searchPhrase, double wait = 1.5, string city = "", bool olderFirst = false);
        List<Group> GetGroupsBySearchPhrase(string searchPhrase, int count = 1000);
    }
}
