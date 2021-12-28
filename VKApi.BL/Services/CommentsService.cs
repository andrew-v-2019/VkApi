using System;
using System.Collections.Generic;
using System.Linq;
using VKApi.BL.Interfaces;
using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VKApi.BL.Services
{
    public class CommentsService : ICommentsService
    {

        public List<Comment> GetComments(long ownerId, long postId, VkApi api, ref List<User> profiles)
        {
            //https://vknet.github.io/vk/wall/getComments/
            const int step = 100;


            var comments = new List<Comment>();
            long offset = 0;
            long totalCount = 0;
            do
            {
                try
                {
                    var param = new WallGetCommentsParams
                    {
                        OwnerId = ownerId,
                        PostId = postId,
                        Count = step,
                        Offset = offset,
                        NeedLikes = true,
                        Extended = true,

                        
                    };
                    var getResult = api.Wall.GetComments(param);
                    profiles = getResult.Profiles.ToList();
                    var commentsChunk = getResult.Items.Select(p => p).ToList();
                    comments.AddRange(commentsChunk);
                    offset += step;
                    param.Offset = offset;
                    totalCount = getResult.Count;
                }
                catch(Exception)
                {
                    offset += 1;
                }
            } while (offset < totalCount);

            var orderedComments = comments.OrderByDescending(p => p.Date)
                .ThenByDescending(p =>p.Likes?.Count)
                .ToList();
            return orderedComments;
        }
    }
}
