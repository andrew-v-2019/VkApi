using VkApi.Models.Configurations;
using VKApi.ChicksLiker;

namespace VkApi.Models
{
    public class LikeClickerConfiguration: ConfigurationBase
    {
        public string GroupName { get; set; }


        public int PostsCountToAnalyze { get; set; }


        public string CitiesString { get; set; }


        public int ProfilePhotosToLike { get; set; }


        public int MinAge { get; set; }

        public int MaxAge { get; set; }

        public Strategy Strategy { get; set; }
    }
}
