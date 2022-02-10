using System.Collections.Generic;
using VkNet.Model;

namespace VKApi.BL.Models
{
    public class CityExtended: City
    {
        public List<string> Names { get; set; } = new List<string>();
    }
}
