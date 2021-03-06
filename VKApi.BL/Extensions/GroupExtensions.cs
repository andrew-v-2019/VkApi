﻿using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Enums;
using VkNet.Model;

namespace VKApi.BL
{
    public static class GroupExtensions
    {

        public static IEnumerable<T> SuperSelect<T>(this IEnumerable<T> items, Func<T, T> func)
        {
            var enumerator = items.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var result = func(enumerator.Current);
                yield return result;
            }
        }

        public static List<Group> GetClosedGroups(this List<Group> groups)
        {
            var result = groups.Where(g => g.IsClosed.HasValue &&
                                           (g.IsClosed.Value == GroupPublicity.Closed ||
                                            g.IsClosed.Value == GroupPublicity.Private))
                .ToList();
            return result;

        }

        public static List<Group> GetOpenGroups(this List<Group> groups)
        {
            var result = groups.Where(g => g.IsClosed.HasValue &&
                                           g.IsClosed.Value == GroupPublicity.Public)
                .ToList();
            return result;
        }
    }
}
