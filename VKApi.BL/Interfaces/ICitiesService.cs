using System.Collections.Generic;
using VKApi.BL.Models;

namespace VKApi.BL.Interfaces
{
    public interface ICitiesService
    {
        List<CityExtended> GetCities(int[] cityIds, bool fakeApi = true);
    }
}
