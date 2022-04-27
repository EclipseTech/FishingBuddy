using Blish_HUD;
using System.Collections.Generic;


namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class FishingMaps
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(FishingMaps));

        public Dictionary<int, List<int>> MapAchievements { get; }

        public FishingMaps()
        {
            this.MapAchievements = new Dictionary<int, List<int>>();
            foreach (int mapId in AscalonianMaps) this.MapAchievements.Add(mapId, AscalonianFisher);
            foreach (int mapId in KrytanMaps) this.MapAchievements.Add(mapId, KrytanFisher);
            foreach (int mapId in MaguumaMaps) this.MapAchievements.Add(mapId, MaguumaFisher);
            foreach (int mapId in ShiverpeaksMaps) this.MapAchievements.Add(mapId, ShiverpeaksFisher);
            foreach (int mapId in OrrianMaps) this.MapAchievements.Add(mapId, OrrianFisher);
            foreach (int mapId in DesertMaps) this.MapAchievements.Add(mapId, DesertFisher);
            foreach (int mapId in DesertIslesMaps) this.MapAchievements.Add(mapId, DesertIslesFisher);
            foreach (int mapId in RingOfFireMaps) this.MapAchievements.Add(mapId, RingOfFireFisher);
            foreach (int mapId in SeitungProvinceMaps) this.MapAchievements.Add(mapId, SeitungProvinceFisher);
            foreach (int mapId in KainengMaps) this.MapAchievements.Add(mapId, KainengFisher);
            foreach (int mapId in EchovaldWildsMaps) this.MapAchievements.Add(mapId, EchovaldWildsFisher);
            foreach (int mapId in DragonsEndMaps) this.MapAchievements.Add(mapId, DragonsEndFisher);
            foreach (int mapId in ThousandSeasPavilion) this.MapAchievements.Add(mapId, ThousandSeasPavilionFisher);
        }

        // All from fishing achievement category 317 https://api.guildwars2.com/v2/achievements/categories/317
        public readonly static int FISHING_ACHIEVEMENT_CATEGORY_ID = 317;
        // Ascalonian Fisher 6330 Avid Ascalonian Fisher 6484
        public readonly static List<int> AscalonianFisher = new List<int> { 6330, 6484 };
        // Ascalonian Maps: Fireheart Rise 22, Diessa Plateau 32, Plains of Ashford 19, Grothmar Valley 1330, Fields of Ruin 21, Iron Marches 25, (20 Blazeridge Steppes? Does this map have any fishing holes?)
        public readonly static List<int> AscalonianMaps = new List<int> { 22, 32, 19, 1330, 21, 25 };
        // Krytan Fisher 6068 Avid Krytan Fisher 6263
        public readonly static List<int> KrytanFisher = new List<int> { 6068, 6263 };
        // Krytan Maps: Bloodtide Coast 73, Harathi Hinterlands 17, Gendarran Fields 24, Lion's Arch 50, Southsun Cove 873, Kessex Hills 23, Queensdale 15, Lake Doric 1185
        public readonly static List<int> KrytanMaps = new List<int> { 73, 17, 24, 50, 873, 23, 15, 1185 };
        // Maguuma Fisher 6344 Avid Maguuma Fisher 6475
        public readonly static List<int> MaguumaFisher = new List<int> { 6344, 6475 };
        // Maguuma Maps: Sparkfly Fen 53, Mount Maelstrom 39, Caledon Forest 34, Metrica Province 35, Brisban Wildlands 54, Rata Sum 139, Guilded Hollow 1068, 1101, 1107, 1108, 1121, Lost Precipice 1069, 1071, 1076, 1104, 1124
        public readonly static List<int> MaguumaMaps = new List<int> { 53, 39, 34, 35, 54, 139, 1068, 1101, 1107, 1108, 1121, 1069, 1071, 1076, 1104, 1124 };
        // Shiverpeaks Fisher 6179 Avid Shiverpeaks Fisher 6153
        public readonly static List<int> ShiverpeaksFisher = new List<int> { 6179, 6153 };
        // Shiverpeaks Maps: Frostgorge Sound 30, Drizzlewood Coast 1371, Thunderhead Peaks 1310, Timberline Falls 29, Lornar's Pass 27, Snowden Drifts 31, Wayfarer Foothills 28, Bitterfrost Frontier 1178
        public readonly static List<int> ShiverpeaksMaps = new List<int> { 30, 1371, 1310, 29, 27, 31, 28, 1178 };
        // Orrian Fisher 6363 Avid Orrian Fisher 6227
        public readonly static List<int> OrrianFisher = new List<int> { 6363, 6227 };
        // Orrian Maps: Siren's Landing 1203, Straits of Devastation 51, Malchor's Leap 65, Cursed Shore 62
        public readonly static List<int> OrrianMaps = new List<int> { 1203, 51, 65, 62 };
        // Desert Fisher 6317 Avid Desert Fisher 6509
        public readonly static List<int> DesertFisher = new List<int> { 6317, 6509 };
        // Desert Maps: Crystal Oasis 1210, Domain of Kourna 1288, The Desolation 1226, Elon Riverlands 1228, Desert Highlands 1211, Windswept Haven 1214, 1215, 1224, 1232, 1243, 1250
        public readonly static List<int> DesertMaps = new List<int> { 1210, 1288, 1226, 1228, 1211, 1214, 1215, 1224, 1232, 1243, 1250 };
        // Desert Isles Fisher 6106 Avid Desert Isles Fisher 6250
        public readonly static List<int> DesertIslesFisher = new List<int> { 6106, 6250 };
        // Desert Isles Maps: Domain of Istan 1263 & Sandswept Isles 1271
        public readonly static List<int> DesertIslesMaps = new List<int> { 1263, 1271 };
        // Ring of Fire Fisher 6489 Avid Ring of Fire Fisher 6339
        public readonly static List<int> RingOfFireFisher = new List<int> { 6489, 6339 };
        // Ring of Fire Maps: Ember Bay 1175 & Draconis Mons 1195
        public readonly static List<int> RingOfFireMaps = new List<int> { 1175, 1195 };

        // Cantha EoD Maps
        public readonly static List<int> CanthaMaps = new List<int> { 1442, 1419, 1444, 1462, 1438, 1452, 1428, 1422 };
        // Seitung Province Fisher 6336 Avid Seitung Province Fisher 6264
        public readonly static List<int> SeitungProvinceFisher = new List<int> { 6336, 6264 };
        // Seitung Province Maps: Seitung Province 1442 & Isle of Reflection 1419, 1444, 1462
        public readonly static List<int> SeitungProvinceMaps = new List<int> { 1442, 1419, 1444, 1462 };
        // Kaineng Fisher 6342 Avid Kaineng Fisher 6192
        public readonly static List<int> KainengFisher = new List<int> { 6342, 6192 };
        // Kaineng Maps: New Kaineng City 1438
        public readonly static List<int> KainengMaps = new List<int> { 1438 };
        // Echovald Wilds Fisher 6258 Avid Echovald Wilds Fisher 6466
        public readonly static List<int> EchovaldWildsFisher = new List<int> { 6258, 6466 };
        // Echovald Wilds Maps: The Echovald Wilds 1452 & Arborstone 1428
        public readonly static List<int> EchovaldWildsMaps = new List<int> { 1452, 1428 };
        // Dragon's End Fisher 6506 Avid Dragon's End Fisher 6402
        public readonly static List<int> DragonsEndFisher = new List<int> { 6506, 6402 };
        // Dragon's End Maps: Dragon's End 1422
        public readonly static List<int> DragonsEndMaps = new List<int> { 1422 };
        // Thousand Seas Pavilion 1465 Day 12:00 noon https://wiki.guildwars2.com/wiki/Mysterious_Waters_Fish contains Seitung Province & Kaineng daytime non-Ascended and non-Legendary fish
        public readonly static List<int> ThousandSeasPavilionFisher = new List<int> { 6336, 6264, 6342, 6192 };
        public readonly static List<int> ThousandSeasPavilion = new List<int> { 1465 };

        // World Class Fisher 6224 Avid World Class Fisher 6110
        public readonly static List<int> WorldClassFisher = new List<int> { 6224, 6110 };
        // Saltwater Fisher 6471 Avid Saltwater Fisher 6393
        public readonly static List<int> SaltwaterFisher = new List<int> { 6471, 6393 };
        // https://wiki.guildwars2.com/wiki/API:2/account/achievements
        public readonly static List<int> FISHER_ACHIEVEMENT_IDS = new List<int> { 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402, 6224, 6110, 6471, 6393 };
        public readonly static List<int> BASE_FISHER_ACHIEVEMENT_IDS = new List<int> { 6330, 6068, 6344, 6179, 6363, 6317, 6106, 6489, 6336, 6342, 6258, 6506, 6224, 6471 };
        public readonly static List<int> AVID_FISHER_ACHIEVEMENT_IDS = new List<int> { 6484, 6263, 6475, 6153, 6227, 6509, 6250, 6339, 6264, 6192, 6466, 6402, 6110, 6393 };
    }
}
