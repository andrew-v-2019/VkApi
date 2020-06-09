using System;
using VkNet.Model.Attachments;

namespace VKApi.BL.Extensions
{
    public static class PostExtensions
    {
        public static string GetPostUrl(this Post post)
        {
            if (!post.OwnerId.HasValue)
                return string.Empty;

            var ownerId = Math.Abs(post.OwnerId.Value);
            var res = $"https://vk.com/club{ownerId}?w=wall-{ownerId}_{post.Id}";
            return res;
        }
    }
}
