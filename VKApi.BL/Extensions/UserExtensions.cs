using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Interception.Utilities;
using VkNet.Enums;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VKApi.BL.Models;

namespace VKApi.BL
{
    public static class UserExtensions
    {
        public static bool IsAgeBetween(this UserExtended user, int min, int max)
        {
            if (!AgeVisible(user))
            {
                return true;
            }

            try
            {
                var years = GetAge(user);
                if (years.HasValue)
                {
                    return years >= min && years <= max;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int? GetAge(this UserExtended user)
        {
            if (!AgeVisible(user))
            {
                return null;
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
                return years;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool AgeVisible(this UserExtended user)
        {
            if (user.BirthdayVisibility == BirthdayVisibility.Invisible ||
                user.BirthdayVisibility == BirthdayVisibility.OnlyDayAndMonth)
            {
                return false;
            }
            return !string.IsNullOrWhiteSpace(user.BirthDate);
        }

        public static bool HasBeenOfflineMoreThanDays(this UserExtended user, int days)
        {
            var lastVisitDate = user.LastActivityDate;
            var daysWithoutVist = Math.Abs((DateTime.Now - lastVisitDate).Days);
            return daysWithoutVist > days;
        }

        public static bool FromCity(this UserExtended user, string cityName)
        {
            var cityLow = cityName.Trim().ToLower();
            return user.City != null && user.City.Title.ToLower().Contains(cityLow);
        }

        public static bool FromCity(this UserExtended user, string[] cities)
        {
            if (user.City == null)
            {
                return false;
            }
            var citiesLowered = cities.Select(c => c.Trim().ToLower()).ToList();
            return citiesLowered.Contains(user.City.Title.Trim().ToLower());
        }

        public static bool BlackListed(this User user)
        {
            return user.Blacklisted || user.BlacklistedByMe;
        }

        public static UserExtended ToExtendedModel(this User u)
        {
            return new UserExtended(u);
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
            //return user.Relation != RelationType.Amorous
            //    && user.Relation != RelationType.CivilMarriage
            //    && user.Relation != RelationType.HasFriend
            //    && user.Relation != RelationType.Married
            //    && user.Relation != RelationType.Engaged;

            return user.Relation == RelationType.NotMarried 
                 || user.Relation == RelationType.InActiveSearch 
                 || user.Relation == RelationType.ItsComplex 
                 || user.Relation == RelationType.Unknown;
        }


        public static DateTime GetLastActivityDate(this User user)
        {
            DateTime lastActivityDate;
            if (user.Online == true)
            {
                lastActivityDate = DateTime.Now;
            }
            else
            {
                lastActivityDate = user.LastSeen?.Time != null
                    ? user.LastSeen.Time.GetValueOrDefault()
                    : new DateTime(1970, 1, 1);
            }
            return lastActivityDate;
        }

        public static bool HasChildrens(this User user)
        {
            return user.Relatives != null && user.Relatives.Any(x => x.Type == RelativeType.Child);
        }


    }
}
