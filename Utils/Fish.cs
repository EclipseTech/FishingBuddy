namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class Fish
    {
        public enum TimeOfDay
        {
            Any,
            Nighttime,
            Daytime,
            DuskDawn
        }

        string fish;
        string fishingHole;
        string bait;
        TimeOfDay time;
        bool openWater;
        string quality;
        string location;
        string notes;
        string achieve;
        string achieveOrder;
    }
}
