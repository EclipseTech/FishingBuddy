using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Eclipse1807.BlishHUD.FishingBuddy.Utils;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


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
        public static SettingEntry<int> _fishImgSize;
        public static SettingEntry<Point> _fishPanelLoc;
        public static readonly string[] _fishPanelOrientations = new string[] { "Vertical", "Horizontal" };
        public static SettingEntry<string> _fishPanelOrientation;
        public static readonly string[] _fishPanelDirections = new string[] { "Top-left", "Top-right", "Bottom-left", "Bottom-right" };
        public static SettingEntry<string> _fishPanelDirection;
        public static SettingEntry<bool> _dragTimeOfDayClock;
        public static SettingEntry<int> _timeOfDayImgSize;
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
        private bool UiIsAvailable => this.MumbleIsAvailable && !GameService.Gw2Mumble.UI.IsMapOpen;
        private bool HidingInCombat => this.MumbleIsAvailable && _hideInCombat.Value && GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
        //TODO make this random from 3-6 minutes to try to not overlap with other timers
        private Random rand = new Random();
        private double INTERVAL_UPDATE_FISH = 5 * 60 * 1000; // 5 minutes
        private double _lastUpdateFish = 0;

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
            _includeSaltwater = settings.DefineSetting("IncludeSaltwater", false, () => "Display Saltwater", () => "Include Saltwater Fisher fish");
            _includeWorldClass = settings.DefineSetting("IncludeWorldClass", false, () => "Display World Class", () => "Include World Class Fisher fish");
            _displayUncatchableFish = settings.DefineSetting("DisplayUncatchable", false, () => "Display Uncatchable", () => "Display fish that cannot be caught at this time of day");
            _fishPanelLoc = settings.DefineSetting("FishPanelLoc", new Point(160, 100), () => "Fish Panel Location", () => "");
            _dragFishPanel = settings.DefineSetting("FishPanelDrag", false, () => "Drag Fish", () => "");
            _fishImgSize = settings.DefineSetting("FishImgWidth", 30, () => "Fish Size", () => "");
            _showRarityBorder = settings.DefineSetting("ShowRarityBorder", true, () => "Show Rarity", () => "Display fish rarity border");
            _fishPanelOrientation = settings.DefineSetting("FishPanelOrientation", _fishPanelOrientations.First(), () => "Orientation", () => "Fish panel orientation");
            _fishPanelDirection = settings.DefineSetting("FishPanelDirection", _fishPanelDirections.First(), () => "Direction", () => "Fish panel Direction");
            _ignoreCaughtFish.SettingChanged += this.OnUpdateFishSettings;
            _includeSaltwater.SettingChanged += this.OnUpdateFishSettings;
            _includeWorldClass.SettingChanged += this.OnUpdateFishSettings;
            _displayUncatchableFish.SettingChanged += this.OnUpdateFishSettings;
            _fishPanelLoc.SettingChanged += this.OnUpdateSettings;
            _dragFishPanel.SettingChanged += this.OnUpdateSettings;
            _showRarityBorder.SettingChanged += this.OnUpdateFishSettings;
            _fishImgSize.SettingChanged += this.OnUpdateSettings;
            _fishImgSize.SetRange(16, 96);
            _fishPanelOrientation.SettingChanged += this.OnUpdateSettings;
            _fishPanelDirection.SettingChanged += this.OnUpdateSettings;
            // Time of Day Settings
            _timeOfDayPanelLoc = settings.DefineSetting("TimeOfDayPanelLoc", new Point(100, 100), () => "Time of Day Details Location", () => "");
            _dragTimeOfDayClock = settings.DefineSetting("TimeOfDayPanelDrag", false, () => "Drag Time Display", () => "Drag time of day display");
            _timeOfDayImgSize = settings.DefineSetting("TimeImgWidth", 64, () => "Time of Day Size", () => "");
            //_settingClockAlign = settings.DefineSetting("ClockTimeAlign", "Bottom", () => "Clock Position", () => "Clock display alignment");
            //TODO should this be _showTimeOfDay?
            _hideTimeOfDay = settings.DefineSetting("HideTimeOfDay", false, () => "Hide Time Display", () => "Opption to hide time display");
            _timeOfDayPanelLoc.SettingChanged += this.OnUpdateClockLocation;
            _dragTimeOfDayClock.SettingChanged += this.OnUpdateClockSettings;
            _timeOfDayImgSize.SetRange(16, 96);
            _timeOfDayImgSize.SettingChanged += this.OnUpdateClockSize;
            _dragTimeOfDayClock.SettingChanged += this.OnUpdateClockSettings;
            _hideTimeOfDay.SettingChanged += this.OnUpdateClockSettings;
            //_settingClockAlign.SettingChanged += OnUpdateClockLabel;
            // Common settings
            _hideInCombat = settings.DefineSetting("HideInCombat", false, () => "Hide In Combat", () => "Hide all fishing info in combat");
            _hideInCombat.SettingChanged += this.OnUpdateFishSettings;
        }

        protected override void Initialize()
        {
            this.Gw2ApiManager.SubtokenUpdated += this.OnApiSubTokenUpdated;
            this.catchableFish = new List<Fish>();
            this.fishingMaps = new FishingMaps();
            this._mapRepository = new AsyncCache<int, Map>(this.RequestMap);
            _imgBorderBlack = this.ContentsManager.GetTexture(@"border_black.png");
            _imgBorderJunk = this.ContentsManager.GetTexture(@"border_junk.png");
            _imgBorderBasic = this.ContentsManager.GetTexture(@"border_basic.png");
            _imgBorderFine = this.ContentsManager.GetTexture(@"border_fine.png");
            _imgBorderMasterwork = this.ContentsManager.GetTexture(@"border_masterwork.png");
            _imgBorderRare = this.ContentsManager.GetTexture(@"border_rare.png");
            _imgBorderExotic = this.ContentsManager.GetTexture(@"border_exotic.png");
            _imgBorderAscended = this.ContentsManager.GetTexture(@"border_ascended.png");
            _imgBorderLegendary = this.ContentsManager.GetTexture(@"border_legendary.png");
            _imgBorderX = this.ContentsManager.GetTexture(@"border_x.png");
            _imgDawn = this.ContentsManager.GetTexture(@"dawn.png");
            _imgDay = this.ContentsManager.GetTexture(@"day.png");
            _imgDusk = this.ContentsManager.GetTexture(@"dusk.png");
            _imgNight = this.ContentsManager.GetTexture(@"night.png");

            this._allFishList = new List<Fish>();

            // Load fish.json data
            using (StreamReader r = new StreamReader(this.ContentsManager.GetFileStream(@"fish.json")))
            {
                string json = r.ReadToEnd();
                this._allFishList.AddRange(JsonConvert.DeserializeObject<List<Fish>>(json));
                Logger.Debug("fish list: " + string.Join(", ", this._allFishList.Select(fish => fish.Name)));
            }
            this._useAPIToken = true;

            INTERVAL_UPDATE_FISH = rand.Next(3 * 60 * 1000, 6 * 60 * 1000);
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            // setup time of day clock
            this._timeOfDayClock = new Clock
            {
                Parent = GameService.Graphics.SpriteScreen
            };
            this.OnUpdateClockSettings();
            //OnUpdateClockLabel();
            this.OnUpdateClockLocation();
            this.OnUpdateClockSize();

            this.GetCurrentMapTime();
            GameService.Gw2Mumble.CurrentMap.MapChanged += this.OnMapChanged;
            this.DrawIcons();

            this._timeOfDayClock.TimeOfDayChanged += this.OnTimeOfDayChanged;
        }

        public override IView GetSettingsView() => new FishingBuddy.Views.SettingsView();

        //private double _runningTime;
        protected override void Update(GameTime gameTime)
        {
            // Refresh on 3 min timer
            //this._runningTime += gameTime.ElapsedGameTime.TotalSeconds;
            //if (this._runningTime > (3 * 60))
            //{
            //    Blish_HUD.Controls.ScreenNotification.ShowNotification("The examples module shows this message every 3 min!", Blish_HUD.Controls.ScreenNotification.NotificationType.Info);
            //    await this.getCurrentMapsFish();
            //    this.DrawIcons();
            //    this._runningTime -= (3 * 60);
            //}
            UpdateCadenceUtil.UpdateAsyncWithCadence(GetCurrentMapsFish, gameTime, INTERVAL_UPDATE_FISH, ref _lastUpdateFish);

            if (this.UiIsAvailable && !this.HidingInCombat)
            {
                this.GetCurrentMapTime();
                if (!_hideTimeOfDay.Value) this._timeOfDayClock.Show();
                _fishPanel.Show();
            }
            else
            {
                this._timeOfDayClock.Hide();
                _fishPanel.Hide();
            }
            if (this._draggingFishPanel)
            {
                Point nOffset = InputService.Input.Mouse.Position - this._dragFishPanelStart;
                _fishPanel.Location += nOffset;

                this._dragFishPanelStart = InputService.Input.Mouse.Position;
            }
        }

        /// <inheritdoc />
        protected override void Unload() // Unload here
        {
            // Fish Settings
            _ignoreCaughtFish.SettingChanged -= this.OnUpdateFishSettings;
            _includeSaltwater.SettingChanged -= this.OnUpdateFishSettings;
            _includeWorldClass.SettingChanged -= this.OnUpdateFishSettings;
            _displayUncatchableFish.SettingChanged -= this.OnUpdateFishSettings;
            _fishPanelLoc.SettingChanged -= this.OnUpdateSettings;
            _dragFishPanel.SettingChanged -= this.OnUpdateSettings;
            _showRarityBorder.SettingChanged -= this.OnUpdateFishSettings;
            _fishImgSize.SettingChanged -= this.OnUpdateSettings;
            _fishPanelOrientation.SettingChanged -= this.OnUpdateSettings;
            _fishPanelDirection.SettingChanged -= this.OnUpdateSettings;
            _fishPanel?.Dispose();
            // Time of Day Settings
            _timeOfDayPanelLoc.SettingChanged -= this.OnUpdateClockLocation;
            _dragTimeOfDayClock.SettingChanged -= this.OnUpdateClockSettings;
            _timeOfDayImgSize.SettingChanged -= this.OnUpdateClockSize;
            //_settingClockAlign.SettingChanged -= OnUpdateClockLabel;
            _hideTimeOfDay.SettingChanged -= this.OnUpdateClockSettings;
            this._timeOfDayClock?.Dispose();
            // Common settings
            _hideInCombat.SettingChanged -= this.OnUpdateFishSettings;

            GameService.Gw2Mumble.CurrentMap.MapChanged -= this.OnMapChanged;

            this.Gw2ApiManager.SubtokenUpdated -= this.OnApiSubTokenUpdated;

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

        protected virtual void OnUpdateSettings<T>(object sender = null, ValueChangedEventArgs<T> e = null)
        {
            Logger.Debug("Settings updated");
            this.GetCurrentMapTime();
            this.DrawIcons();
        }

        protected virtual async void OnUpdateFishSettings<T>(object sender = null, ValueChangedEventArgs<T> e = null)
        {
            Logger.Debug("Fish settings updated");
            this.GetCurrentMapTime();
            await this.GetCurrentMapsFish();
            this.DrawIcons();
        }

        private void OnUpdateClockSettings(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            if (_hideTimeOfDay.Value)
            {
                this._timeOfDayClock.Drag = false;
                this._timeOfDayClock.Hide();
                return;
            }
            else
            {
                this._timeOfDayClock.Show();
            }
            //TODO add show/hide clock label
            this._timeOfDayClock.Drag = _dragTimeOfDayClock.Value;
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
            this._timeOfDayClock.Location = _timeOfDayPanelLoc.Value;
        }

        private void OnUpdateClockSize(object sender = null, ValueChangedEventArgs<int> e = null) => this._timeOfDayClock.Size = new Point(_timeOfDayImgSize.Value);

        protected void DrawIcons()
        {
            _fishPanel?.Dispose();

            int fishPanelRows = Clamp((int)Math.Ceiling((double)this.catchableFish.Count() / 2), 1, 7);
            int fishPanelColumns = Clamp((int)Math.Ceiling((double)this.catchableFish.Count() / fishPanelRows), 1, 7);
            // swap row column if necessary
            if (fishPanelRows < fishPanelColumns) { int swap = fishPanelRows; fishPanelRows = fishPanelColumns; fishPanelColumns = swap; }
            if (Equals(_fishPanelOrientation.Value, "Horizontal")) { int swap = fishPanelRows; fishPanelRows = fishPanelColumns; fishPanelColumns = swap; }
            _fishPanel = new ClickThroughPanel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = _fishPanelLoc.Value,
                Size = new Point(_fishImgSize.Value * 7),
                Capture = _dragFishPanel.Value
            };
            Logger.Debug($"Fish Panel Size; Rows: {fishPanelRows} Columns: {fishPanelColumns}, {_fishPanel.Size}");

            // Top-left is default
            int x = 0; int y = 0; int count = 1; int xStart = x; int yStart = y;
            if (Equals(_fishPanelDirection.Value, "Top-right")) {
                x = _fishPanel.Size.X - _fishImgSize.Value; xStart = x;
            } else if (Equals(_fishPanelDirection.Value, "Bottom-left")) {
                y = _fishPanel.Size.Y - _fishImgSize.Value; yStart = y;
            } else if (Equals(_fishPanelDirection.Value, "Bottom-right")) {
                x = _fishPanel.Size.X - _fishImgSize.Value; xStart = x;
                y = _fishPanel.Size.Y - _fishImgSize.Value; yStart = y;
            }
            foreach (Fish fish in this.catchableFish)
            {
                string openWater = fish.OpenWater ? ", Open Water" : "";
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    //TODO BUG! enable disable module too quickly, leaves around images...
                    Texture = fish.IconImg, //TODO shouldn't use render service here... causes issues if module loaded and unloaded to quickly
                    Size = new Point(_fishImgSize.Value),
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
                        Size = new Point(_fishImgSize.Value),
                        Location = new Point(x, y),
                        ZIndex = 1,
                        Capture = _dragFishPanel.Value
                    };
                }
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    Texture = _showRarityBorder.Value ? this.GetImageBorder(fish.Rarity) : _imgBorderBlack,
                    Size = new Point(_fishImgSize.Value),
                    Opacity = 0.8f,
                    Location = new Point(x, y),
                    BasicTooltipText = $"{fish.Name}\n" +
                                       $"Fishing Hole: {fish.FishingHole}{openWater}\n" +
                                       $"Favored Bait: {fish.Bait}\n" +
                                       $"Time of Day: {(fish.Time == Fish.TimeOfDay.DuskDawn ? "Dusk/Dawn" : fish.Time.ToString())}\n" +
                                       $"Achievement: {fish.Achievement}\n" +
                                       $"Rarity: {fish.Rarity}",
                    ZIndex = 2,
                    Capture = _dragFishPanel.Value
                };
                // Build Left -> Right
                if (Equals(_fishPanelDirection.Value, "Top-left") || Equals(_fishPanelDirection.Value, "Bottom-left")) x += _fishImgSize.Value;
                // Build Right -> Left
                if (Equals(_fishPanelDirection.Value, "Top-right") || Equals(_fishPanelDirection.Value, "Bottom-right")) x -= _fishImgSize.Value;
                if (count == fishPanelColumns)
                {
                    x = xStart;
                    // Build Top -> Bottom
                    if (Equals(_fishPanelDirection.Value, "Top-left") || Equals(_fishPanelDirection.Value, "Top-right")) y += _fishImgSize.Value;
                    // Build Bottom -> Top
                    if (Equals(_fishPanelDirection.Value, "Bottom-left") || Equals(_fishPanelDirection.Value, "Bottom-right")) y -= _fishImgSize.Value;
                    count = 0;
                }
                count++;
            }

            if (_dragFishPanel.Value)
            {
                _fishPanel.Capture = true;
                _fishPanel.LeftMouseButtonPressed += delegate
                {
                    this._draggingFishPanel = true;
                    this._dragFishPanelStart = InputService.Input.Mouse.Position;
                    _fishPanel.ShowTint = true;
                };
                _fishPanel.LeftMouseButtonReleased += delegate
                {
                    this._draggingFishPanel = false;
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
            this._currentMap = await this._mapRepository.GetItem(e.Value);
            if (this._currentMap == null || this._currentMap.Id == this._prevMapId) return;
            Logger.Debug($"Current map {this._currentMap.Name} {this._currentMap.Id}");
            this._prevMapId = this._currentMap.Id;
            this.GetCurrentMapTime();
            // TODO recalc & set TimeTilNextPhase on map change
            //_timeOfDayClock.TimeTilNextPhase = TyriaTime.CalcTimeTilNextPhase(GameService.Gw2Mumble.CurrentMap.Id);
            await this.GetCurrentMapsFish();
            this.DrawIcons();
        }

        private async void OnTimeOfDayChanged(object sender = null, ValueChangedEventArgs<string> e = null)
        {
            await this.GetCurrentMapsFish();
            this.DrawIcons();
            // reset update timer
            _lastUpdateFish = 0;
            INTERVAL_UPDATE_FISH = rand.Next(3 * 60 * 1000, 6 * 60 * 1000);
        }

        // TODO move this to clock update
        // TODO execute on timer.CountDownFinished += () => {...do this stuff...}
        private void GetCurrentMapTime()
        {
            if (this.MumbleIsAvailable)
            {
                this._timeOfDayClock.TimePhase = TyriaTime.CurrentMapPhase(GameService.Gw2Mumble.CurrentMap.Id);
            }
        }

        private async void OnApiSubTokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            if (this.Gw2ApiManager.HasPermissions(this.Gw2ApiManager.Permissions) == false)
            {
                Logger.Debug("API permissions are missing");
                this._useAPIToken = false;
                return;
            }

            try
            {
                await this.GetCurrentMapsFish();
                this.DrawIcons();
                this._useAPIToken = true;
            }
            catch (Exception)
            {
                Logger.Debug("Failed to get info from api.");
            }
        }

        // TODO probably move this to Fish or FishingMaps?
        private async Task GetCurrentMapsFish(GameTime gameTime)
        {
            await GetCurrentMapsFish();
            this.DrawIcons();
        }
        private async Task GetCurrentMapsFish(CancellationToken cancellationToken = default)
        {
            await this._updateFishSemaphore.WaitAsync(cancellationToken);
            try
            {
                try
                {
                    if (this.Gw2ApiManager.HasPermissions(this.Gw2ApiManager.Permissions))
                    {
                        // Get all account achievements
                        Gw2Sharp.WebApi.V2.IApiV2ObjectList<AccountAchievement> accountAchievements = await this.Gw2ApiManager.Gw2ApiClient.V2.Account.Achievements.GetAsync();
                        // Get just the not done fishing achievements
                        this.accountFishingAchievements = from achievement in accountAchievements where FishingMaps.FISHER_ACHIEVEMENT_IDS.Contains(achievement.Id) && (achievement.Current != achievement.Max) select achievement;
                        this._useAPIToken = true;

                        // Extra info, probably remove this later
                        var currentAchievementIds = this.accountFishingAchievements.Select(achievement => achievement.Id);
                        var currentProgress = this.accountFishingAchievements.Select(achievement => achievement.Current);
                        var progressMax = this.accountFishingAchievements.Select(achievement => achievement.Max);
                        var currentOfMax = currentProgress.Zip(progressMax, (current, max) => current + "/" + max);
                        Logger.Debug("All account fishing achievement Ids: " + string.Join(", ", currentAchievementIds));
                        Logger.Debug("Account fishing achievement progress: " + string.Join(", ", currentOfMax));
                        // End Extra info
                    }
                    else
                    {
                        Logger.Debug("API permissions are missing");
                        this._useAPIToken = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                    this._useAPIToken = false;
                }

                // Refresh catchable fish
                this.catchableFish.Clear();
                // Achievement Ids from current map
                List<int> achievementsInMap = new List<int>();

                // Get player's current map if necessary
                if (this._currentMap == null)
                {
                    try
                    {
                        this._currentMap = await this._mapRepository.GetItem(GameService.Gw2Mumble.CurrentMap.Id);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug(ex, "Couldn't get player's current map.");
                    }
                }
                if (this._currentMap != null && this.fishingMaps.mapAchievements.ContainsKey(this._currentMap.Id))
                {
                    achievementsInMap.AddRange(this.fishingMaps.mapAchievements[this._currentMap.Id]);
                }
                else { Logger.Debug("Couldn't get player's current map, skipping current map fish."); }
                if (_includeSaltwater.Value) achievementsInMap.AddRange(FishingMaps.SaltwaterFisher);
                if (_includeWorldClass.Value) achievementsInMap.AddRange(FishingMaps.WorldClassFisher);
                if (achievementsInMap.Count == 0) { Logger.Debug("No achieveable fish in map."); return; }
                Logger.Debug($"All map achievements: {string.Join(", ", achievementsInMap)}");

                if (_ignoreCaughtFish.Value && this._useAPIToken)
                {
                    var currentMapAchievable = from achievement in this.accountFishingAchievements where achievementsInMap.Contains(achievement.Id) select achievement;
                    Logger.Debug($"Current map achieveable: {string.Join(", ", currentMapAchievable.Select(achievement => $"id: {achievement.Id} current: {achievement.Current} done: {achievement.Done}"))}");
                    // Counter to help facilitate ignoring already caught fish
                    int bitsCounter = 0;
                    foreach (AccountAchievement accountAchievement in currentMapAchievable)
                    {
                        Achievement currentAccountAchievement = await this.RequestAchievement(accountAchievement.Id);
                        if (currentAccountAchievement == null) continue;
                        if (currentAccountAchievement.Bits == null) continue;
                        foreach (AchievementBit bit in currentAccountAchievement.Bits)
                        {
                            if (bit == null) { Logger.Debug($"Bit in {currentAccountAchievement.Id} is null"); continue; }
                            if (accountAchievement.Bits != null && accountAchievement.Bits.Contains(bitsCounter)) { bitsCounter++; continue; }
                            int itemId = ((AchievementItemBit)bit).Id;
                            Item fish = await this.RequestItem(itemId);
                            // Get first fish in all fish list that matches name
                            var fishNameMatch = this._allFishList.Where(phish => phish.Name == fish.Name);
                            Fish ghoti = fishNameMatch.Count() != 0 ? fishNameMatch.First() : null;
                            if (ghoti == null) { Logger.Debug($"Missing fish from all fish list: {fish.Name}"); continue; }
                            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
                            if (ghoti.Time != Fish.TimeOfDay.Any &&
                                !(this._timeOfDayClock.TimePhase.Equals("Dawn") || this._timeOfDayClock.TimePhase.Equals("Dusk")) &&
                                !Equals(ghoti.Time.ToString(), this._timeOfDayClock.TimePhase))
                                ghoti.Visible = false;
                            else ghoti.Visible = true;
                            ghoti.Icon = fish.Icon; ghoti.ItemId = fish.Id; ghoti.AchievementId = currentAccountAchievement.Id;
                            ghoti.IconImg = this.RequestItemIcon(fish);
                            this.catchableFish.Add(ghoti);
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
                        Achievement currentAchievement = await this.RequestAchievement(achievementId);
                        if (currentAchievement == null) continue;
                        foreach (AchievementBit bit in currentAchievement.Bits)
                        {
                            if (bit == null) { Logger.Debug($"Bit in {currentAchievement.Id} is null"); continue; }
                            int itemId = ((AchievementItemBit)bit).Id;
                            Item fish = await this.RequestItem(itemId);
                            Logger.Debug($"Found Fish {fish.Name} {fish.Id}");
                            // Get first fish in all fish list that matches name
                            var fishNameMatch = this._allFishList.Where(phish => phish.Name == fish.Name);
                            Fish ghoti = fishNameMatch.Count() != 0 ? fishNameMatch.First() : null;
                            if (ghoti == null) { Logger.Warn($"Missing fish from all fish list: {fish.Name}"); continue; }
                            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
                            if (ghoti.Time != Fish.TimeOfDay.Any &&
                                !(this._timeOfDayClock.TimePhase.Equals("Dawn") || this._timeOfDayClock.TimePhase.Equals("Dusk")) &&
                                !Equals(ghoti.Time.ToString(), this._timeOfDayClock.TimePhase))
                                ghoti.Visible = false;
                            else ghoti.Visible = true;
                            ghoti.Icon = fish.Icon; ghoti.ItemId = fish.Id; ghoti.AchievementId = currentAchievement.Id;
                            ghoti.IconImg = this.RequestItemIcon(fish);
                            this.catchableFish.Add(ghoti);
                        }
                    }
                }
                if (!_displayUncatchableFish.Value) this.catchableFish = this.catchableFish.Where(phish => phish.Visible).ToList();
                Logger.Debug("Shown fish in current map count: " + this.catchableFish.Count());
            }
            catch (Exception ex) { Logger.Error(ex, "Unknown exception getting current map fish"); }
            finally { this._updateFishSemaphore.Release(); }
        }

        // based on https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs
        private async Task<Map> RequestMap(int id)
        {
            Logger.Debug($"Requested map id: {id}");
            try
            {
                Task<Map> mapTask = this.Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id);
                await mapTask;
                return mapTask.Result;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
        }

        // TODO Add retry to Request(s)...
        private async Task<Achievement> RequestAchievement(int id)
        {
            Logger.Debug($"Requested achievement id: {id}");
            // TODO instead of await each call. queue/addtolist each task, Task.WaitAll(queue/list), requeue nulls/failures/errors?
            try
            {
                Task<Achievement> achievementTask = this.Gw2ApiManager.Gw2ApiClient.V2.Achievements.GetAsync(id);
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
                Task<Item> itemTask = this.Gw2ApiManager.Gw2ApiClient.V2.Items.GetAsync(id);
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
        private AsyncTexture2D RequestItemIcon(Item item) => GameService.Content.GetRenderServiceTexture(item.Icon);
    }
}
