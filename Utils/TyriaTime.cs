using Blish_HUD;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    // https://wiki.guildwars2.com/wiki/Day_and_night
    // Based on https://github.com/manlaan/BlishHud-Clock/
    class TyriaTime
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
        public static readonly DateTime canthaDawnStartUTC = new DateTime(2000, 1, 1, 0, 35, 0);
        public static readonly DateTime canthaDayStartUTC = new DateTime(2000, 1, 1, 0, 40, 0);
        public static readonly DateTime canthaDuskStartUTC = new DateTime(2000, 1, 1, 0, 35, 0);
        public static readonly DateTime canthaNightStartUTC = new DateTime(2000, 1, 1, 0, 40, 0);
        public static readonly DateTime centralDawnStartUTC = new DateTime(2000, 1, 1, 0, 25, 0);
        public static readonly DateTime centralDayStartUTC = new DateTime(2000, 1, 1, 0, 30, 0);
        public static readonly DateTime centralDuskStartUTC = new DateTime(2000, 1, 1, 0, 40, 0);
        public static readonly DateTime centralNightStartUTC = new DateTime(2000, 1, 1, 0, 45, 0);
        public static readonly DateTime _0h = new DateTime(2000, 1, 1, 0, 0, 0);
        public static readonly DateTime _1h = new DateTime(2000, 1, 1, 1, 0, 0);
        // Map time info
        public static readonly List<int> CanthaMaps = new List<int> { 1442, 1419, 1444, 1462, 1438, 1452, 1428, 1422 };
        public static readonly int CanthaRegionId = 37;
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
            else if (map.RegionId == CanthaRegionId)
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

        public static DateTime NextPhaseTime(Map map) => DateTime.Now + TimeTilNextPhase(map);

        public static TimeSpan TimeTilNextPhase(Map map)
        {
            DateTime TyriaTime = CalcTyriaTime();
            DateTime nowish = new DateTime(2000, 1, 1, 0, DateTime.Now.Minute, DateTime.Now.Second);
            TimeSpan timeTilNextPhase = TimeSpan.Zero;

            if (map is null || AlwaysDayMaps.Contains(map.Id) || AlwaysNightMaps.Contains(map.Id)) timeTilNextPhase = TimeSpan.Zero;
            else if (map.RegionId == CanthaRegionId)
            {   // Cantha Maps
                if (TyriaTime >= canthaDawnStart && TyriaTime < canthaDayStart)
                { // 5 min
                    timeTilNextPhase = canthaDayStartUTC.Subtract(nowish);
                }
                else if (TyriaTime >= canthaDayStart && TyriaTime < canthaDuskStart)
                { // 55 min
                    if (nowish >= canthaDayStartUTC && nowish < _1h)
                    {
                        timeTilNextPhase = _1h.Subtract(nowish);
                        timeTilNextPhase += canthaDuskStartUTC.Subtract(_0h);
                    }
                    else
                    { timeTilNextPhase = canthaDuskStartUTC.Subtract(nowish); }
                }
                else if (TyriaTime >= canthaDuskStart && TyriaTime < canthaNightStart)
                { // 5 min
                    timeTilNextPhase = canthaNightStartUTC.Subtract(nowish);
                }
                else
                { // 55 min
                    if (nowish >= canthaNightStartUTC && nowish < _1h)
                    {
                        timeTilNextPhase = _1h.Subtract(nowish);
                        timeTilNextPhase += canthaDawnStartUTC.Subtract(_0h);
                    }
                    else
                    { timeTilNextPhase = canthaDuskStartUTC.Subtract(nowish); }
                }
            }
            else
            {   // Central Tyria Maps
                if (TyriaTime >= centralDawnStart && TyriaTime < centralDayStart)
                { // 5 min
                    timeTilNextPhase = centralDayStartUTC.Subtract(nowish);
                }
                else if (TyriaTime >= centralDayStart && TyriaTime < centralDuskStart)
                { // 70 min
                    if (nowish >= centralDayStartUTC && nowish < _1h)
                    {
                        timeTilNextPhase = _1h.Subtract(nowish);
                        timeTilNextPhase += centralDuskStartUTC.Subtract(_0h);
                    }
                    else
                    { timeTilNextPhase = centralDuskStartUTC.Subtract(nowish); }
                }
                else if (TyriaTime >= centralDuskStart && TyriaTime < centralNightStart)
                { // 5 min
                    timeTilNextPhase = centralNightStartUTC.Subtract(nowish);
                }
                else
                { // 40 min
                    if (nowish >= centralNightStartUTC && nowish < _1h)
                    {
                        timeTilNextPhase = _1h.Subtract(nowish);
                        timeTilNextPhase += centralDawnStartUTC.Subtract(_0h);
                    }
                    else
                    { timeTilNextPhase = centralDawnStartUTC.Subtract(nowish); }
                }
            }
            return timeTilNextPhase;
        }
    }
}
