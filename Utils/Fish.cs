// all fishing: 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402, 6224, 6110, 6471, 6393
// https://api.guildwars2.com/v2/achievements?ids=6068,6106,6109,6110,6111,6153,6179,6192,6201,6224,6227,6250,6258,6263,6264,6279,6284,6317,6330,6336,6339,6342,6344,6363,6393,6402,6439,6466,6471,6475,6478,6484,6489,6505,6506,6509

// fish data based on:
// https://github.com/patrick-petersen/gw2-fishing/blob/master/src/api/FishData.tsx

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Gw2Sharp.WebApi;
    using Gw2Sharp.WebApi.V2.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public class Fish
    {
        internal static readonly Logger Logger = Logger.GetLogger(typeof(Fish));

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

        // Fishing holes: https://wiki.guildwars2.com/wiki/Fishing#Fishing_holes
        [JsonConverter(typeof(StringEnumConverter))]
        public enum FishingHole
        {
            None,
            Any,
            [EnumMember(Value = "Boreal Fish")]
            BorealFish,
            [EnumMember(Value = "Cavern Fish")]
            CavernFish,
            [EnumMember(Value = "Channel Fish")]
            ChannelFish,
            [EnumMember(Value = "Coastal Fish")]
            CoastalFish,
            [EnumMember(Value = "Deep Fishing Hole")]
            DeepFishingHole,
            [EnumMember(Value = "Desert Fish")]
            DesertFish,
            [EnumMember(Value = "Freshwater Fish")]
            FreshwaterFish,
            [EnumMember(Value = "Grotto Fish")]
            GrottoFish,
            [EnumMember(Value = "Lake Fish")]
            LakeFish,
            [EnumMember(Value = "Lutgardis Trout")]
            LutgardisTrout,
            [EnumMember(Value = "Mysterious Waters Fish")]
            MysteriousWatersFish,
            [EnumMember(Value = "Noxious Water Fish")]
            NoxiousWaterFish,
            [EnumMember(Value = "Offshore Fish")]
            OffshoreFish,
            [EnumMember(Value = "Polluted Lake Fish")]
            PollutedLakeFish,
            [EnumMember(Value = "Quarry Fish")]
            QuarryFish,
            [EnumMember(Value = "Rare Fish")]
            RareFish,
            [EnumMember(Value = "River Fish")]
            RiverFish,
            [EnumMember(Value = "Saltwater Fish")]
            SaltwaterFish,
            [EnumMember(Value = "Special Fishing Hole")]
            SpecialFishingHole,
            [EnumMember(Value = "Shore Fish")]
            ShoreFish,
            [EnumMember(Value = "Volcanic Fish")]
            VolcanicFish,
            [EnumMember(Value = "Wreckage Site")]
            WreckageSite
        }

        // Fish Item Name
        public string Name { get; set; }
        // Item Id
        public int ItemId { get; set; }
        // Junk, Basic, Fine, Rare, Masterwork, Exotic, Ascended, Legendary
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemRarity Rarity { get; set; }
        // Fishing Hole
        [JsonProperty("FishingHole")]
        public FishingHole Hole { get; set; }
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
        //TODO save item code to clipboard on modifier+click
        public string ChatLink { get; set; }

        public static string BuildFishTooltip(Fish fish) {
            string name = $"{Properties.Strings.FishName}: {fish.Name}";
            string bait = $"{Properties.Strings.FishFavoredBait}: {fish.Bait.GetEnumMemberValue()}";
            string time = $"{Properties.Strings.FishTimeOfDay}: {fish.Time.GetEnumMemberValue()}";
            string hole = $"{Properties.Strings.FishFishingHole}: {fish.Hole.GetEnumMemberValue()}{(fish.OpenWater ? $", {Properties.Strings.OpenWater}" : string.Empty)}";
            string achieve = $"{Properties.Strings.Achievement}: {fish.Achievement}";
            string rarity = $"{Properties.Strings.Rarity}: {Properties.Strings.ResourceManager.GetString(fish.Rarity.ToString(), Properties.Strings.Culture)}";
            string hiddenReason = string.Empty;
            if (FishingBuddyModule._useAPIToken) {
                if (!fish.Visible && fish.Caught) hiddenReason = $"{Properties.Strings.Hidden}: {Properties.Strings.TimeOfDay}, {Properties.Strings.HiddenCaught}";
                else if (!fish.Visible) hiddenReason = $"{Properties.Strings.Hidden}: {Properties.Strings.TimeOfDay}";
                else if (fish.Caught) hiddenReason = $"{Properties.Strings.Hidden}: {Properties.Strings.HiddenCaught}";
            }
            string notes = !string.IsNullOrWhiteSpace(fish.Notes) ? $"{Properties.Strings.Notes}: {Properties.Strings.ResourceManager.GetString(fish.Notes, Properties.Strings.Culture)}" : string.Empty;
            string tooltip = FishingBuddyModule._fishPanelTooltipDisplay.Value;
            // Standard replacements
            tooltip = tooltip.Replace("@1", name);
            tooltip = tooltip.Replace("@2", bait);
            tooltip = tooltip.Replace("@3", time);
            tooltip = tooltip.Replace("@4", hole);
            tooltip = tooltip.Replace("@5", achieve);
            tooltip = tooltip.Replace("@6", rarity);
            tooltip = tooltip.Replace("@7", hiddenReason);
            tooltip = tooltip.Replace("@8", notes);
            // Create your own tooltip (not documented)
            tooltip = tooltip.Replace("#1", fish.Name);
            tooltip = tooltip.Replace("#2", fish.Bait.GetEnumMemberValue());
            tooltip = tooltip.Replace("#3", fish.Time.GetEnumMemberValue());
            tooltip = tooltip.Replace("#4", $"{fish.Hole.GetEnumMemberValue()}{(fish.OpenWater ? $", {Properties.Strings.OpenWater}" : string.Empty)}");
            tooltip = tooltip.Replace("#5", fish.Achievement);
            tooltip = tooltip.Replace("#6", Properties.Strings.ResourceManager.GetString(fish.Rarity.ToString(), Properties.Strings.Culture));
            tooltip = tooltip.Replace("#8", Properties.Strings.ResourceManager.GetString(fish.Notes, Properties.Strings.Culture));
            // Newline string replacement
            tooltip = tooltip.Replace("\\n", "\n");
            // Clean up double newlines
            tooltip = tooltip.Replace("\n\n", "\n");
            return tooltip.Trim();
        }
    }
}
