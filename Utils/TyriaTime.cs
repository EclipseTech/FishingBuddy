using Blish_HUD;
using System;
using System.Collections.Generic;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    // https://wiki.guildwars2.com/wiki/Day_and_night
    class TyriaTime
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(TyriaTime));

        public static readonly DateTime canthaDawnStart = new DateTime(2000, 1, 1, 7, 0, 0);
        public static readonly DateTime canthaDayStart = new DateTime(2000, 1, 1, 8, 0, 0);
        public static readonly DateTime canthaDuskStart = new DateTime(2000, 1, 1, 19, 0, 0);
        public static readonly DateTime canthaNightStart = new DateTime(2000, 1, 1, 20, 0, 0);
        public static readonly DateTime centralDawnStart = new DateTime(2000, 1, 1, 5, 0, 0);
        public static readonly DateTime centralDayStart = new DateTime(2000, 1, 1, 6, 0, 0);
        public static readonly DateTime centralDuskStart = new DateTime(2000, 1, 1, 20, 0, 0);
        public static readonly DateTime centralNightStart = new DateTime(2000, 1, 1, 21, 0, 0);
        public static readonly List<int> CanthaMaps = new List<int> { 1442, 1419, 1444, 1462, 1438, 1452, 1428, 1422 };

        //  TODO display time https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L86
        public static string CurrentMapTime(int MapId)
        {
            DateTime TyriaTime = CalcTyriaTime();

            if (CanthaMaps.Contains(MapId))
            {   // Cantha Maps
                if (TyriaTime >= canthaDawnStart && TyriaTime < canthaDayStart)
                {
                    return "Dawn";
                }
                else if (TyriaTime >= canthaDayStart && TyriaTime < canthaDuskStart)
                {
                    return "Day";
                }
                else if (TyriaTime >= canthaDuskStart && TyriaTime < canthaNightStart)
                {
                    return "Dusk";
                }
                else
                {
                    return "Night";
                }
            }
            else
            {   // Central Tyria Maps
                if (TyriaTime >= centralDawnStart && TyriaTime < centralDayStart)
                {
                    return "Dawn";
                }
                else if (TyriaTime >= centralDayStart && TyriaTime < centralDuskStart)
                {
                    return "Day";
                }
                else if (TyriaTime >= centralDuskStart && TyriaTime < centralNightStart)
                {
                    return "Dusk";
                }
                else
                {
                    return "Night";
                }
            }
        }

        public static DateTime CalcTyriaTime()
        {
            try
            {
                DateTime UTC = DateTime.UtcNow;
                int utcsec = utcsec = (UTC.Hour * 3600) + (UTC.Minute * 60) + UTC.Second;
                int tyriasec = (utcsec * 12) - 60;
                tyriasec %= (3600 * 24);
                int tyrianhour = (int)(tyriasec / 3600);
                tyriasec %= 3600;
                int tyrianmin = (int)(tyriasec / 60);
                tyriasec %= 60;
                return new DateTime(2000, 1, 1, tyrianhour, tyrianmin, tyriasec);
            }
            catch
            {
                return new DateTime(2000, 1, 1, 0, 0, 0);
            }

        }
    }
}
