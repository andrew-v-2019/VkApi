using System;

namespace VKApi.Console.Blacklister.Extensions
{
   public static class ExceptionExtensions
    {
        public static bool IsFloodControl(this Exception e)
        {
            return e.Contains("flood");
        }

        public static bool IsOwnerIdIncorrect(this Exception e)
        {
            return e.Contains("owner_id is incorrect");
        }

        private static bool Contains(this Exception e, string stringToCheck)
        {
            return e.Message.ToLower().Contains(stringToCheck);
        }
    }
}
