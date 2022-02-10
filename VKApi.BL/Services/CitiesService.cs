using System.Collections.Generic;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using VkNet.Model;

namespace VKApi.BL.Services
{
    public class CitiesService : ICitiesService
    {
        private readonly IVkApiFactory _apiFactory;

        public CitiesService(IVkApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        public List<CityExtended> GetCities(int[] cityIds, bool fakeApi = true)
        {
            using (var api = _apiFactory.CreateVkApi(fakeApi))
            {
                var cities = api.Database.GetCitiesById(cityIds);
                var citiesExtended = new List<CityExtended>();
                foreach (var city in cities)
                {
                    citiesExtended.Add(GetExtModel(city));
                }
                return citiesExtended;
            }
        }

        private CityExtended GetExtModel(City city)
        {
            var cityExt = new CityExtended()
            {
                Area = city.Area,
                Id = city.Id,
                Important = city.Important,
                Region = city.Region,
                Title = city.Title,
                Names = new List<string> { city.Title.ToLower() }
            };

            if (cityExt.Id == 73)
            {
                cityExt.Names.AddRange(new List<string> { "krasnoyarsk", "красноярск" });
            }

            if (cityExt.Id == 641)
            {
                cityExt.Names.AddRange(new List<string> { "divnogorsk", "дивногорск" });
            }

            return cityExt;
        }
    }
}
