using System.Configuration;
using System.Linq;
using VKApi.BL.Interfaces;

namespace VKApi.BL
{
    public class ConfigurationProvider: IConfigurationProvider
    {
        public string GetConfig(string name)
        {
            if (!ConfigurationManager.AppSettings.AllKeys.Contains(name)) return string.Empty;
            var configvalue = ConfigurationManager.AppSettings[name];
            return configvalue;
        }
    }
}
