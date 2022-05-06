using Blish_HUD;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    // https://wiki.guildwars2.com/wiki/Day_and_night
    // Based on https://github.com/manlaan/BlishHud-Clock/
    public static class TyriaTime
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(TyriaTime));

        // Tyria times
        public static readonly DateTime canthaDawnStart = new DateTime(2000, 1, 1, 7, 0, 0);
        public static readonly DateTime canthaDayStart = new DateTime(2000, 1, 1, 8, 0, 0);
        public static readonly DateTime canthaDuskStart = new DateTime(2000, 1, 1, 19, 0, 0);
        public static readonly DateTime canthaNightStart = new DateTime(2000, 1, 1, 20, 0, 0);
        public static readonly DateTime centralDawnStart = new DateTime(2000, 1, 1, 5, 0, 0);
        public static readonly DateTime centralDayStart = new DateTime(2000, 1, 1, 6, 0, 0);
        public static readonly DateTime centralDuskStart = new DateTime(2000, 1, 1, 20, 0, 0);
        public static readonly DateTime centralNightStart = new DateTime(2000, 1, 1, 21, 0, 0);
        // Earth real times (UTC)
        public static readonly int canthaDayLength = 55;
        public static readonly int canthaNightLength = 55;
        public static readonly int centralDayLength = 70;
        public static readonly int centralNightLength = 40;
        public static readonly int DuskDawnLength = 5;
        public static readonly DateTime canthaDawnStartUTC = new DateTime(2000, 1, 1, 0, 35, 0);
        public static readonly DateTime canthaDayStartUTC = new DateTime(2000, 1, 1, 0, 40, 0);
        public static readonly DateTime canthaDuskStartUTC = new DateTime(2000, 1, 1, 1, 35, 0);
        public static readonly DateTime canthaNightStartUTC = new DateTime(2000, 1, 1, 1, 40, 0);
        public static readonly DateTime centralDawnStartUTC = new DateTime(2000, 1, 1, 0, 25, 0);
        public static readonly DateTime centralDayStartUTC = new DateTime(2000, 1, 1, 0, 30, 0);
        public static readonly DateTime centralDuskStartUTC = new DateTime(2000, 1, 1, 1, 40, 0);
        public static readonly DateTime centralNightStartUTC = new DateTime(2000, 1, 1, 1, 45, 0);
        // Map time info
        //TODO finish filling these out
        // https://wiki.guildwars2.com/wiki/Day_and_night#List_of_locations_with_day-night_cycle
        // Draconis Mons 1195 Always 9:00am, Thousand Seas Pavilion 1465 Day 12:00pm noon, Mistlock Sanctuary 1206 11:00am, Edge of the Mists 968 7:00am
        public static readonly List<int> AlwaysDayMaps = new List<int> { 1195, 1465, 1206, 968 };
        // The Nightmare Incarnate 1361 1:00am, The Twisted Marionette (Public/Private) 1413/1414 0:00am, Mad King's Realm 862/863/864/865/866/1304/1316, 
        public static readonly List<int> AlwaysNightMaps = new List<int> { 1361, 1413, 1414, 862, 863, 864, 865, 866, 1304, 1316 };

        public static string CurrentMapPhase(Map map)
        {
            DateTime TyriaTime = CalcTyriaTime();

            if (AlwaysDayMaps.Contains(map.Id)) return "Day";
            else if (AlwaysNightMaps.Contains(map.Id)) return "Night";
            else if (map.RegionId == FishingMaps.CanthaRegionId)
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
                int tyrianhour = tyriasec / 3600;
                tyriasec %= 3600;
                int tyrianmin = tyriasec / 60;
                tyriasec %= 60;
                return new DateTime(2000, 1, 1, tyrianhour, tyrianmin, tyriasec);
            }
            catch
            {
                return new DateTime(2000, 1, 1, 0, 0, 0);
            }

        }

        public static TimeSpan TimeTilNextPhase(Map map)
        {
            DateTime TyriaTime = CalcTyriaTime();
            DateTime now = DateTime.UtcNow;
            DateTime nowish = new DateTime(2000, 1, 1, now.Hour%2, now.Minute, now.Second);

            DateTime currentPhaseEnd;

            if (map is null || AlwaysDayMaps.Contains(map.Id) || AlwaysNightMaps.Contains(map.Id)) { return TimeSpan.Zero; }
            else if (map.RegionId == FishingMaps.CanthaRegionId)
            {   // Cantha Maps
                if (TyriaTime >= canthaDawnStart && TyriaTime < canthaDayStart)
                { // Cantha Dawn 5 min x:35->x:40
                    currentPhaseEnd = canthaDawnStartUTC.AddMinutes(DuskDawnLength);
                }
                else if (TyriaTime >= canthaDayStart && TyriaTime < canthaDuskStart)
                { // Cantha day 55 min x:40->y:35
                    currentPhaseEnd = canthaDayStartUTC.AddMinutes(canthaDayLength);
                }
                else if (TyriaTime >= canthaDuskStart && TyriaTime < canthaNightStart)
                { // Cantha Dusk 5 min x:35->x:40
                    currentPhaseEnd = canthaDuskStartUTC.AddMinutes(DuskDawnLength);
                }
                else
                { // Cantha Night 55 min x:40->y:35
                    currentPhaseEnd = canthaNightStartUTC.AddMinutes(canthaNightLength);
                    if (nowish.Hour == 0) nowish = nowish.AddHours(2);
                }
            }
            else
            {   // Central Tyria Maps
                if (TyriaTime >= centralDawnStart && TyriaTime < centralDayStart)
                { // Central Dawn 5 min x:25->x:30
                    currentPhaseEnd = centralDawnStartUTC.AddMinutes(DuskDawnLength);
                }
                else if (TyriaTime >= centralDayStart && TyriaTime < centralDuskStart)
                { // Central Day 70 min x:30->y:40
                    currentPhaseEnd = centralDayStartUTC.AddMinutes(centralDayLength);
                }
                else if (TyriaTime >= centralDuskStart && TyriaTime < centralNightStart)
                { // Central Dusk 5 min x:40->x:45
                    currentPhaseEnd = centralDuskStartUTC.AddMinutes(DuskDawnLength);
                }
                else
                { // Central Night 40 min x:45->y:25
                    currentPhaseEnd = centralNightStartUTC.AddMinutes(centralNightLength);
                    if (nowish.Hour == 0) nowish = nowish.AddHours(2);
                }
            }
            return currentPhaseEnd.Subtract(nowish);
        }
    }
}
