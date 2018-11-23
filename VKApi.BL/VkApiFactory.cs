using System;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VKApi.BL.Interfaces;

namespace VKApi.BL
{
    public class VkApiFactory: IVkApiFactory
    {
        private readonly IConfigurationProvider _configurationProvider;

        public VkApiFactory(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        private VkApi _api;

        public VkApi CreateVkApi()
        {
            if (_api != null) 
            {
                return _api;
            }

            _api = new VkApi();

            _api.Authorize(new ApiAuthParams
            {
                ApplicationId =
                    Convert.ToUInt64(_configurationProvider.GetConfig(nameof(ApiAuthParams.ApplicationId))),
                Login = _configurationProvider.GetConfig(nameof(ApiAuthParams.Login)),
                Password = _configurationProvider.GetConfig(nameof(ApiAuthParams.Password)),
                Settings = Settings.All
            });

            return _api;
        }
    }
}
