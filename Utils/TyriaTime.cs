using System;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class TyriaTime
    {
        public static readonly DateTime canthaDawnStart = new DateTime(2000, 1, 1, 7, 0, 0);
        public static readonly DateTime canthaDayStart = new DateTime(2000, 1, 1, 8, 0, 0);
        public static readonly DateTime canthaDuskStart = new DateTime(2000, 1, 1, 19, 0, 0);
        public static readonly DateTime canthaNightStart = new DateTime(2000, 1, 1, 20, 0, 0);
        public static readonly DateTime centralDawnStart = new DateTime(2000, 1, 1, 5, 0, 0);
        public static readonly DateTime centralDayStart = new DateTime(2000, 1, 1, 6, 0, 0);
        public static readonly DateTime centralDuskStart = new DateTime(2000, 1, 1, 20, 0, 0);
        public static readonly DateTime centralNightStart = new DateTime(2000, 1, 1, 21, 0, 0);

        //  TODO display time https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L86
        public static string CurrentMapTime(int MapId)
        {
            DateTime TyriaTime = CalcTyriaTime();

            if (MapId == 1452 /*Echovald*/ || MapId == 1442 /*Seitung*/ || MapId == 1438 /*Kaineng*/ || MapId == 1422 /*Dragon's End*/ || MapId == 1462 /*Guild Hall*/ )
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
