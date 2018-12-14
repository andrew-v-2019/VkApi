
using VkNet;
using VKApi.BL.Models;

namespace VKApi.BL.Interfaces
{
    public interface IVkApiFactory
    {
        VkApi CreateVkApi();
        VkApi CreateVkApi(Account account);
    }
}
