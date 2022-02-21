namespace Inst.Api.Services
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Threading.Tasks;
    using InstagramApiSharp.API;
    using InstagramApiSharp.API.Builder;
    using InstagramApiSharp.Classes;
    using InstagramApiSharp.Logger;
    using Interfaces;
    
    
    public class InstApiFactory:IInstApiFactory
    {
        public async Task<IInstaApi> Login()
        {
            var userName = ConfigurationManager.AppSettings["InstUserName"];
            var pass = ConfigurationManager.AppSettings["InstPassword"];

            var userSession = new UserSessionData
            {
                UserName = userName,
                Password = pass
            };

            var instaApi = InstaApiBuilder.CreateBuilder()
                .SetUser(userSession)
                .UseLogger(new DebugLogger(LogLevel.Exceptions))
                .Build();
            const string stateFile = "state.bin";
            try
            {
                if (File.Exists(stateFile))
                {
                    Console.WriteLine("Loading state from file");
                    using (var fs = File.OpenRead(stateFile))
                    {
                        await instaApi.LoadStateDataFromStreamAsync(fs);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (!instaApi.IsUserAuthenticated)
            {
                // login
                Console.WriteLine($"Logging in as {userSession.UserName}");
                var logInResult = await instaApi.LoginAsync();
                if (!logInResult.Succeeded)
                {
                    Console.WriteLine($"Unable to login: {logInResult.Info.Message}");
                    return instaApi;
                }
            }

            var state = await instaApi.GetStateDataAsStreamAsync();

            using (var fileStream = File.Create(stateFile))
            {
                state.Seek(0, SeekOrigin.Begin);
                await state.CopyToAsync(fileStream);
            }

            return instaApi;
        }
    }
}