using System;
using System.IO;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;

namespace Inst.Api
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            var instApi = await Login();
            var blockedUsers = await instApi.UserProcessor.GetBlockedUsersAsync(PaginationParameters.MaxPagesToLoad(1));
        }

        private static async Task<IInstaApi> Login()
        {
            var userSession = new UserSessionData
            {
                UserName = "index.out.of.range",
                Password = "Kludgekludge1"
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