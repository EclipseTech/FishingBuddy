using Blish_HUD;
using Blish_HUD.Content;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.V2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

// all fishing: 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402, 6224, 6110, 6471, 6393
// https://api.guildwars2.com/v2/achievements?ids=6068,6106,6109,6110,6111,6153,6179,6192,6201,6224,6227,6250,6258,6263,6264,6279,6284,6317,6330,6336,6339,6342,6344,6363,6393,6402,6439,6466,6471,6475,6478,6484,6489,6505,6506,6509

// fish data based on:
// https://github.com/patrick-petersen/gw2-fishing/blob/master/src/api/FishData.tsx

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    public class Fish
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(Fish));

        [Flags]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TimeOfDay
        {
            None = 0,
            Dawn = 1,
            Day = 2,
            Dusk = 4,
            Night = 8,
            // Fishing will treat dawn and dusk as both day and night at the same time https://wiki.guildwars2.com/wiki/Day_and_night
            [EnumMember(Value = "Dawn/Dusk")]
            DawnDusk = Dawn | Dusk,
            [EnumMember(Value = "Dusk/Dawn")]
            DuskDawn = Dusk | Dawn,
            Any = Dawn | Day | Dusk | Night,
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum FishBait
        {
            Any,
            [EnumMember(Value = "Fish Eggs")]
            FishEggs,
            [EnumMember(Value = "Glow Worms")]
            GlowWorms,
            [EnumMember(Value = "Haiju Minnows")]
            HaijuMinnows,
            [EnumMember(Value = "Lava Beetles")]
            LavaBeetles,
            Leeches,
            [EnumMember(Value = "Lightning Bugs")]
            LightningBugs,
            Mackerel,
            Minnows,
            Nightcrawlers,
            [EnumMember(Value = "Ramshorn Snails")]
            RamshornSnails,
            Sardines,
            Scorpions,
            Shrimplings,
            [EnumMember(Value = "Sparkfly Larvae")]
            SparkflyLarvae,
        }

        // Fish Item Name
        public string Name { get; set; }
        // Item Id
        public int ItemId { get; set; }
        // TODO change this to enum?
        // Junk, Basic, Fine, Rare, Masterwork, Exotic, Ascended, Legendary
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemRarity Rarity { get; set; }
        // Fishing holes: Any, None, Boreal Fish, Cavern Fish, Channel Fish, Coastal Fish, Deep Fishing Hole, Desert Fish, Freshwater Fish, Grotto Fish, Lake Fish, Noxious Water Fish,
        // Offshore Fish, Polluted Lake Fish, Quarry Fish, Rare Fish, River Fish, Saltwater Fish, Special Fishing Hole, Shore Fish, Volcanic Fish, Wreckage Site
        public string FishingHole { get; set; }
        // https://wiki.guildwars2.com/wiki/Bait
        // Any, Fish Egg, Freshwater Minnow, Glow Worm, Lava Beetle, Leech, Lightning Bug, Mackerel, Nightcrawler, Ramshorn Snail, Sardine, Scorpion, Shrimpling, Sparkfly Nymph, Haiju Minnows
        public FishBait Bait { get; set; }
        // Time of day fish can be caught
        public TimeOfDay Time { get; set; }
        // Can this fish be caught in open water?
        public bool OpenWater { get; set; }
        // Map region location ie Seitung Province.. should this be map id? list? Or even more specific, just the map this fish is found in? Saltwater, Anywhere/Any?
        public string Location { get; set; }
        // Special case map locations
        public List<int> Locations { get; set; }
        // Achievement Name
        public string Achievement { get; set; }
        // Id of related fishing achievement
        public int AchievementId { get; set; }
        // All related achievements
        public List<int> AchievementIds { get; set; }
        // Ex: Used for _ achievement or part of _ collection
        public string Notes { get; set; }
        // Url item icon
        public RenderUrl Icon { get; set; }
        public bool Visible { get; set; } = true;
        public bool Caught { get; set; } = false;
        public AsyncTexture2D IconImg { get; set; }
        public string ChatLink { get; set; }
        //TODO can save item code to clipboard on modifier+click
    }

    public static class Extension
    {
        public static string GetEnumMemberValue(this Enum value)
        {
            string ret = value.GetType().GetMember(value.ToString()).FirstOrDefault()?
                        .GetCustomAttribute<EnumMemberAttribute>(false)?.Value;
            return ret != null ? ret : value.ToString();
        }
    }
}
