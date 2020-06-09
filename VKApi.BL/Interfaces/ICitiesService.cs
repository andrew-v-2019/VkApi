using System.Collections.Generic;
using VkNet.Model;

namespace VKApi.BL.Interfaces
{
    public interface ICitiesService
    {
        List<City> GetCities(int[] cityIds, bool fakeApi = true);
    }
}
