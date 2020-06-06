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

        public List<Comment> GetComments(long ownerId, long postId, VkApi api)
        {
            var step = 100;

            var comments = new List<Comment>();
            long offset = 0;
            long totalCount = 0;
            do
            {
                try
                {
                    var param = new WallGetCommentsParams()
                    {
                        OwnerId = ownerId,
                        PostId = postId,
                        Count = step,
                        Offset = offset,
                    };
                    var getResult = api.Wall.GetComments(param);
                    var commentsChunk = getResult.Items.Select(p => p).ToList();
                    comments.AddRange(commentsChunk);
                    offset = offset + step;
                    param.Offset = offset;
                    totalCount = getResult.Count;
                    Console.Clear();
                    Console.WriteLine($"Total posts count {comments.Count}.");
                }
                catch
                {
                    offset = offset + 1;
                }
            } while (offset < totalCount);

            var orderredComments = comments.OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.Likes.Count)
                .ToList();
            return orderredComments;
        }
    }
}
