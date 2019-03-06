﻿using System;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;

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
                Settings = Settings.Wall,
               // AccessToken = "6e3813426e3813426e381342086e51042366e386e3813423246640d0f9ba0d4bdf02a8f",
                
            });

            return _api;
        }

        public VkApi CreateVkApi(Account account)
        {
            if (_api != null)
            {
                return _api;
            }

            _api = new VkApi();

            _api.Authorize(new ApiAuthParams
            {
                ApplicationId =
                    Convert.ToUInt64(account.ApplicationId),
                Login = account.Login,
                Password = account.Password,
                Settings = Settings.All
            });

            return _api;
        }
    }
}
