using Blish_HUD;
using Blish_HUD.Content;
using Gw2Sharp.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

// all fishing: 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402, 6224, 6110, 6471, 6393
// https://api.guildwars2.com/v2/achievements?ids=6068,6106,6109,6110,6111,6153,6179,6192,6201,6224,6227,6250,6258,6263,6264,6279,6284,6317,6330,6336,6339,6342,6344,6363,6393,6402,6439,6466,6471,6475,6478,6484,6489,6505,6506,6509

// fish data based on:
// https://github.com/patrick-petersen/gw2-fishing/blob/master/src/api/FishData.tsx

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class Fish
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
            DawnDusk = Dawn | Dusk,
            DuskDawn = Dusk | Dawn,
            Any = Dawn | Day | Dusk | Night,
        }

        // Fish Item Name
        public string name { get; set; }
        // Item Id
        public int itemId { get; set; }
        // Junk, Basic, Fine, Rare, Masterwork, Exotic, Ascended, Legendary
        public string rarity { get; set; }
        // Fishing holes: Any, None, Boreal Fish, Cavern Fish, Channel Fish, Coastal Fish, Deep Fishing Hole, Desert Fish, Freshwater Fish, Grotto Fish, Lake Fish, Noxious Water Fish,
        // Offshore Fish, Polluted Lake Fish, Quarry Fish, Rare Fish, River Fish, Saltwater Fish, Special Fishing Hole, Shore Fish, Volcanic Fish, Wreckage Site
        public string fishingHole { get; set; }
        // https://wiki.guildwars2.com/wiki/Bait
        // Any, Fish Egg, Freshwater Minnow, Glow Worm, Lava Beetle, Leech, Lightning Bug, Mackerel, Nightcrawler, Ramshorn Snail, Sardine, Scorpion, Shrimpling, Sparkfly Nymph, Haiju Minnows
        public string bait { get; set; }
        // Time of day fish can be caught
        public TimeOfDay timeOfDay { get; set; }
        // Can this fish be caught in open water?
        public bool openWater { get; set; }
        // Map region location ie Seitung Province.. should this be map id? list? Or even more specific, just the map this fish is found in? Saltwater, Anywhere/Any?
        public string location { get; set; }
        // Achievement Name
        public string achievement { get; set; }
        // Id of related fishing achievement
        public int achievementId { get; set; }
        // Ex: Used for _ achievement or part of _ collection
        public string notes { get; set; }
        // Url item icon
        public RenderUrl icon { get; set; }
        public bool Visible { get; set; }
        public AsyncTexture2D iconImg { get; set; }
        //TODO can save item code to clipboard on click
    }
}