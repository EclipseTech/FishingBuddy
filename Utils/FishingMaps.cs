using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class FishingMaps
    {
        public Dictionary<int, List<int>> mapAchievements { get { return _mapAchievements; } }
        private Dictionary<int, List<int>> _mapAchievements; //mapId, achievementIds... <int, List<int>>

        public FishingMaps()
        {
            this._mapAchievements = new Dictionary<int, List<int>>();
            foreach (int mapId in AscalonianMaps) _mapAchievements.Add(mapId, AscalonianFisher);
            foreach (int mapId in KrytanMaps) _mapAchievements.Add(mapId, KrytanFisher);
            foreach (int mapId in MaguumaMaps) _mapAchievements.Add(mapId, MaguumaFisher);
            foreach (int mapId in ShiverpeaksMaps) _mapAchievements.Add(mapId, ShiverpeaksFisher);
            foreach (int mapId in OrrianMaps) _mapAchievements.Add(mapId, OrrianFisher);
            foreach (int mapId in DesertMaps) _mapAchievements.Add(mapId, DesertFisher);
            foreach (int mapId in DesertIslesMaps) _mapAchievements.Add(mapId, DesertIslesFisher);
            foreach (int mapId in RingOfFireMaps) _mapAchievements.Add(mapId, RingOfFireFisher);
            foreach (int mapId in SeitungProvinceMaps) _mapAchievements.Add(mapId, SeitungProvinceFisher);
            foreach (int mapId in KainengMaps) _mapAchievements.Add(mapId, KainengFisher);
            foreach (int mapId in EchovaldWildsMaps) _mapAchievements.Add(mapId, EchovaldWildsFisher);
            foreach (int mapId in DragonsEndMaps) _mapAchievements.Add(mapId, DragonsEndFisher);
        }

        // All from fishing achievement category 317 https://api.guildwars2.com/v2/achievements/categories/317
        public readonly int FISHING_ACHIEVEMENT_CATEGORY_ID = 317;
        // Ascalonian Fisher 6330 Avid Ascalonian Fisher 6484
        public readonly List<int> AscalonianFisher = new List<int> { 6330, 6484 };
        // Ascalonian Maps 22 Fireheart Rise, 32 Diessa Plateau, 19 Plains of Ashford, 1330 Grothmar Valley, 21 Fields of Ruin, 25 Iron Marches (20 Blazeridge Steppes?)
        public readonly List<int> AscalonianMaps = new List<int> { 22, 32, 19, 1330, 21, 25 };
        // Krytan Fisher 6068 Avid Krytan Fisher 6263
        public readonly List<int> KrytanFisher = new List<int> { 6068, 6263 };
        // 73 Bloodtide Coast, 17 Harathi Hinterlands, 24 Gendarran Fields, 50 Lion's Arch, 873 Southsun Cove, 23 Kessex Hills, 15 Queensdale, 1185 Lake Doric
        public readonly List<int> KrytanMaps = new List<int> { 73, 17, 24, 50, 873, 23, 15, 1185 };
        // Maguuma Fisher 6344 Avid Maguuma Fisher 6475
        public readonly List<int> MaguumaFisher = new List<int> { 6344, 6475 };
        // 53 Sparkfly Fen, 39 Mount Maelstrom, 34 Caledon Forest, 35 Metrica Province, 54 Brisban Wildlands (missing guild halls)
        public readonly List<int> MaguumaMaps = new List<int> { 53, 39, 34, 35, 54 };
        // Shiverpeaks Fisher 6179 Avid Shiverpeaks Fisher 6153
        public readonly List<int> ShiverpeaksFisher = new List<int> { 6179, 6153 };
        // Shiverpeaks Maps 30 Frostgorge Sound, 1371 Drizzlewood Coast, 1310 Thunderhead Peaks, 29 Timberline Falls, 27 Lornar's Pass, 31 Snowden Drifts, 28 Wayfarer Foothills
        public readonly List<int> ShiverpeaksMaps = new List<int> { 30, 1371, 1310, 29, 27, 31, 28 };
        // Orrian Fisher 6363 Avid Orrian Fisher 6227
        public readonly List<int> OrrianFisher = new List<int> { 6363, 6227 };
        // Orrian Maps 1203 Siren's Landing, 51 Straits of Devastation, 65 Malchor's Leap, 62 Cursed Shore
        public readonly List<int> OrrianMaps = new List<int> { 1203, 51, 65, 62 };
        // Desert Fisher 6317 Avid Desert Fisher 6509
        public readonly List<int> DesertFisher = new List<int> { 6317, 6509 };
        // Desert Maps 1210 Crystal Oasis, 1288 Domain of Kourna, 1226 The Desolation, 1228 Elon Riverlands, 1211 Desert Highlands, 1214 Windswept Haven
        public readonly List<int> DesertMaps = new List<int> { 1210, 1288, 1226, 1228, 1211, 1214 };
        //TODO split out special case of desert isles & ring of fire achievements fish per zone
        // Desert Isles Fisher 6106 Avid Desert Isles Fisher 6250
        public readonly List<int> DesertIslesFisher = new List<int> { 6106, 6250 };
        // Desert Isles Maps 1263 Domain of Istan & 1271 Sandswept Isles
        public readonly List<int> DesertIslesMaps = new List<int> { 1263, 1271 };
        // Ring of Fire Fisher 6489 Avid Ring of Fire Fisher 6339
        public readonly List<int> RingOfFireFisher = new List<int> { 6489, 6339 };
        // Ring of Fire Maps 1175 Ember Bay & 1195 Draconis Mons
        public readonly List<int> RingOfFireMaps = new List<int> { 1175, 1195 };
        // Seitung Province Fisher 6336 Avid Seitung Province Fisher 6264
        public readonly List<int> SeitungProvinceFisher = new List<int> { 6336, 6264 };
        // Seitung Province Maps 1442 Seitung Province & ?1419 Isle of Reflection?
        public readonly List<int> SeitungProvinceMaps = new List<int> { 1442, 1419 };
        // Kaineng Fisher 6342 Avid Kaineng Fisher 6192
        public readonly List<int> KainengFisher = new List<int> { 6342, 6192 };
        // Kaineng Maps 1438 New Kaineng City
        public readonly List<int> KainengMaps = new List<int> { 1438 };
        // Echovald Wilds Fisher 6258 Avid Echovald Wilds Fisher 6466
        public readonly List<int> EchovaldWildsFisher = new List<int> { 6258, 6466 };
        // Echovald Wilds Maps 1452 The Echovald Wilds & 1428 Arborstone
        public readonly List<int> EchovaldWildsMaps = new List<int> { 1452, 1428 };
        // Dragon's End Fisher 6506 Avid Dragon's End Fisher 6402
        public readonly List<int> DragonsEndFisher = new List<int> { 6506, 6402 };
        // Dragon's End Maps 1422 Dragon's End
        public readonly List<int> DragonsEndMaps = new List<int> { 1422 };
        // World Class Fisher 6224 Avid World Class Fisher 6110
        public readonly List<int> WorldClassFisher = new List<int> { 6224, 6110 };
        // Saltwater Fisher 6471 Avid Saltwater Fisher 6393
        public readonly List<int> SaltwaterFisher = new List<int> { 6471, 6393 };
    }
}
