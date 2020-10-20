using System;
using System.Linq;
using VKApi.BL.Extensions;
using VkNet.Model;

namespace VKApi.BL.Models
{
    public class UserExtended : User
    {
        public UserExtended(User u)
        {
            var properties = u.GetType().GetProperties();

            properties.ToList()
                .ForEach(property =>
                {
                    var prop = u.GetType().GetProperty(property.Name);
                    if (prop == null)
                    {
                        return;
                    }
                    var value = prop.GetValue(u, null);

                    var thisProp = this.GetType().GetProperty(property.Name);

                    if (thisProp == null)
                    {
                        return;
                    }
                    thisProp.SetValue(this, value, null);
                });
            Age = this.GetAge();
            LastActivityDate = this.GetLastActivityDate();
            HasChildrens = this.HasChildrens();
        }

        public int? Age { get; set; }
        public DateTime LastActivityDate { get; set; }
        public bool HasChildrens { get; set; }

    
    }
}
