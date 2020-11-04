using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Extentions
{
    public static class Extentions
    {
        public static string TrimEndChar(this string s, string character)
        {
            if (s.EndsWith('/'))
            {
                int lastSlash = s.LastIndexOf(character);
                return (lastSlash > -1) ? s.Substring(0, lastSlash) : s;
            }
            return s;
        }

    }
}
