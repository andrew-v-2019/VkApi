using System.Collections.Generic;
using System.Linq;
using VKApi.BL.Interfaces;
using VkNet.Model;

namespace VKApi.BL.Services
{
    public class CitiesService:ICitiesService
    {
        private readonly IVkApiFactory _apiFactory;

        public CitiesService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        public List<City> GetCities(int[] cityIds, bool fakeApi = true)
        {
            using (var api = _apiFactory.CreateVkApi(fakeApi))
            {
                var cities = api.Database.GetCitiesById(cityIds);
                return cities.ToList();
            }
        }
    }
}
