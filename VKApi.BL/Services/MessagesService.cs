using System.Collections.Generic;
using System.Linq;
using VKApi.BL.Extensions;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VkNet.Model;
using VkNet.Model.RequestParams;

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
                //var allChats = api.Messages.GetConversations(new GetConversationsParams());
                //var chatUsers = api.Messages.GetChatUsers(chatIds, UsersFields.All, NameCase.Abl);

                var chatUsers = new List<long>();
                foreach (var chatId in chatIds)
                {
                    var getMembersRes = api.Messages.GetConversationMembers(chatId, new List<string> {"id"});
                    chatUsers.AddRange(getMembersRes.Items.Select(x=>x.MemberId));
                }
               
                return chatUsers.Select(u => new UserExtended(new User {Id = u})).ToList();
            }
        }
    }
}
