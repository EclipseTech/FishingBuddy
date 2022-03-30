using System;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class Fish
    {
        [Flags]
        public enum TimeOfDay
        {
            None = 0,
            Dawn = 1,
            Day = 2,
            Dusk = 4,
            Night = 8,
            DawnDusk = Dawn | Dusk,
            Any = Dawn | Day | Dusk | Night,
        }

        // Fish Item Name
        public string name { get; set; }
        // Item Id
        public int itemId { get; set; }
        // Junk, Basic, Fine, Rare, Masterwork, Exotic, Ascended, Legendary
        public string rarity { get; set; }
        // Fishing holes: Any, Boreal Fish, Cavern Fish, Channel Fish, Coastal Fish, Deep Fishing Hole, Desert Fish, Freshwater Fish, Grotto Fish, Lake Fish, Noxious Water Fish,
        // Offshore Fish, Polluted Lake Fish, Quarry Fish, Rare Fish, River Fish, Saltwater Fish, Special Fishing Hole, Shore Fish, Volcanic Fish, Wreckage Site
        public string fishingHole { get; set; }
        // Any, Fish Egg, Freshwater Minnow, Glow Worm, Lava Beetle, Leech, Lightning Bug, Mackerel, Nightcrawler, Ramshorn Snail, Sardine, Scorpion, Shrimpling, Sparkfly Nymph
        // https://wiki.guildwars2.com/wiki/Bait
        public string bait { get; set; }
        // Time of day fish can be caught
        public TimeOfDay time { get; set; }
        // Can fish be caught in open water?
        public bool openWater { get; set; }
        // Map region location ie Seitung Province.. should this be map id? list?
        public string location { get; set; }
        // Name of related fishing achievement
        public string achieve { get; set; }
        // Used for _ achievement or part of _ collection
        public string notes { get; set; }
    }
}


// fish data https://github.com/patrick-petersen/gw2-fishing/blob/4616c7021368b8b9811d4ca441398c2a3cda5697/src/api/FishData.tsx
//https://github.com/patrick-petersen/gw2-fishing/blob/master/src/api/FishData.tsx
// TODO Ascalon
// TODO Krytan
// TODO Maguuma
// TODO Shiverpeaks
// TODO Orrian
//private int[] DESERT_FISH_IDS = new int[] {96445, 96367, 97744, 97848, 96769, 96724, 97466, 96349, 96308, 96676, 97109, 96428, 96094, 97763, 96854, 97755, 95859, 95608, 97187, 97145, 95929};
//private int[] DESERT_ISLES_FISH_IDS = new int[] {97369, 96085, 95794, 97756, 97746, 96513, 96225, 95890, 96397, 97844, 97001, 97443, 95849, 97489};
// TODO Ring of Fire
//private int[] SEITUNG_FISH_IDS = new int[] {95894, 96350, 97278, 96425, 97604, 95603, 97753, 97865, 96719, 95936, 97692, 97722, 96523, 95926, 96757, 96071, 96944, 97061, 97714, 96318, 97181};
//private int[] KAINENG_FISH_IDS = new int[] {96297, 97885, 97074, 95875, 96985, 96105, 95609, 97584, 96176, 96942, 96226, 96672, 95843, 97004, 96532, 97121, 96931, 97887, 97479, 97163, 96081};
//private int[] ECHOVALD_WILDS_FISH_IDS = new int[] {96807, 96195, 96017, 96834, 95861, 95584, 97716, 96096, 95596, 96310, 96792, 97329, 95765, 97559};
//private int[] DRAGONS_END_FISH_IDS = new int[] {97240, 97814, 97853, 97183, 96443, 96181, 96817, 96913, 95729, 95670, 96076, 97794, 95699, 95632};
