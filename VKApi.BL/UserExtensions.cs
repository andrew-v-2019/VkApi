using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet.Enums;
using VkNet.Model;

namespace VKApi.BL
{
    public static class UserExtensions
    {
        public static bool IsAgeBetween(this User user, int min, int max)
        {
            if (user.BirthdayVisibility == BirthdayVisibility.Invisible ||
                user.BirthdayVisibility == BirthdayVisibility.OnlyDayAndMonth)
            {
                return true;
            }
            var birthDateString = user.BirthDate.Split('.');
            var d = Convert.ToInt32(birthDateString[0]);
            var m = Convert.ToInt32(birthDateString[1]);
            var y = Convert.ToInt32(birthDateString[2]);
            try
            {
                var birthDate = new DateTime(y, m, d);
                var birthDateYear = birthDate.Year;
                var now = DateTime.Now.Year;
                var years = now - birthDateYear;
                return years >= min && years <= max;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasBeenOfflineMoreThanDays(this User user, int days)
        {
            if (user.LastSeen?.Time == null)
            {
                return false;
            }
            var lastVisitDate = user.LastSeen.Time.Value;

            var daysWithoutVist = Math.Abs((DateTime.Now - lastVisitDate).Days);

            return daysWithoutVist > days;
        }

        public static bool FromCity(this User user, string cityName)
        {
            var cityLow = cityName.Trim().ToLower();
            return user.City != null && user.City.Title.ToLower().Contains(cityLow);
        }

        public static bool BlackListed(this User user)
        {
            return user.Blacklisted || user.BlacklistedByMe;
        }

        public static IOrderedEnumerable<User> OrderByLsatActivityDateDesc(this List<User> users)
        {
            var usersOrdered = users.OrderByDescending(u => u.Online)
                .ThenByDescending(u => u.LastSeen != null ? u.LastSeen.Time : new DateTime(1970, 1, 1));
            return usersOrdered;
        }

        public static long GetPhotoId(this User user)
        {
            if (!user.PhotoId.Contains("_"))
            {
                return Convert.ToUInt32(user.PhotoId);
            }
            var spl = user.PhotoId.Split('_');
            var ulon = Convert.ToUInt32(spl[1]);
            return ulon;
        }

        public static bool IsSingle(this User user)
        {
            return user.Relation != RelationType.Amorous && user.Relation != RelationType.CivilMarriage &&
                   user.Relation != RelationType.HasFriend && user.Relation != RelationType.Married;
        }
    }
}
