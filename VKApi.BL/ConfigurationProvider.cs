using System.Configuration;
using VKApi.BL.Interfaces;

namespace VKApi.BL
{
    public class ConfigurationProvider: IConfigurationProvider
    {
        public string GetConfig(string name)
        {
            var configvalue2 = ConfigurationManager.AppSettings[name];
            return configvalue2;
        }
    }
}
