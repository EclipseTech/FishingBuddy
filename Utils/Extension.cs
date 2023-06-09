// all fishing: 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402, 6224, 6110, 6471, 6393
// https://api.guildwars2.com/v2/achievements?ids=6068,6106,6109,6110,6111,6153,6179,6192,6201,6224,6227,6250,6258,6263,6264,6279,6284,6317,6330,6336,6339,6342,6344,6363,6393,6402,6439,6466,6471,6475,6478,6484,6489,6505,6506,6509

// fish data based on:
// https://github.com/patrick-petersen/gw2-fishing/blob/master/src/api/FishData.tsx

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    public static class Extension
    {
        public static string GetEnumMemberValue(this Enum value)
        {
            string enumToString = value.GetType().GetMember(value.ToString()).FirstOrDefault()?
                        .GetCustomAttribute<EnumMemberAttribute>(false)?.Value ?? value.ToString();
            return Properties.Strings.ResourceManager.GetString(enumToString, Properties.Strings.Culture);
        }
    }
}
