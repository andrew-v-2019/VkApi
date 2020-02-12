
using VkNet;

namespace VKApi.BL.Interfaces
{
    public interface IVkApiFactory
    {
        VkApi CreateVkApi(bool fake = false);
    }
}
