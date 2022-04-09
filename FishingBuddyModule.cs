using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel.Composition;
using Eclipse1807.BlishHUD.FishingBuddy.Utils;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD.Content;
using Blish_HUD.Controls;


// TODO display timeofday countdown timer
// TODO should other map Ids not show any fishing info, or show open water info? or saltwater/world class info? hide in instances?
// TODO cache fishing images from api, save / download icons to directory cache & get from cache before web, Download / Use Icon w/ GetRenderServiceTexture ex: GameService.Content.GetRenderServiceTexture(fish.Icon);
// TODO should be caching map info too
// TODO in bounds checking for UI elements, ex: https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L64 & https://github.com/manlaan/BlishHud-Clock/blob/main/Module.cs#L145
// TODO notifications? on dawn https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs#L108 ?15 sec til?
// TODO add item id to fish.json
// TODO add achievement id to fish.json
// TODO (inventory permissions required) Add caught fish counter (count per rarity & ? count per type of fish ? per zone ? per session ? per hour ?)
// TODO BLOCKED get/display equipped lure & bait w/ #s (optional w/ mouseover info)
// TODO BLOCKED bait & lure icons via api... get bait & lure type/count from api? is this even detailed anywhere? or no api yet for this?
// TODO add option to ignore time of day


namespace Eclipse1807.BlishHUD.FishingBuddy
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class FishingBuddyModule : Blish_HUD.Modules.Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(FishingBuddyModule));

        internal static FishingBuddyModule ModuleInstance;

        //private Texture2D _imgLure;
        //private Texture2D _imgBait;
        private static Texture2D _imgBorderBlack;
        private static Texture2D _imgBorderJunk;
        private static Texture2D _imgBorderBasic;
        private static Texture2D _imgBorderFine;
        private static Texture2D _imgBorderMasterwork;
        private static Texture2D _imgBorderRare;
        private static Texture2D _imgBorderExotic;
        private static Texture2D _imgBorderAscended;
        private static Texture2D _imgBorderLegendary;
        private static Texture2D _imgBorderX;
        internal static Texture2D _imgDawn;
        internal static Texture2D _imgDay;
        internal static Texture2D _imgDusk;
        internal static Texture2D _imgNight;

        private AsyncCache<int, Map> _mapRepository;

        private static ClickThroughPanel _fishPanel;
        private bool _draggingFishPanel;
        private Point _dragFishPanelStart = Point.Zero;
        public static SettingEntry<bool> _dragFishPanel;
        public static SettingEntry<int> _fishImgWidth;
        public static SettingEntry<Point> _fishPanelLoc;
        public static SettingEntry<bool> _dragTimeOfDayClock;
        public static SettingEntry<int> _timeOfDayImgWidth;
        public static SettingEntry<Point> _timeOfDayPanelLoc;
        public static SettingEntry<bool> _ignoreCaughtFish;
        public static SettingEntry<bool> _includeWorldClass;
        public static SettingEntry<bool> _includeSaltwater;
        public static SettingEntry<bool> _displayUncatchableFish;
        public static SettingEntry<bool> _hideInCombat;
        public static SettingEntry<bool> _hideTimeOfDay;
        private List<Fish> catchableFish;
        private FishingMaps fishingMaps;
        private IEnumerable<AccountAchievement> accountFishingAchievements;
        public static SettingEntry<bool> _showRarityBorder;
        //public static string[] _clockAlign = new string[] { "Top", "Bottom" };
        //public static SettingEntry<string> _settingClockAlign;
        private Clock _timeOfDayClock;

        private List<Fish> _allFishList;
        private Map _currentMap;
        private bool _useAPIToken;
        private readonly SemaphoreSlim _updateFishSemaphore = new SemaphoreSlim(1, 1);
        private bool MumbleIsAvailable => GameService.Gw2Mumble.IsAvailable && GameService.GameIntegration.Gw2Instance.IsInGame;
        private bool uiIsAvailable => MumbleIsAvailable && !GameService.Gw2Mumble.UI.IsMapOpen;
        private bool hidingInCombat => MumbleIsAvailable && _hideInCombat.Value && GameService.Gw2Mumble.PlayerCharacter.IsInCombat;

        #region Service Managers
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        #endregion

        [ImportingConstructor]
        public FishingBuddyModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void DefineSettings(SettingCollection settings)
        {
            // Fish Settings
            _ignoreCaughtFish = settings.DefineSetting("IgnoreCaughtFish", true, () => "Ignore Caught", () => "Ignore fish already counted towards achievements");
            _includeSaltwater = settings.DefineSetting("IncludeSaltwater", false, () => "Include Saltwater", () => "Include Saltwater Fisher fish");
            _includeWorldClass = settings.DefineSetting("IncludeWorldClass", false, () => "Include World Class", () => "Include World Class Fisher fish");
            _displayUncatchableFish = settings.DefineSetting("DisplayUncatchable", false, () => "Display Uncatchable", () => "Display fish that cannot be caught at this time of day");
            _fishPanelLoc = settings.DefineSetting("FishPanelLoc", new Point(160, 100), () => "Fish Location", () => "");
            _dragFishPanel = settings.DefineSetting("FishPanelDrag", false, () => "Drag Fish", () => "");
            _fishImgWidth = settings.DefineSetting("FishImgWidth", 30, () => "Fish Size", () => "");
            _showRarityBorder = settings.DefineSetting("ShowRarityBorder", true, () => "Show Rarity", () => "Display fish rarity border");
            _ignoreCaughtFish.SettingChanged += OnUpdateFishSettings;
            _includeSaltwater.SettingChanged += OnUpdateFishSettings;
            _includeWorldClass.SettingChanged += OnUpdateFishSettings;
            _displayUncatchableFish.SettingChanged += OnUpdateFishSettings;
            _fishPanelLoc.SettingChanged += OnUpdateSettings;
            _dragFishPanel.SettingChanged += OnUpdateSettings;
            _showRarityBorder.SettingChanged += OnUpdateFishSettings;
            _fishImgWidth.SettingChanged += OnUpdateSettings;
            _fishImgWidth.SetRange(16, 96);
            // Time of Day Settings
            _timeOfDayPanelLoc = settings.DefineSetting("TimeOfDayPanelLoc", new Point(100, 100), () => "Time of Day Details Location", () => "");
            _dragTimeOfDayClock = settings.DefineSetting("TimeOfDayPanelDrag", false, () => "Drag Time Display", () => "Drag time of day display");
            _timeOfDayImgWidth = settings.DefineSetting("TimeImgWidth", 64, () => "Time of Day Size", () => "");
            //_settingClockAlign = settings.DefineSetting("ClockTimeAlign", "Bottom", () => "Clock Position", () => "Clock display alignment");
            //TODO should this be _showTimeOfDay?
            _hideTimeOfDay = settings.DefineSetting("HideTimeOfDay", false, () => "Hide Time Display", () => "Opption to hide time display");
            _timeOfDayPanelLoc.SettingChanged += OnUpdateClockLocation;
            _dragTimeOfDayClock.SettingChanged += OnUpdateClockSettings;
            _timeOfDayImgWidth.SetRange(16, 96);
            _timeOfDayImgWidth.SettingChanged += OnUpdateClockSize;
            _dragTimeOfDayClock.SettingChanged += OnUpdateClockSettings;
            _hideTimeOfDay.SettingChanged += OnUpdateClockSettings;
            //_settingClockAlign.SettingChanged += OnUpdateClockLabel;
            // Common settings
            _hideInCombat = settings.DefineSetting("HideInCombat", false, () => "Hide In Combat", () => "Hide all fishing info in combat");
            _hideInCombat.SettingChanged += OnUpdateFishSettings;
        }

        protected override void Initialize()
        {
            Gw2ApiManager.SubtokenUpdated += OnApiSubTokenUpdated;
            catchableFish = new List<Fish>();
            fishingMaps = new FishingMaps();
            _mapRepository = new AsyncCache<int, Map>(RequestMap);
            _imgBorderBlack = ContentsManager.GetTexture(@"border_black.png");
            _imgBorderJunk = ContentsManager.GetTexture(@"border_junk.png");
            _imgBorderBasic = ContentsManager.GetTexture(@"border_basic.png");
            _imgBorderFine = ContentsManager.GetTexture(@"border_fine.png");
            _imgBorderMasterwork = ContentsManager.GetTexture(@"border_masterwork.png");
            _imgBorderRare = ContentsManager.GetTexture(@"border_rare.png");
            _imgBorderExotic = ContentsManager.GetTexture(@"border_exotic.png");
            _imgBorderAscended = ContentsManager.GetTexture(@"border_ascended.png");
            _imgBorderLegendary = ContentsManager.GetTexture(@"border_legendary.png");
            _imgBorderX = ContentsManager.GetTexture(@"border_x.png");
            _imgDawn = ContentsManager.GetTexture(@"dawn.png");
            _imgDay = ContentsManager.GetTexture(@"day.png");
            _imgDusk = ContentsManager.GetTexture(@"dusk.png");
            _imgNight = ContentsManager.GetTexture(@"night.png");

            _allFishList = new List<Fish>();

            // Load fish.json data
            using (StreamReader r = new StreamReader(ContentsManager.GetFileStream(@"fish.json")))
            {
                string json = r.ReadToEnd();
                _allFishList.AddRange(JsonConvert.DeserializeObject<List<Fish>>(json));
                Logger.Debug("fish list: " + string.Join(", ", _allFishList.Select(fish => fish.name)));
            }
            _useAPIToken = true;
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            // setup time of day clock
            _timeOfDayClock = new Clock();
            _timeOfDayClock.Parent = GameService.Graphics.SpriteScreen;
            OnUpdateClockSettings();
            OnUpdateClockLabel();
            OnUpdateClockLocation();
            OnUpdateClockSize();

            GetCurrentMapTime();
            GameService.Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            DrawIcons();
        }

        //TODO settings view
        //public override IView GetSettingsView()
        //{
        //    return new FishingBuddy.Views.SettingsView();
        //}

        //private double _runningTime;
        protected override void Update(GameTime gameTime)
        {
            // Refresh on 3 min timer
            //_runningTime += gameTime.ElapsedGameTime.TotalMilliseconds;
            //if (_runningTime > (3 * 60000))
            //{
            //    //Blish_HUD.Controls.ScreenNotification.ShowNotification("The examples module shows this message every 3 min!", Blish_HUD.Controls.ScreenNotification.NotificationType.Warning);
            //    await getCurrentMapsFish();
            //    DrawIcons();
            //    _runningTime -= (3 * 60000);
            //}

            if (uiIsAvailable && !hidingInCombat)
            {
                GetCurrentMapTime();
                if (!_hideTimeOfDay.Value) _timeOfDayClock.Show();
                _fishPanel.Show();
            }
            else
            {
                _timeOfDayClock.Hide();
                _fishPanel.Hide();
            }
            if (_draggingFishPanel)
            {
                var nOffset = InputService.Input.Mouse.Position - _dragFishPanelStart;
                _fishPanel.Location += nOffset;

                _dragFishPanelStart = InputService.Input.Mouse.Position;
            }
        }

        //TODO BUG! enable disable, leaves around images...

        /// <inheritdoc />
        protected override void Unload() // Unload here
        {
            // Fish Settings
            _ignoreCaughtFish.SettingChanged -= OnUpdateFishSettings;
            _includeSaltwater.SettingChanged -= OnUpdateFishSettings;
            _includeWorldClass.SettingChanged -= OnUpdateFishSettings;
            _displayUncatchableFish.SettingChanged -= OnUpdateFishSettings;
            _fishPanelLoc.SettingChanged -= OnUpdateSettings;
            _dragFishPanel.SettingChanged -= OnUpdateSettings;
            _showRarityBorder.SettingChanged -= OnUpdateFishSettings;
            _fishImgWidth.SettingChanged -= OnUpdateSettings;
            _fishPanel?.Dispose();
            // Time of Day Settings
            _timeOfDayPanelLoc.SettingChanged -= OnUpdateClockLocation;
            _dragTimeOfDayClock.SettingChanged -= OnUpdateClockSettings;
            _timeOfDayImgWidth.SettingChanged -= OnUpdateClockSize;
            //_settingClockAlign.SettingChanged -= OnUpdateClockLabel;
            _hideTimeOfDay.SettingChanged -= OnUpdateClockSettings;
            _timeOfDayClock?.Dispose();
            // Common settings
            _hideInCombat.SettingChanged -= OnUpdateFishSettings;

            GameService.Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;

            Gw2ApiManager.SubtokenUpdated -= OnApiSubTokenUpdated;

            _imgBorderBlack?.Dispose();
            _imgBorderJunk?.Dispose();
            _imgBorderBasic?.Dispose();
            _imgBorderFine?.Dispose();
            _imgBorderMasterwork?.Dispose();
            _imgBorderRare?.Dispose();
            _imgBorderExotic?.Dispose();
            _imgBorderAscended?.Dispose();
            _imgBorderLegendary?.Dispose();
            _imgBorderX?.Dispose();

            ModuleInstance?.Dispose(); ModuleInstance = null;
            // All static members must be manually unset
        }

        protected virtual void OnUpdateSettings(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            Logger.Debug("Settings updated");
            GetCurrentMapTime();
            DrawIcons();
        }
        protected virtual void OnUpdateSettings(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            Logger.Debug("Settings updated");
            GetCurrentMapTime();
            DrawIcons();
        }
        protected virtual void OnUpdateSettings(object sender = null, ValueChangedEventArgs<int> e = null)
        {
            Logger.Debug("Settings updated");
            GetCurrentMapTime();
            DrawIcons();
        }

        protected virtual async void OnUpdateFishSettings(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            Logger.Debug("Fish settings updated");
            GetCurrentMapTime();
            await getCurrentMapsFish();
            DrawIcons();
        }

        private void OnUpdateClockSettings(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            if (_hideTimeOfDay.Value)
            {
                _timeOfDayClock.Drag = false;
                _timeOfDayClock.Hide();
                return;
            } else
            {
                _timeOfDayClock.Show();
            }
            //TODO add show/hide clock label
            _timeOfDayClock.Drag = _dragTimeOfDayClock.Value;
        }

        //private void OnUpdateClockLabel(object sender = null, ValueChangedEventArgs<string> e = null)
        //{
        //    _timeOfDayClock.LabelAlign = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), _settingClockAlign.Value);
        //}

        private void OnUpdateClockLocation(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            // Offscreen reset
            if (_timeOfDayPanelLoc.Value.X < 0)
                _timeOfDayPanelLoc.Value = new Point(0, _timeOfDayPanelLoc.Value.Y);
            if (_timeOfDayPanelLoc.Value.Y < 0)
                _timeOfDayPanelLoc.Value = new Point(_timeOfDayPanelLoc.Value.X, 0);
            _timeOfDayClock.Location = _timeOfDayPanelLoc.Value;
        }

        private void OnUpdateClockSize(object sender = null, ValueChangedEventArgs<int> e = null)
        {
            _timeOfDayClock.Size = new Point(_timeOfDayImgWidth.Value);
        }

        protected void DrawIcons()
        {
            _fishPanel?.Dispose();

            int fishPanelRows = Clamp((int)Math.Ceiling((double)catchableFish.Count() / 2), 1, 7);
            int fishPanelColumns = Clamp((int)Math.Ceiling((double)catchableFish.Count() / fishPanelRows), 1, 7);
            // swap row column if necessary
            if (fishPanelRows < fishPanelColumns) { int swap = fishPanelRows; fishPanelRows = fishPanelColumns; fishPanelColumns = swap; }
            _fishPanel = new ClickThroughPanel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = _fishPanelLoc.Value,
                Size = new Point(fishPanelColumns * (_fishImgWidth.Value), fishPanelRows * (_fishImgWidth.Value)),
                capture = _dragFishPanel.Value
            };
            Logger.Debug($"Fish Panel Size; Rows: {fishPanelRows} Columns: {fishPanelColumns}, {_fishPanel.Size}");

            int x = 0; int y = 0; int count = 1;
            foreach (Fish fish in catchableFish)
            {
                string openWater = fish.openWater ? ", Open Water" : "";
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    Texture = fish.iconImg, //TODO can't use render service here... causes issues if module loaded and unloaded to quickly
                    Size = new Point(_fishImgWidth.Value),
                    Location = new Point(x, y),
                    ZIndex = 0,
                    Capture = _dragFishPanel.Value,
                    Opacity = (fish.Visible ? 1.0f : 0.5f)
                };
                if (_displayUncatchableFish.Value && !fish.Visible)
                {
                    new ClickThroughImage
                    {
                        Parent = _fishPanel,
                        Texture = _imgBorderX,
                        Size = new Point(_fishImgWidth.Value),
                        Location = new Point(x, y),
                        ZIndex = 1,
                        Capture = _dragFishPanel.Value
                    };
                }
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    Texture = _showRarityBorder.Value ? GetImageBorder(fish.rarity) : _imgBorderBlack,
                    Size = new Point(_fishImgWidth.Value),
                    Opacity = 0.8f,
                    Location = new Point(x, y),
                    BasicTooltipText = $"{fish.name}\n" +
                                       $"Fishing Hole: {fish.fishingHole}{openWater}\n" +
                                       $"Favored Bait: {fish.bait}\n" +
                                       $"Time of Day: {(fish.timeOfDay == Fish.TimeOfDay.DuskDawn ? "Dusk/Dawn" : fish.timeOfDay.ToString())}\n" +
                                       $"Achievement: {fish.achievement}\n" +
                                       $"Rarity: {fish.rarity}",
                    ZIndex = 2,
                    Capture = _dragFishPanel.Value
                };
                x += _fishImgWidth.Value;
                if (count == fishPanelColumns) { x = 0; y += _fishImgWidth.Value; count = 0; }
                count++;
            }

            if (_dragFishPanel.Value)
            {
                _fishPanel.capture = true;
                _fishPanel.LeftMouseButtonPressed += delegate
                {
                    _draggingFishPanel = true;
                    _dragFishPanelStart = InputService.Input.Mouse.Position;
                    _fishPanel.ShowTint = true;
                };
                _fishPanel.LeftMouseButtonReleased += delegate
                {
                    _draggingFishPanel = false;
                    _fishPanelLoc.Value = _fishPanel.Location;
                    _fishPanel.ShowTint = false;
                };
            }
        }

        private Texture2D GetImageBorder(string rarity)
        {
            switch (rarity)
            {
                case "Junk":
                    return _imgBorderJunk;
                case "Basic":
                    return _imgBorderBasic;
                case "Fine":
                    return _imgBorderFine;
                case "Masterwork":
                    return _imgBorderMasterwork;
                case "Rare":
                    return _imgBorderRare;
                case "Exotic":
                    return _imgBorderExotic;
                case "Ascended":
                    return _imgBorderAscended;
                case "Legendary":
                    return _imgBorderLegendary;
                default:
                    return _imgBorderBlack;
            }
        }

        public static int Clamp(int n, int min, int max)
        {
            if (n < min) return min;
            if (n > max) return max;
            return n;
        }

        private int _prevMapId;
        private async void OnMapChanged(object o, ValueEventArgs<int> e)
        {
            Logger.Debug("Map Changed");
            _currentMap = await _mapRepository.GetItem(e.Value);
            if (_currentMap == null || _currentMap.Id == _prevMapId) return;
            Logger.Debug($"Current map {_currentMap.Name} {_currentMap.Id}");
            _prevMapId = _currentMap.Id;
            GetCurrentMapTime();
            // TODO recalc & set TimeTilNextPhase on map change
            //_timeOfDayClock.TimeTilNextPhase = TyriaTime.CalcTimeTilNextPhase(GameService.Gw2Mumble.CurrentMap.Id);
            await getCurrentMapsFish();
            DrawIcons();
        }

        // TODO move to event thing so this gets called
        private async void TimeOfDayChanged()
        {
            await getCurrentMapsFish();
            DrawIcons();
        }

        // TODO move this to clock update
        // TODO execute on timer.CountDownFinished += () => {...do this stuff...}
        private void GetCurrentMapTime()
        {
            if (MumbleIsAvailable)
            {
                _timeOfDayClock.TimePhase = TyriaTime.CurrentMapPhase(GameService.Gw2Mumble.CurrentMap.Id);
            }
        }

        private async void OnApiSubTokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            if (Gw2ApiManager.HasPermissions(Gw2ApiManager.Permissions) == false)
            {
                Logger.Debug("API permissions are missing");
                _useAPIToken = false;
                return;
            }

            try
            {
                await getCurrentMapsFish();
                DrawIcons();
                _useAPIToken = true;
            }
            catch (Exception)
            {
                Logger.Debug("Failed to get info from api.");
            }
        }

        private async Task getCurrentMapsFish(CancellationToken cancellationToken = default)
        {
            await _updateFishSemaphore.WaitAsync(cancellationToken);
            try
            {
                try
                {
                    if (Gw2ApiManager.HasPermissions(Gw2ApiManager.Permissions))
                    {
                        // Get all account achievements
                        Gw2Sharp.WebApi.V2.IApiV2ObjectList<AccountAchievement> accountAchievements = await Gw2ApiManager.Gw2ApiClient.V2.Account.Achievements.GetAsync();
                        // Get just the not done fishing achievements
                        accountFishingAchievements = from achievement in accountAchievements where FishingMaps.FISHER_ACHIEVEMENT_IDS.Contains(achievement.Id) && (achievement.Current != achievement.Max) select achievement;
                        _useAPIToken = true;

                        // Extra info, probably remove this later
                        var currentAchievementIds = accountFishingAchievements.Select(achievement => achievement.Id);
                        var currentProgress = accountFishingAchievements.Select(achievement => achievement.Current);
                        var progressMax = accountFishingAchievements.Select(achievement => achievement.Max);
                        var currentOfMax = currentProgress.Zip(progressMax, (current, max) => current + "/" + max);
                        Logger.Debug("All account fishing achievement Ids: " + string.Join(", ", currentAchievementIds));
                        Logger.Debug("Account fishing achievement progress: " + string.Join(", ", currentOfMax));
                        // End Extra info
                    }
                    else
                    {
                        Logger.Debug("API permissions are missing");
                        _useAPIToken = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                    _useAPIToken = false;
                }

                // Refresh catchable fish
                catchableFish.Clear();
                // Achievement Ids from current map
                List<int> achievementsInMap = new List<int>();

                // Get player's current map if necessary
                if (_currentMap == null)
                {
                    try {
                        _currentMap = await _mapRepository.GetItem(GameService.Gw2Mumble.CurrentMap.Id);
                    } catch (Exception ex) {
                        Logger.Debug(ex, "Couldn't get player's current map.");
                    }
                }
                if (_currentMap != null && fishingMaps.mapAchievements.ContainsKey(_currentMap.Id)) {
                    achievementsInMap.AddRange(fishingMaps.mapAchievements[_currentMap.Id]);
                } else { Logger.Debug("Couldn't get player's current map, skipping current map fish."); }
                if (_includeSaltwater.Value) achievementsInMap.AddRange(FishingMaps.SaltwaterFisher);
                if (_includeWorldClass.Value) achievementsInMap.AddRange(FishingMaps.WorldClassFisher);
                if (achievementsInMap.Count == 0) { Logger.Debug("No achieveable fish in map."); return; }
                Logger.Debug($"All map achievements: {string.Join(", ", achievementsInMap)}");

                if (_ignoreCaughtFish.Value && _useAPIToken)
                {
                    var currentMapAchievable = from achievement in accountFishingAchievements where achievementsInMap.Contains(achievement.Id) select achievement;
                    Logger.Debug($"Current map achieveable: {string.Join(", ", currentMapAchievable.Select(achievement => $"id: {achievement.Id} current: {achievement.Current} done: {achievement.Done}"))}");
                    // Counter to help facilitate ignoring already caught fish
                    int bitsCounter = 0;
                    foreach (AccountAchievement accountAchievement in currentMapAchievable)
                    {
                        Achievement currentAccountAchievement = await RequestAchievement(accountAchievement.Id);
                        if (currentAccountAchievement == null) continue;
                        if (currentAccountAchievement.Bits == null) continue;
                        foreach (AchievementBit bit in currentAccountAchievement.Bits)
                        {
                            if (bit == null) { Logger.Debug($"Bit in {currentAccountAchievement.Id} is null"); continue; }
                            if (accountAchievement.Bits != null && accountAchievement.Bits.Contains(bitsCounter)) { bitsCounter++; continue; }
                            int itemId = ((AchievementItemBit)bit).Id;
                            Item fish = await RequestItem(itemId);
                            // Get first fish in all fish list that matches name
                            var fishNameMatch = _allFishList.Where(phish => phish.name == fish.Name);
                            Fish ghoti = fishNameMatch.Count() != 0 ? fishNameMatch.First() : null;
                            if (ghoti == null) { Logger.Debug($"Missing fish from all fish list: {fish.Name}"); continue; }
                            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
                            if (ghoti.timeOfDay != Fish.TimeOfDay.Any &&
                                !(_timeOfDayClock.TimePhase.Equals("Dawn") || _timeOfDayClock.TimePhase.Equals("Dusk")) &&
                                !Equals(ghoti.timeOfDay.ToString(), _timeOfDayClock.TimePhase))
                                 ghoti.Visible = false; 
                            else ghoti.Visible = true;
                            ghoti.icon = fish.Icon; ghoti.itemId = fish.Id; ghoti.achievementId = currentAccountAchievement.Id;
                            ghoti.iconImg = RequestItemIcon(fish);
                            catchableFish.Add(ghoti);
                            bitsCounter++;
                        }
                        bitsCounter = 0;
                    }
                }
                else
                {
                    var currentMapAchievableIds = from achievementId in FishingMaps.BASE_FISHER_ACHIEVEMENT_IDS where achievementsInMap.Contains(achievementId) select achievementId;
                    Logger.Debug($"Current map achieveable: {string.Join(", ", currentMapAchievableIds)}");
                    foreach (int achievementId in currentMapAchievableIds)
                    {
                        Achievement currentAchievement = await RequestAchievement(achievementId);
                        if (currentAchievement == null) continue;
                        foreach (AchievementBit bit in currentAchievement.Bits)
                        {
                            if (bit == null) { Logger.Debug($"Bit in {currentAchievement.Id} is null"); continue; }
                            int itemId = ((AchievementItemBit)bit).Id;
                            Item fish = await RequestItem(itemId);
                            Logger.Debug($"Found Fish {fish.Name} {fish.Id}");
                            // Get first fish in all fish list that matches name
                            var fishNameMatch = _allFishList.Where(phish => phish.name == fish.Name);
                            Fish ghoti = fishNameMatch.Count() != 0 ? fishNameMatch.First() : null;
                            if (ghoti == null) { Logger.Warn($"Missing fish from all fish list: {fish.Name}"); continue; }
                            // TODO option to fade (opacity) as well as remove
                            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
                            if (ghoti.timeOfDay != Fish.TimeOfDay.Any &&
                                !(_timeOfDayClock.TimePhase.Equals("Dawn") || _timeOfDayClock.TimePhase.Equals("Dusk")) &&
                                !Equals(ghoti.timeOfDay.ToString(), _timeOfDayClock.TimePhase))
                                ghoti.Visible = false;
                            else ghoti.Visible = true;
                            ghoti.icon = fish.Icon; ghoti.itemId = fish.Id; ghoti.achievementId = currentAchievement.Id;
                            ghoti.iconImg = RequestItemIcon(fish);
                            catchableFish.Add(ghoti);
                        }
                    }
                }
                if (!_displayUncatchableFish.Value) catchableFish = catchableFish.Where(phish => phish.Visible).ToList();
                Logger.Debug("Shown fish in current map count: " + catchableFish.Count());
            }
            catch (Exception ex) { Logger.Error(ex, "Unknown exception getting current map fish"); }
            finally { _updateFishSemaphore.Release(); }
        }

        // based on https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs
        private async Task<Map> RequestMap(int id)
        {
            Logger.Debug($"Requested map id: {id}");
            try
            {
                Task<Map> mapTask = Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id);
                await mapTask;
                return mapTask.Result;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
        }

        // TODO Add retry to Request...
        private async Task<Achievement> RequestAchievement(int id)
        {
            Logger.Debug($"Requested achievement id: {id}");
            // TODO instead of await each call. queue/addtolist each task, Task.WaitAll(queue/list), requeue nulls/failures/errors?
            try
            {
                Task<Achievement> achievementTask = Gw2ApiManager.Gw2ApiClient.V2.Achievements.GetAsync(id);
                await achievementTask;
                return achievementTask.Result;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
        }

        private async Task<Item> RequestItem(int id)
        {
            Logger.Debug($"Requested item id: {id}");
            try
            {
                Task<Item> itemTask = Gw2ApiManager.Gw2ApiClient.V2.Items.GetAsync(id);
                await itemTask;
                return itemTask.Result;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
        }

        // TODO find a way to wait on texture finished loading
        private AsyncTexture2D RequestItemIcon(Item item)
        {
            return GameService.Content.GetRenderServiceTexture(item.Icon);
        }
    }
}
