using System.Collections.Generic;
using System.Linq;
using VKApi.BL.Extensions;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;

namespace VKApi.BL.Services
{
    public class MessagesService : IMessagesService
    {

        private readonly IVkApiFactory _apiFactory;


        public MessagesService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        public List<UserExtended> GeChatUsers(List<long> chatIds, bool fakeApi = true)
        {
            if (chatIds == null || !chatIds.Any())
                return new List<UserExtended>();

            using (var api = _apiFactory.CreateVkApi(fakeApi))
            {
                var chatUsers = api.Messages.GetChatUsers(chatIds, UsersFields.All, NameCase.Abl);
                return chatUsers.Select(u => u.ToExtendedModel()).ToList();
            }
        }
    }
}
