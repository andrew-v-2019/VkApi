using System.Collections.Generic;
using VKApi.BL.Models;

namespace VKApi.BL.Interfaces
{
    public interface IMessagesService
    {
        
        List<UserExtended> GeChatUsers(List<long> chatIds, bool fakeApi = true);
    }
}
