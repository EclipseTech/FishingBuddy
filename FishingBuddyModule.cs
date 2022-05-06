using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
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


// TODO should other map Ids not show any fishing info, or show open water info? or saltwater/world class info? hide in instances?
// TODO cache fishing images from api, save / download icons to directory cache & get from cache before web, Download / Use Icon w/ GetRenderServiceTexture ex: GameService.Content.GetRenderServiceTexture(fish.Icon);
// TODO should be caching map info too
// TODO in bounds checking for UI elements, ex: https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L64 & https://github.com/manlaan/BlishHud-Clock/blob/main/Module.cs#L145
// TODO notifications? on dawn https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs#L108 ?15 sec til?
// TODO (inventory permissions required) Add caught fish counter (count per rarity & ? count per type of fish ? per zone ? per session ? per hour ?)
// TODO BLOCKED get/display equipped lure & bait w/ #s (optional w/ mouseover info)
// TODO BLOCKED bait & lure icons via api... get bait & lure type/count from api? is this even detailed anywhere? or no api yet for this?


namespace Eclipse1807.BlishHUD.FishingBuddy
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class FishingBuddyModule : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(FishingBuddyModule));

        internal static FishingBuddyModule ModuleInstance;

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
        private static Texture2D _imgBorderXY;
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
        public static SettingEntry<string> _fishPanelTooltipDisplay;
        public static SettingEntry<bool> _dragTimeOfDayClock;
        public static SettingEntry<int> _timeOfDayImgSize;
        public static SettingEntry<Point> _timeOfDayPanelLoc;
        public static SettingEntry<bool> _ignoreCaughtFish;
        public static SettingEntry<bool> _includeWorldClass;
        public static SettingEntry<bool> _includeSaltwater;
        public static SettingEntry<bool> _displayUncatchableFish;
        public static SettingEntry<bool> _hideTimeOfDay;
        public static SettingEntry<bool> _settingClockLabel;
        public static SettingEntry<bool> _hideInCombat;
        private List<Fish> catchableFish;
        private FishingMaps _fishingMaps;
        private IEnumerable<AccountAchievement> accountFishingAchievements;
        public static SettingEntry<bool> _showRarityBorder;
        public static readonly string[] _verticalAlignmentOptions = new string[] { "Top", "Middle", "Bottom" };
        public static SettingEntry<string> _settingClockAlign;
        private Clock _timeOfDayClock;

        private List<Fish> _allFishList;
        internal static Map _currentMap;
        private bool _useAPIToken;
        private readonly SemaphoreSlim _updateFishSemaphore = new SemaphoreSlim(1, 1);
        private bool MumbleIsAvailable => GameService.Gw2Mumble.IsAvailable && GameService.GameIntegration.Gw2Instance.IsInGame;
        private bool UiIsAvailable => this.MumbleIsAvailable && !GameService.Gw2Mumble.UI.IsMapOpen;
        private bool HidingInCombat => this.MumbleIsAvailable && _hideInCombat.Value && GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
        // 5 minutes API update interval
        private readonly double INTERVAL_UPDATE_FISH = 5 * 60 * 1000;
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
            _fishPanelTooltipDisplay = settings.DefineSetting("FishPanelTooltipDisplay", "#1\n@2\n@3\n@4\n@5\n@6\n@7@8", () => "Tooltip Display", () =>
                                                              "Default: #1\\n@2\\n@3\\n@4\\n@5\\n@6\\n@7@8\n" +
                                                              "Simple: #1\\nBait: #2\\nHole: #4\\n@7\n" +
                                                              "Compact: #1 [#6]\\nBait: #2\\nHole: #4 (#3)\\n@7\n" +
                                                              "@number uses default string, #number allows for more customization\n" +
                                                              "@#1: Name\n" +
                                                              "@#2: Favored Bait\n" +
                                                              "@#3: Time of Day\n" +
                                                              "@#4: Fishing Hole\n" +
                                                              "@#5: Achievement\n" +
                                                              "@#6: Rarity\n" +
                                                              "@7:  Reason for Hiding\n" +
                                                              "@#8: Fishy Notes\n" +
                                                              "(\\n adds new line)");
            _fishPanelTooltipDisplay.SettingChanged += this.OnUpdateSettings;
            // Time of Day Settings
            _timeOfDayPanelLoc = settings.DefineSetting("TimeOfDayPanelLoc", new Point(100, 100), () => "Time of Day Details Location", () => "");
            _dragTimeOfDayClock = settings.DefineSetting("TimeOfDayPanelDrag", false, () => "Drag Time Display", () => "Drag time of day display");
            _timeOfDayImgSize = settings.DefineSetting("TimeImgWidth", 64, () => "Time of Day Size", () => "");
            _settingClockLabel = settings.DefineSetting("ClockLabel", false, () => "Hide Time Label", () => "Show/Hide clock time label display");
            _settingClockAlign = settings.DefineSetting("TimeLabelAlign", "Bottom", () => "Time Label Position", () => "Clock display label alignment");
            _hideTimeOfDay = settings.DefineSetting("HideTimeOfDay", false, () => "Hide Time Display", () => "Option to hide time display");
            _timeOfDayPanelLoc.SettingChanged += this.OnUpdateClockLocation;
            _dragTimeOfDayClock.SettingChanged += this.OnUpdateClockSettings;
            _timeOfDayImgSize.SetRange(16, 96);
            _timeOfDayImgSize.SettingChanged += this.OnUpdateClockSize;
            _dragTimeOfDayClock.SettingChanged += this.OnUpdateClockSettings;
            _hideTimeOfDay.SettingChanged += this.OnUpdateClockSettings;
            _settingClockAlign.SettingChanged += this.OnUpdateClockLabelAlign;
            _settingClockLabel.SettingChanged += this.OnUpdateHideClockLabel;
            // Common settings
            _hideInCombat = settings.DefineSetting("HideInCombat", false, () => "Hide In Combat", () => "Hide all fishing info in combat");
            _hideInCombat.SettingChanged += this.OnUpdateFishSettings;
        }

        protected override void Initialize()
        {
            this.Gw2ApiManager.SubtokenUpdated += this.OnApiSubTokenUpdated;
            this.catchableFish = new List<Fish>();
            this._fishingMaps = new FishingMaps();
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
            _imgBorderXY = this.ContentsManager.GetTexture(@"border_xy.png");
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
                Logger.Debug("Fish list: " + string.Join(", ", this._allFishList.Select(fish => fish.Name)));
            }
            this._useAPIToken = true;
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            // setup time of day clock
            this._timeOfDayClock = new Clock
            {
                Parent = GameService.Graphics.SpriteScreen,
                Visible = _hideTimeOfDay.Value,
                Location = _timeOfDayPanelLoc.Value,
                Size = new Point(_timeOfDayImgSize.Value, _timeOfDayImgSize.Value+40),
                Drag = _dragTimeOfDayClock.Value,
                HideLabel = _settingClockLabel.Value,
                LabelVerticalAlignment = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), _settingClockAlign.Value),
            };

            GameService.Gw2Mumble.CurrentMap.MapChanged += this.OnMapChanged;
            this.DrawIcons();

            this._timeOfDayClock.TimeOfDayChanged += this.OnTimeOfDayChanged;
        }

        public override IView GetSettingsView() => new FishingBuddy.Views.SettingsView();

        protected override void Update(GameTime gameTime)
        {
            // TODO display optional notification w/ options set time before time of day change (ie "15 seconds til Dawn")
            // Blish_HUD.Controls.ScreenNotification.ShowNotification("The examples module shows this message every 3 min!", Blish_HUD.Controls.ScreenNotification.NotificationType.Info);

            // Update Account Achievements periodically
            UpdateCadenceUtil.UpdateAsyncWithCadence(this.GetCurrentMapsFish, gameTime, this.INTERVAL_UPDATE_FISH, ref this._lastUpdateFish);

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
            _fishPanelTooltipDisplay.SettingChanged -= this.OnUpdateSettings;
            _fishPanel?.Dispose();
            // Time of Day Settings
            _timeOfDayPanelLoc.SettingChanged -= this.OnUpdateClockLocation;
            _dragTimeOfDayClock.SettingChanged -= this.OnUpdateClockSettings;
            _timeOfDayImgSize.SettingChanged -= this.OnUpdateClockSize;
            _settingClockAlign.SettingChanged -= this.OnUpdateClockLabelAlign;
            _settingClockLabel.SettingChanged -= this.OnUpdateHideClockLabel;
            _hideTimeOfDay.SettingChanged -= this.OnUpdateClockSettings;
            this._timeOfDayClock?.Dispose();
            // Common settings
            _hideInCombat.SettingChanged -= this.OnUpdateFishSettings;

            GameService.Gw2Mumble.CurrentMap.MapChanged -= this.OnMapChanged;
            this._timeOfDayClock.TimeOfDayChanged -= this.OnTimeOfDayChanged;

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
            _imgBorderXY?.Dispose();

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
            this._timeOfDayClock.Drag = _dragTimeOfDayClock.Value;
        }

        private void OnUpdateClockLabelAlign(object sender = null, ValueChangedEventArgs<string> e = null)
            => this._timeOfDayClock.LabelVerticalAlignment = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), _settingClockAlign.Value);

        private void OnUpdateHideClockLabel(object sender = null, ValueChangedEventArgs<bool> e = null)
            => this._timeOfDayClock.HideLabel = _settingClockLabel.Value;


        private void OnUpdateClockLocation(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            // Offscreen reset
            if (_timeOfDayPanelLoc.Value.X < 0)
                _timeOfDayPanelLoc.Value = new Point(0, _timeOfDayPanelLoc.Value.Y);
            if (_timeOfDayPanelLoc.Value.Y < 0)
                _timeOfDayPanelLoc.Value = new Point(_timeOfDayPanelLoc.Value.X, 0);
            this._timeOfDayClock.Location = _timeOfDayPanelLoc.Value;
        }

        private void OnUpdateClockSize(object sender = null, ValueChangedEventArgs<int> e = null)
            => this._timeOfDayClock.Size = new Point(_timeOfDayImgSize.Value);

        protected void DrawIcons()
        {
            _fishPanel?.Dispose();

            int fishPanelRows = Clamp((int)Math.Ceiling((double)this.catchableFish.Count() / 2), 1, 7);
            int fishPanelColumns = Clamp((int)Math.Ceiling((double)this.catchableFish.Count() / fishPanelRows), 1, 7);
            // swap row column if necessary
            if (fishPanelRows < fishPanelColumns) { int swap = fishPanelRows; fishPanelRows = fishPanelColumns; fishPanelColumns = swap; }
            if (Equals(_fishPanelOrientation.Value, "Horizontal")) { int swap = fishPanelRows; fishPanelRows = fishPanelColumns; fishPanelColumns = swap; }
            //TODO make this a new container type for sizing/resizing easier... ?similar to FlowPanel?
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
            //TODO give setting options to sort fish???
            // For now sort uncaught -> caught -> uncatchable -> rarity -> name
            // Thoughts: is there an order panel / drag drop type?
            // ! descending order @ ascending order
            // "1": Uncaught; "2": Caught; "3": Rarity; "4" Name; bait/time/hole/???
            // default: !1@2@3@4
            var catchable = this.catchableFish.OrderByDescending(f => f.Visible).
                                               ThenBy(f => f.Caught && f.Visible).
                                               ThenBy(f => f.Rarity).
                                               ThenBy(f => f.Name);
            foreach (Fish fish in catchable)
            {
                string fishTooltip = this.BuildTooltip(fish);
                // Fish image
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    // TODO BUG! enable -> disable module too quickly, leaves around images...
                    // Workaround: re-enable module, wait til load, then disables
                    // Seems to be caused by using render service here... causes issues if module loaded and unloaded to quickly
                    // AnycTexture2D delayed render
                    Texture = fish.IconImg,
                    Size = new Point(_fishImgSize.Value),
                    Location = new Point(x, y),
                    ZIndex = 0,
                    Capture = _dragFishPanel.Value,
                    Opacity = ((fish.Visible && !fish.Caught) ? 1.0f : 0.5f)
                };
                // TODO maybe give Opacity Setting options dropdown of "Hide", 25%, 50%, 75%, 100%, X
                // Display Uncaught
                if (!_ignoreCaughtFish.Value && fish.Caught)
                {
                    new ClickThroughImage
                    {
                        Parent = _fishPanel,
                        Texture = _imgBorderXY,
                        Size = new Point(_fishImgSize.Value),
                        Location = new Point(x, y),
                        ZIndex = 1,
                        Capture = _dragFishPanel.Value,
                        Opacity = 0.75f
                    };
                }
                // TODO maybe give Opacity Setting options dropdown of "Hide", 25%, 50%, 75%, 100%, X
                // Display Uncatchables (time)
                if (_displayUncatchableFish.Value && !fish.Visible)
                {
                    new ClickThroughImage
                    {
                        Parent = _fishPanel,
                        Texture = _imgBorderX,
                        Size = new Point(_fishImgSize.Value),
                        Location = new Point(x, y),
                        ZIndex = 2,
                        Capture = _dragFishPanel.Value,
                        Opacity = 1.0f
                    };
                }
                // Rarity border (shows tooltip as this is the top and always exists)
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    Texture = _showRarityBorder.Value ? this.GetImageBorder(fish.Rarity) : _imgBorderBlack,
                    Size = new Point(_fishImgSize.Value),
                    Opacity = 0.8f,
                    Location = new Point(x, y),
                    BasicTooltipText = fishTooltip,
                    ZIndex = 3,
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

        private string BuildTooltip(Fish fish)
        {
            string name = $"Name: {fish.Name}";
            string bait = $"Favored Bait: {fish.Bait.GetEnumMemberValue()}";
            string time = $"Time of Day: {fish.Time.GetEnumMemberValue()}";
            string hole = $"Fishing Hole: {fish.FishingHole}{(fish.OpenWater ? $", Open Water" : "")}";
            string achieve = $"Achievement: {fish.Achievement}";
            string rarity = $"Rarity: {fish.Rarity}";
            string hiddenReason = "";
            if (this._useAPIToken)
            {
                if (!fish.Visible && fish.Caught) hiddenReason = "Hidden: Time of Day, Already Caught";
                else if (!fish.Visible) hiddenReason = "Hidden: Time of Day";
                else if (fish.Caught) hiddenReason = "Hidden: Already Caught";

            }
            string notes = !string.IsNullOrWhiteSpace(fish.Notes) ? $"Notes: {fish.Notes}" : "";
            string tooltip = _fishPanelTooltipDisplay.Value;
            // Standard replacements
            tooltip = tooltip.Replace("@1", name);
            tooltip = tooltip.Replace("@2", bait);
            tooltip = tooltip.Replace("@3", time);
            tooltip = tooltip.Replace("@4", hole);
            tooltip = tooltip.Replace("@5", achieve);
            tooltip = tooltip.Replace("@6", rarity);
            tooltip = tooltip.Replace("@7", hiddenReason);
            tooltip = tooltip.Replace("@8", notes);
            // Create your own tooltip (not documented)
            tooltip = tooltip.Replace("#1", fish.Name);
            tooltip = tooltip.Replace("#2", fish.Bait.GetEnumMemberValue());
            tooltip = tooltip.Replace("#3", fish.Time.GetEnumMemberValue());
            tooltip = tooltip.Replace("#4", $"{fish.FishingHole}{(fish.OpenWater ? $", Open Water" : "")}");
            tooltip = tooltip.Replace("#5", fish.Achievement);
            tooltip = tooltip.Replace("#6", fish.Rarity.ToString());
            tooltip = tooltip.Replace("#8", fish.Notes);
            // Newline string replacement
            tooltip = tooltip.Replace("\\n", "\n");
            // Clean up double newlines
            tooltip = tooltip.Replace("\n\n", "\n");
            return tooltip.Trim();
        }

        private Texture2D GetImageBorder(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Junk => _imgBorderJunk,
                ItemRarity.Basic => _imgBorderBasic,
                ItemRarity.Fine => _imgBorderFine,
                ItemRarity.Masterwork => _imgBorderMasterwork,
                ItemRarity.Rare => _imgBorderRare,
                ItemRarity.Exotic => _imgBorderExotic,
                ItemRarity.Ascended => _imgBorderAscended,
                ItemRarity.Legendary => _imgBorderLegendary,
                _ => _imgBorderBlack,
            };
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
            _currentMap = await this._mapRepository.GetItem(e.Value);
            if (_currentMap is null || _currentMap.Id == this._prevMapId) return;
            Logger.Debug($"Current map {_currentMap.Name} {_currentMap.Id}");
            this._prevMapId = _currentMap.Id;
            this.GetCurrentMapTime();
            await this.GetCurrentMapsFish();
            this.DrawIcons();
        }

        private async void OnTimeOfDayChanged(object sender = null, ValueChangedEventArgs<string> e = null)
        {
            await this.GetCurrentMapsFish();
            this.DrawIcons();
            // reset update timer
            this._lastUpdateFish = 0;
        }

        private void GetCurrentMapTime()
        {
            if (this.MumbleIsAvailable && _currentMap != null)
            {
                this._timeOfDayClock.TimePhase = TyriaTime.CurrentMapPhase(_currentMap);
            }
            else { this._timeOfDayClock.Hide(); }
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

        // TODO maybe move this to Fish or FishingMaps?
        private async Task GetCurrentMapsFish(GameTime gameTime)
        {
            await this.GetCurrentMapsFish();
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
                List<int> verifyMapAchievable = new List<int>();

                // Get player's current map if necessary
                if (_currentMap is null)
                {
                    try
                    {
                        _currentMap = await this._mapRepository.GetItem(GameService.Gw2Mumble.CurrentMap.Id);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug(ex, "Couldn't get player's current map.");
                    }
                }
                if (_currentMap != null && this._fishingMaps.MapAchievements.ContainsKey(_currentMap.Id))
                {
                    achievementsInMap.AddRange(this._fishingMaps.MapAchievements[_currentMap.Id]);
                    verifyMapAchievable.AddRange(this._fishingMaps.MapAchievements[_currentMap.Id]);
                }
                else { Logger.Debug("Couldn't get player's current map, skipping current map fish."); }
                if (_includeSaltwater.Value) achievementsInMap.AddRange(FishingMaps.SaltwaterFisher);
                if (_includeWorldClass.Value) achievementsInMap.AddRange(FishingMaps.WorldClassFisher);
                if (achievementsInMap.Count == 0) { Logger.Debug("No achievable fish in map."); return; }
                Logger.Debug($"All map achievements: {string.Join(", ", achievementsInMap)}");

                if (this._useAPIToken)
                {
                    Logger.Debug("Using API");
                    var currentMapAchievable = (from achievement in this.accountFishingAchievements where achievementsInMap.Contains(achievement.Id) select achievement).ToList();
                    // Add achievements that have 0 fish caught in zone when required. Account achievements don't count 0 as progress on account.
                    if (!currentMapAchievable.Any(a => verifyMapAchievable.Contains(a.Id))) {
                        // NOTE: this might not be enough of a fix for certain special cases (Thousand Seas Pavillion & other multi map if 0 in multiple maps)
                        currentMapAchievable.Add(new AccountAchievement { Id = this._fishingMaps.MapAchievements[_currentMap.Id].First(), Current = 0, Done = false });
                    }
                    if (_includeSaltwater.Value && !currentMapAchievable.Any(a => FishingMaps.SaltwaterFisher.Contains(a.Id))) {
                        currentMapAchievable.Add(new AccountAchievement { Id = FishingMaps.SaltwaterFisher.First(), Current = 0, Done = false });
                    }
                    if (_includeWorldClass.Value && !currentMapAchievable.Any(a => FishingMaps.WorldClassFisher.Contains(a.Id))) {
                        currentMapAchievable.Add(new AccountAchievement { Id = FishingMaps.WorldClassFisher.First(), Current = 0, Done = false });
                    }
                    Logger.Debug($"Current map achievable: {string.Join(", ", currentMapAchievable.Select(achievement => $"id: {achievement.Id} current: {achievement.Current} done: {achievement.Done}"))}");
                    // Counter to help facilitate ignoring already caught fish
                    int bitsCounter = 0;
                    foreach (AccountAchievement accountAchievement in currentMapAchievable)
                    {
                        Achievement currentAccountAchievement = await this.RequestAchievement(accountAchievement.Id);
                        if (currentAccountAchievement is null) { Logger.Debug($"Requested achievement by id is null, id: {accountAchievement.Id}"); continue; }
                        if (currentAccountAchievement.Bits is null) continue;
                        foreach (AchievementBit bit in currentAccountAchievement.Bits)
                        {
                            if (bit is null) { Logger.Debug($"Bit in {currentAccountAchievement.Id} is null"); continue; }
                            if (_ignoreCaughtFish.Value && accountAchievement.Bits != null && accountAchievement.Bits.Contains(bitsCounter)) { bitsCounter++; continue; }
                            int itemId = ((AchievementItemBit)bit).Id;
                            Item fish = await this.RequestItem(itemId);
                            Logger.Debug($"Found Fish '{fish.Name}' id: '{fish.Id}'");
                            // Get first fish in all fish list that matches name
                            var fishIdMatch = this._allFishList.Where(phish => phish.ItemId == fish.Id);
                            Fish ghoti = fishIdMatch.Count() != 0 ? fishIdMatch.First() : null;
                            if (ghoti is null) { Logger.Warn($"Missing fish from all fish list: name: '{fish.Name}' id: '{fish.Id}' (This may be caused by language)"); continue; }
                            ghoti.Caught = accountAchievement.Bits != null && accountAchievement.Bits.Contains(bitsCounter);
                            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
                            ghoti.Visible = ghoti.Time == Fish.TimeOfDay.Any ||
                                this._timeOfDayClock.TimePhase.Equals("Dawn") || this._timeOfDayClock.TimePhase.Equals("Dusk") ||
                                Equals(ghoti.Time.ToString(), this._timeOfDayClock.TimePhase);
                            ghoti.Icon = fish.Icon; ghoti.ItemId = fish.Id; ghoti.AchievementId = currentAccountAchievement.Id;
                            ghoti.IconImg = this.RequestItemIcon(fish);
                            // Only add if no special cases or fits in special case
                            if (ghoti.Locations is null || ghoti.Locations.Contains(_currentMap.Id)) {
                                this.catchableFish.Add(ghoti);
                            } else { Logger.Debug($"Skipping {fish.Name} {fish.Id}, not available in current map."); }
                            bitsCounter++;
                        }
                        bitsCounter = 0;
                    }
                }
                else
                {
                    Logger.Debug("Not using API");
                    var currentMapAchievableIds = from achievementId in FishingMaps.BASE_FISHER_ACHIEVEMENT_IDS where achievementsInMap.Contains(achievementId) select achievementId;
                    Logger.Debug($"Current map achievable: {string.Join(", ", currentMapAchievableIds)}");
                    foreach (int achievementId in currentMapAchievableIds)
                    {
                        Achievement currentAchievement = await this.RequestAchievement(achievementId);
                        if (currentAchievement is null) { Logger.Debug($"Requested achievement by id is null, id: {achievementId}"); continue; }
                        foreach (AchievementBit bit in currentAchievement.Bits)
                        {
                            if (bit is null) { Logger.Debug($"Bit in {currentAchievement.Id} is null"); continue; }
                            int itemId = ((AchievementItemBit)bit).Id;
                            Item fish = await this.RequestItem(itemId);
                            if (fish != null) { Logger.Debug($"Found Fish '{fish.Name}' id: '{fish.Id}'"); }
                            else { Logger.Warn($"Skipping fish due to API issue. id: '{itemId}'"); continue; }
                            // Get first fish in all fish list that matches name
                            var fishIdMatch = this._allFishList.Where(phish => phish.ItemId == fish.Id);
                            Fish ghoti = fishIdMatch.Count() != 0 ? fishIdMatch.First() : null;
                            if (ghoti is null) { Logger.Warn($"Missing fish from all fish list: '{fish.Name}' id: '{fish.Id}'"); continue; }
                            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
                            ghoti.Visible = ghoti.Time == Fish.TimeOfDay.Any ||
                                this._timeOfDayClock.TimePhase.Equals("Dawn") || this._timeOfDayClock.TimePhase.Equals("Dusk") ||
                                Equals(ghoti.Time.ToString(), this._timeOfDayClock.TimePhase);
                            // TODO AutoMapper merge here instead of all these sets? https://github.com/AutoMapper/AutoMapper
                            ghoti.Name = fish.Name; ghoti.Icon = fish.Icon; ghoti.ItemId = fish.Id; ghoti.Achievement = currentAchievement.Name; ghoti.AchievementId = currentAchievement.Id;
                            ghoti.Rarity = fish.Rarity; ghoti.ChatLink = fish.ChatLink; ghoti.IconImg = this.RequestItemIcon(fish);
                            // Only add if no special cases or fits in special case
                            if (ghoti.Locations is null || ghoti.Locations.Contains(_currentMap.Id)) {
                                this.catchableFish.Add(ghoti);
                            } else { Logger.Debug($"Skipping {fish.Name} {fish.Id}, not available in current map."); }
                        }
                    }
                }
                if (!_displayUncatchableFish.Value) this.catchableFish = this.catchableFish.Where(phish => phish.Visible).ToList();
                Logger.Debug("Shown fish in current map count: " + this.catchableFish.Count());
            }
            catch (Exception ex) { Logger.Warn(ex, $"Unknown exception getting current map ({_currentMap.Name} {_currentMap.Id}) fish"); }
            finally { this._updateFishSemaphore.Release(); }
        }

        // based on https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs
        private async Task<Map> RequestMap(int id)
        {
            Logger.Debug($"Requested map id: {id}");
            try {
                Task<Map> mapTask = this.Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id);
                await mapTask;
                return mapTask.Result;
            }
            catch (Exception ex) {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
        }

        // TODO Add retry to Request(s)...
        private async Task<Achievement> RequestAchievement(int id)
        {
            Logger.Debug($"Requested achievement id: {id}");
            // TODO instead of await each call. queue/addtolist each task, Task.WaitAll(queue/list), requeue nulls/failures/errors?
            try {
                Task<Achievement> achievementTask = this.Gw2ApiManager.Gw2ApiClient.V2.Achievements.GetAsync(id);
                await achievementTask;
                return achievementTask.Result;
            }
            catch (Exception ex) {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
        }

        private async Task<Item> RequestItem(int id)
        {
            Logger.Debug($"Requested item id: {id}");
            try {
                Task<Item> itemTask = this.Gw2ApiManager.Gw2ApiClient.V2.Items.GetAsync(id);
                await itemTask;
                return itemTask.Result;
            }
            catch (Exception ex) {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
        }

        // TODO find a better way to wait on texture finished loading
        private AsyncTexture2D RequestItemIcon(Item item) => GameService.Content.GetRenderServiceTexture(item.Icon);
    }
}
