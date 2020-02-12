using System;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VKApi.BL.Interfaces;
using VKApi.BL.Models;
using System.Linq;
using System.Collections.Generic;

namespace VKApi.BL
{
    public class VkApiFactory : IVkApiFactory
    {
        private readonly IConfigurationProvider _configurationProvider;

        public VkApiFactory(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        private VkApi _api;
        private VkApi _fakeApi;

        public VkApi CreateVkApi(bool fake = false)
        {
            if (_api != null && !fake)
            {
                return _api;
            }

            if (_fakeApi != null && fake)
            {
                return _fakeApi;
            }

            var api = new VkApi();
            var account = CreateAccount(fake);

            if (!fake)
            {
                _api = api;
            }
            else
            {
                _fakeApi = api;
            }
            api.Authorize(GetAuthParams(account));
            return api;
        }

        private ApiAuthParams GetAuthParams(Account acc)
        {
            var p = new ApiAuthParams()
            {
                ApplicationId =
                    Convert.ToUInt64(acc.ApplicationId),
                Login = acc.Login,
                Password = acc.Password,
                Settings = Settings.Wall,
                
                
            };
            return p;
        }

        private Account CreateAccount(bool fake = false)
        {
            var settingToRead = "Account";
            if (fake)
            {
                settingToRead = "FakeAccount";
            }
            var dictionary = GetCredentials(settingToRead);
            var acc = CreateAccountFormDictionary(dictionary);
            return acc;
        }

        private Account CreateAccountFormDictionary(Dictionary<string, string> dictionary)
        {
            var acc = new Account()
            {
                ApplicationId = dictionary[nameof(Account.ApplicationId)],
                Login = dictionary[nameof(Account.Login)],
                Password = dictionary[nameof(Account.Password)]
            };

            return acc;
        }

        private Dictionary<string, string> GetCredentials(string name)
        {
            return _configurationProvider.GetConfig(name).Split(',').ToDictionary(k => k.Split(':').First(), v => v.Split(':').Last());
        }
    }
}
