﻿using System;
using System.Collections.Generic;
using System.Linq;
using VKApi.BL.Models;
using VkNet.Enums;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;

namespace VKApi.BL.Extensions
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

        public static bool FromCity(this UserExtended user, List<CityExtended> cities)
        {
            if (user?.City?.Id != null)
            {
                var cityIds = cities.Select(x => x.Id).ToList();
                var res = cityIds.Contains(user.City.Id.Value);

                return res;
            }

            if (!string.IsNullOrWhiteSpace(user?.HomeTown))
            {
                var cityNames = cities.SelectMany(x => x.Names.Select(x1 => x1.Trim().ToLower())).ToList();
                var res = cityNames.Contains(user.HomeTown.Trim().ToLower());

                return res;
            }

            return false;
        }

        public static bool FromCity(this UserExtended user, long[] cities)
        {
            if (user.City == null)
            {
                return false;
            }

            if (!user.City.Id.HasValue)
            {
                return false;
            }

            return cities.Any(c => c == user.City.Id.Value);
        }

        public static bool BlackListed(this User user)
        {
            return user.Blacklisted || user.BlacklistedByMe;
        }

        public static string GetDomainForUser(this User user)
        {
            var r = $"vk.com/{user.Domain}";

            if (string.IsNullOrWhiteSpace(user.Domain))
            {
                r = $"vk.com/id{user.Id}";
            }

            return r;
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
            return user.Relatives != null
                && user.Relatives.Any(x => x.Type == RelativeType.Child);
        }


    }
}
