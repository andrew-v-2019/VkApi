using System.Collections.Generic;
using System.ComponentModel;
using VKApi.ChicksLiker;

namespace VkApi.WpfApp
{
    public class ConfigurationViewModel 
    {
        public string ApplicationId { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        public string GroupName { get; set; }


        public int PostsCountToAnalyze { get; set; }



        public string CitiesString { get; set; }


        public int ProfilePhotosToLike { get; set; }


        public int MinAge { get; set; }

        public int MaxAge { get; set; }

        public string Strategy { get; set; }

        public List<string> Strategies { get; set; }

    }
}
