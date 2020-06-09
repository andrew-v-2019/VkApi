using System;
using System.Collections.Generic;
using System.Linq;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.RequestParams;
using VKApi.BL.Interfaces;

namespace VKApi.BL.Services
{
    public class LikesService : ILikesService
    {
        private readonly IVkApiFactory _apiFactory;

        public LikesService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }


        public List<long> GetUsersWhoLiked(long ownerId, long itemId, LikeObjectType type)
        {
            var likerIds = new List<long>();
            using (var api = _apiFactory.CreateVkApi())
            {
                var chunk = GetUsersWhoLiked(ownerId, itemId, type, api);
                likerIds.AddRange(chunk);
                
            }
            return likerIds.Distinct().ToList();
        }



        public List<long> GetUsersWhoLiked(long ownerId, long itemId, LikeObjectType type, VkApi api)
        {
            uint? step = 1000;
            uint? offset = 0;
            ulong totalCount;
            var result = new List<long>();
            do
            {
                var param = new LikesGetListParams
                {
                    OwnerId = ownerId,
                    Count = step,
                    Type = type,
                    ItemId = itemId,
                    Extended = true,
                    Offset = offset
                };
                var likerChunk = api.Likes.GetList(param);

                offset += step;
                totalCount = likerChunk.TotalCount;
                result.AddRange(likerChunk.ToList());
            } while (offset < totalCount);

            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(.10));
            return result;
        }

        public bool AddLike(long ownerId, long itemId, LikeObjectType type, VkApi api)
        {
           
            var param = new LikesAddParams()
            {
                OwnerId = ownerId,
                ItemId = itemId,
                Type = type,
            };
            api.Likes.Add(param);
            return true;
        }

        public void AddLike(long ownerId, List<long> itemIds, LikeObjectType type)
        {
            using (var api = _apiFactory.CreateVkApi())
            {
                foreach (var itemId in itemIds)
                {
                    AddLike(ownerId, itemId, type, api);
                }
            }
        }

    }
}
