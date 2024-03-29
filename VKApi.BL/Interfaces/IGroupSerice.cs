﻿using System.Collections.Generic;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VKApi.BL.Models;
using VkNet.Model.Attachments;
using System;

namespace VKApi.BL.Interfaces
{
    public interface IGroupSerice
    {
        List<UserExtended> GetGroupMembers(string groupName, UsersFields fields = null, int? count = null);
        List<Post> GetPosts(string groupName, ulong? count = null);
        List<Group> GetGroupsBySearchPhrase(string searchPhrase, int count = 1000);
        Group GetByName(string groupName);
        List<Post> GetPostsByGroupId(long groupId, VkApi api, ulong? count = null);

        List<Post> GetPostsByGroupId(long groupId, VkApi api, DateTime minDate);

        List<UserExtended> GetGroupPostsLickers(long groupId, List<long> blackListedUserIds, DateTime minPostDate);
    }
}
