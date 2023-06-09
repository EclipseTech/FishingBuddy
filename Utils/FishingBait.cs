namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    using Blish_HUD;
    using Microsoft.Xna.Framework.Graphics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FishBait {
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

    public class FishingBait
    {
        internal static readonly Logger Logger = Logger.GetLogger(typeof(FishingBait));

        public readonly static Dictionary<FishBait, FishingBait> Bait = new Dictionary<FishBait, FishingBait>()
        {
           { FishBait.Any, new FishingBait { ItemId=96475, ChatLink="[&AgHbeAEA]", IconImg=FishingBuddyModule._imgBaitAny } },
           { FishBait.FishEggs, new FishingBait { ItemId=95886, ChatLink="[&AgGOdgEA]", IconImg=FishingBuddyModule._imgBaitFishEgg } },
           { FishBait.GlowWorms, new FishingBait { ItemId=95622, ChatLink="[&AgGGdQEA]", IconImg=FishingBuddyModule._imgBaitGlowWorm } },
           { FishBait.Minnows, new FishingBait { ItemId=97064, ChatLink="[&AgEoewEA]", IconImg=FishingBuddyModule._imgBaitFreshwaterMinnow } },
           { FishBait.LavaBeetles, new FishingBait { ItemId=97872, ChatLink="[&AgFQfgEA]", IconImg=FishingBuddyModule._imgBaitLavaBeetle } },
           { FishBait.Leeches, new FishingBait { ItemId=97880, ChatLink="[&AgFYfgEA]", IconImg=FishingBuddyModule._imgBaitLeech } },
           { FishBait.LightningBugs, new FishingBait { ItemId=95993, ChatLink="[&AgH5dgEA]", IconImg=FishingBuddyModule._imgBaitLightningBug } },
           { FishBait.Mackerel, new FishingBait { ItemId=95943, ChatLink="[&AgHHdgEA]", IconImg=FishingBuddyModule._imgBaitMackerel } },
           { FishBait.Nightcrawlers, new FishingBait { ItemId=96475, ChatLink="[&AgHbeAEA]", IconImg=FishingBuddyModule._imgBaitNightcrawler } },
           { FishBait.RamshornSnails, new FishingBait { ItemId=96186, ChatLink="[&AgG6dwEA]", IconImg=FishingBuddyModule._imgBaitRamshornSnail } },
           { FishBait.Sardines, new FishingBait { ItemId=96984, ChatLink="[&AgHYegEA]", IconImg=FishingBuddyModule._imgBaitSardine } },
           { FishBait.Scorpions, new FishingBait { ItemId=97569, ChatLink="[&AgEhfQEA]", IconImg=FishingBuddyModule._imgBaitScorpion } },
           { FishBait.Shrimplings, new FishingBait { ItemId=96319, ChatLink="[&AgE/eAEA]", IconImg=FishingBuddyModule._imgBaitShrimpling } },
           { FishBait.SparkflyLarvae, new FishingBait { ItemId=97745, ChatLink="[&AgHRfQEA]", IconImg=FishingBuddyModule._imgBaitSparkflyLarva } },
        };
        // Item Id
        public int ItemId { get; set; }
        //TODO save item code to clipboard on modifier+click
        public string ChatLink { get; set; }
        public Texture2D IconImg { get; set; }

        public static string BuildBaitTooltip(FishBait bait, List<Fish.FishingHole> fishingHoles) {
            //TODO get item name by ItemId API
            string name = bait.GetEnumMemberValue();
            string tooltip = $"{name}";
            if (bait == FishBait.Any) return tooltip.Trim();
            string holes = "";
            foreach (Fish.FishingHole hole in fishingHoles) { holes += $"  {hole.GetEnumMemberValue()}\n"; }
            tooltip = $"{name}\n{holes}";
            return tooltip.Trim();
        }
    }
}
