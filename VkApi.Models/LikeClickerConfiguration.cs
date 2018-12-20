using System.ComponentModel;
using VKApi.ChicksLiker;

namespace VkApi.Models
{
    public class LikeClickerConfiguration
    {
        public string GroupName { get; set; }


        public int PostsCountToAnalyze { get; set; }


        public string CitiesString { get; set; }


        public int ProfilePhotosToLike { get; set; }


        public int MinAge { get; set; }

        public int MaxAge { get; set; }

        public Strategy Strategy  { get; set; }
}
}
