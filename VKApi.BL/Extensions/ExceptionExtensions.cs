using System;

namespace VKApi.BL.Extensions
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

        public static bool DoesGroupHideMembers(this Exception e)
        {
            return e.Contains("group hide members");
        }

        private static bool Contains(this Exception e, string stringToCheck)
        {
            return e.Message.ToLower().Contains(stringToCheck);
        }
    }
}
