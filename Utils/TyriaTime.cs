namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    using Blish_HUD;
    using Gw2Sharp.WebApi.V2.Models;
    using System;
    using System.Collections.Generic;

    // https://wiki.guildwars2.com/wiki/Day_and_night
    // Based on https://github.com/manlaan/BlishHud-Clock/
    public static class TyriaTime
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(TyriaTime));

        // Tyria times
        public static readonly DateTime CanthaDawnStart = new DateTime(2000, 1, 1, 7, 0, 0);
        public static readonly DateTime CanthaDayStart = new DateTime(2000, 1, 1, 8, 0, 0);
        public static readonly DateTime CanthaDuskStart = new DateTime(2000, 1, 1, 19, 0, 0);
        public static readonly DateTime CanthaNightStart = new DateTime(2000, 1, 1, 20, 0, 0);
        public static readonly DateTime CentralDawnStart = new DateTime(2000, 1, 1, 5, 0, 0);
        public static readonly DateTime CentralDayStart = new DateTime(2000, 1, 1, 6, 0, 0);
        public static readonly DateTime CentralDuskStart = new DateTime(2000, 1, 1, 20, 0, 0);
        public static readonly DateTime CentralNightStart = new DateTime(2000, 1, 1, 21, 0, 0);
        // Earth real times (UTC)
        public static readonly int CanthaDayLength = 55;
        public static readonly int CanthaNightLength = 55;
        public static readonly int CentralDayLength = 70;
        public static readonly int CentralNightLength = 40;
        public static readonly int DuskDawnLength = 5;
        public static readonly DateTime CanthaDawnStartUTC = new DateTime(2000, 1, 1, 0, 35, 0);
        public static readonly DateTime CanthaDayStartUTC = new DateTime(2000, 1, 1, 0, 40, 0);
        public static readonly DateTime CanthaDuskStartUTC = new DateTime(2000, 1, 1, 1, 35, 0);
        public static readonly DateTime CanthaNightStartUTC = new DateTime(2000, 1, 1, 1, 40, 0);
        public static readonly DateTime CentralDawnStartUTC = new DateTime(2000, 1, 1, 0, 25, 0);
        public static readonly DateTime CentralDayStartUTC = new DateTime(2000, 1, 1, 0, 30, 0);
        public static readonly DateTime CentralDuskStartUTC = new DateTime(2000, 1, 1, 1, 40, 0);
        public static readonly DateTime CentralNightStartUTC = new DateTime(2000, 1, 1, 1, 45, 0);
        // Map time info
        //TODO finish filling these out
        // https://wiki.guildwars2.com/wiki/Day_and_night#List_of_locations_with_day-night_cycle
        // Draconis Mons 1195 Always 9:00am, Thousand Seas Pavilion 1465 Day 12:00pm noon, Mistlock Sanctuary 1206 11:00am, Edge of the Mists 968 7:00am
        public static readonly List<int> AlwaysDayMaps = new List<int> { 1195, 1465, 1206, 968 };
        // The Nightmare Incarnate 1361 1:00am, The Twisted Marionette (Public/Private) 1413/1414 0:00am, Mad King's Realm 862/863/864/865/866/1304/1316, 
        public static readonly List<int> AlwaysNightMaps = new List<int> { 1361, 1413, 1414, 862, 863, 864, 865, 866, 1304, 1316 };

        public static DateTime CalcTyriaTime()
        {
            try
            {
                DateTime UTC = DateTime.UtcNow;
                int UTCsec = (UTC.Hour * 3600) + (UTC.Minute * 60) + UTC.Second;
                int TyrianSec = (UTCsec * 12) - 60;
                TyrianSec %= (3600 * 24);
                int TyrianHour = TyrianSec / 3600;
                TyrianSec %= 3600;
                int TyrianMin = TyrianSec / 60;
                TyrianSec %= 60;
                return new DateTime(2000, 1, 1, TyrianHour, TyrianMin, TyrianSec);
            }
            catch
            {
                return new DateTime(2000, 1, 1, 0, 0, 0);
            }
        }

        public static string CurrentMapPhase(Map map)
        {
            DateTime TyriaTime = CalcTyriaTime();

            if (AlwaysDayMaps.Contains(map.Id)) return Properties.Strings.Day;
            else if (AlwaysNightMaps.Contains(map.Id)) return Properties.Strings.Night;
            else if (map.RegionId == FishingMaps.CanthaRegionId)
            {   // Cantha Maps
                if (TyriaTime >= CanthaDawnStart && TyriaTime < CanthaDayStart)
                {
                    return Properties.Strings.Dawn;
                }
                else if (TyriaTime >= CanthaDayStart && TyriaTime < CanthaDuskStart)
                {
                    return Properties.Strings.Day;
                }
                else if (TyriaTime >= CanthaDuskStart && TyriaTime < CanthaNightStart)
                {
                    return Properties.Strings.Dusk;
                }
                else
                {
                    return Properties.Strings.Night;
                }
            }
            else
            {   // Central Tyria Maps
                if (TyriaTime >= CentralDawnStart && TyriaTime < CentralDayStart)
                {
                    return Properties.Strings.Dawn;
                }
                else if (TyriaTime >= CentralDayStart && TyriaTime < CentralDuskStart)
                {
                    return Properties.Strings.Day;
                }
                else if (TyriaTime >= CentralDuskStart && TyriaTime < CentralNightStart)
                {
                    return Properties.Strings.Dusk;
                }
                else
                {
                    return Properties.Strings.Night;
                }
            }
        }

        public static TimeSpan TimeTilNextPhase(Map map)
        {
            DateTime tyriaTime = CalcTyriaTime();
            DateTime now = DateTime.UtcNow;
            DateTime nowish = new DateTime(2000, 1, 1, now.Hour%2, now.Minute, now.Second);

            DateTime currentPhaseEnd;

            if (map is null || AlwaysDayMaps.Contains(map.Id) || AlwaysNightMaps.Contains(map.Id)) { return TimeSpan.Zero; }
            else if (map.RegionId == FishingMaps.CanthaRegionId)
            {   // Cantha Maps
                if (tyriaTime >= CanthaDawnStart && tyriaTime < CanthaDayStart)
                { // Cantha Dawn 5 min x:35->x:40
                    currentPhaseEnd = CanthaDawnStartUTC.AddMinutes(DuskDawnLength);
                }
                else if (tyriaTime >= CanthaDayStart && tyriaTime < CanthaDuskStart)
                { // Cantha day 55 min x:40->y:35
                    currentPhaseEnd = CanthaDayStartUTC.AddMinutes(CanthaDayLength);
                }
                else if (tyriaTime >= CanthaDuskStart && tyriaTime < CanthaNightStart)
                { // Cantha Dusk 5 min x:35->x:40
                    currentPhaseEnd = CanthaDuskStartUTC.AddMinutes(DuskDawnLength);
                }
                else
                { // Cantha Night 55 min x:40->y:35
                    currentPhaseEnd = CanthaNightStartUTC.AddMinutes(CanthaNightLength);
                    if (nowish.Hour == 0) nowish = nowish.AddHours(2);
                }
            }
            else
            {   // Central Tyria Maps
                if (tyriaTime >= CentralDawnStart && tyriaTime < CentralDayStart)
                { // Central Dawn 5 min x:25->x:30
                    currentPhaseEnd = CentralDawnStartUTC.AddMinutes(DuskDawnLength);
                }
                else if (tyriaTime >= CentralDayStart && tyriaTime < CentralDuskStart)
                { // Central Day 70 min x:30->y:40
                    currentPhaseEnd = CentralDayStartUTC.AddMinutes(CentralDayLength);
                }
                else if (tyriaTime >= CentralDuskStart && tyriaTime < CentralNightStart)
                { // Central Dusk 5 min x:40->x:45
                    currentPhaseEnd = CentralDuskStartUTC.AddMinutes(DuskDawnLength);
                }
                else
                { // Central Night 40 min x:45->y:25
                    currentPhaseEnd = CentralNightStartUTC.AddMinutes(CentralNightLength);
                    if (nowish.Hour == 0) nowish = nowish.AddHours(2);
                }
            }
            return currentPhaseEnd.Subtract(nowish);
        }
    }
}
