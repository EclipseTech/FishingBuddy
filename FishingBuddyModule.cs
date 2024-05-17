// TODO cache fishing images from api, save / download icons to directory cache & get from cache before web, Download / Use Icon w/ GetRenderServiceTexture ex: GameService.Content.GetRenderServiceTexture(fish.Icon);
// TODO should be caching map info too
// TODO in bounds checking for UI elements, ex: https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L64 & https://github.com/manlaan/BlishHud-Clock/blob/main/Module.cs#L145
// TODO notifications? on dawn https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs#L108 ?15 sec til?
// TODO BLOCKED info out of date from API (inventory permissions required) Add caught fish counter (count per rarity & ? count per type of fish ? per zone ? per session ? per hour ?)
// TODO BLOCKED get/display equipped lure & bait w/ #s (optional w/ mouseover info)
// TODO BLOCKED bait & lure icons via api... get bait & lure type/count from api? is this even detailed anywhere? or no api yet for this?
namespace Eclipse1807.BlishHUD.FishingBuddy
{
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
    using MonoGame.Extended.Collections;
    using MoreLinq;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class FishingBuddyModule : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(FishingBuddyModule));

        internal static FishingBuddyModule ModuleInstance;

        #region Static Images
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
        internal static Texture2D _imgBaitAny;
        internal static Texture2D _imgBaitFishEgg;
        internal static Texture2D _imgBaitGlowWorm;
        internal static Texture2D _imgBaitFreshwaterMinnow;
        internal static Texture2D _imgBaitLavaBeetle;
        internal static Texture2D _imgBaitLeech;
        internal static Texture2D _imgBaitLightningBug;
        internal static Texture2D _imgBaitMackerel;
        internal static Texture2D _imgBaitNightcrawler;
        internal static Texture2D _imgBaitRamshornSnail;
        internal static Texture2D _imgBaitSardine;
        internal static Texture2D _imgBaitScorpion;
        internal static Texture2D _imgBaitShrimpling;
        internal static Texture2D _imgBaitSparkflyLarva;
        #endregion Static Images

        private AsyncCache<int, Map> _mapRepository;

        #region Fish Settings
        //TODO minimum rarity level to show fish
        private static ClickThroughPanel _fishPanel;
        private bool _draggingFishPanel;
        private Point _dragFishPanelStart = Point.Zero;
        public static SettingEntry<bool> _dragFishPanel;
        public static SettingEntry<int> _fishImgSize;
        public static SettingEntry<Point> _fishPanelLoc;
        public static readonly string[] _fishPanelOrientations = new string[] { Properties.Strings.Vertical, Properties.Strings.Horizontal };
        public static SettingEntry<string> _fishPanelOrientation;
        public static readonly string[] _fishPanelDirections = new string[] { Properties.Strings.Top_left, Properties.Strings.Top_right, Properties.Strings.Bottom_left, Properties.Strings.Bottom_right };
        public static SettingEntry<string> _fishPanelDirection;
        public static SettingEntry<string> _fishPanelTooltipDisplay;
        public static SettingEntry<bool> _showRarityBorder;
        public static readonly string[] _verticalAlignmentOptions = new string[] { "Top", "Middle", "Bottom" };
        #endregion Fish Settings
        #region Bait settings
        //TODO minimum rarity level to show bait
        //TODO panel orientation
        //TODO tooltip enhancements; FishingHole, bait images
        private static ClickThroughPanel _baitPanel;
        private bool _draggingBaitPanel;
        private Point _dragBaitPanelStart = Point.Zero;
        public static SettingEntry<bool> _dragBaitPanel;
        public static SettingEntry<int> _baitImgSize;
        public static SettingEntry<Point> _baitPanelLoc;
        #endregion Bait settings
        #region Clock Settings
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
        public static SettingEntry<string> _settingClockAlign;
        #endregion Clock Settings

        private IEnumerable<AccountAchievement> accountFishingAchievements;
        private FishingMaps _fishingMaps;
        private List<Fish> catchableFish;
        private IEnumerable<Fish> catchable;
        private OrderedDictionary sharkBait;
        private List<Fish> _allFishList;
        private Clock _timeOfDayClock;
        internal static Map _currentMap;
        internal static bool _useAPIToken;
        private readonly SemaphoreSlim _updateFishSemaphore = new SemaphoreSlim(1, 1);
        private bool MumbleIsAvailable => GameService.Gw2Mumble.IsAvailable && GameService.GameIntegration.Gw2Instance.IsInGame;
        private bool UiIsAvailable => this.MumbleIsAvailable && !GameService.Gw2Mumble.UI.IsMapOpen;
        private bool HidingInCombat => this.MumbleIsAvailable && _hideInCombat.Value && GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
        // 5 minutes API update interval
        private readonly double INTERVAL_UPDATE_FISH = 5 * 60 * 1000;
        private double _lastUpdateFish = 0;
        private bool _fishAndBaitAreManuallyHidden = false;

        #region Service Managers
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        #endregion Service Managers

        [ImportingConstructor]
        public FishingBuddyModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void DefineSettings(SettingCollection settings)
        {
            // Fish Settings
            _ignoreCaughtFish = settings.DefineSetting("IgnoreCaughtFish", true, () => Properties.Strings.SettingsIgnoreCaught, () => Properties.Strings.SettingsIgnoreCaughtDescription);
            _includeSaltwater = settings.DefineSetting("IncludeSaltwater", false, () => Properties.Strings.SettingsDisplaySaltwater, () => Properties.Strings.SettingsDisplaySaltwaterDescription);
            _includeWorldClass = settings.DefineSetting("IncludeWorldClass", false, () => Properties.Strings.SettingsDisplayWorldClass, () => Properties.Strings.SettingsDisplayWorldClassDescription);
            _displayUncatchableFish = settings.DefineSetting("DisplayUncatchable", false, () => Properties.Strings.SettingsDisplayUncatchable, () => Properties.Strings.SettingsDisplayUncatchableDescription);
            _fishPanelLoc = settings.DefineSetting("FishPanelLoc", new Point(160, 100), () => Properties.Strings.FishPanelLocation, () => string.Empty);
            _dragFishPanel = settings.DefineSetting("FishPanelDrag", false, () => Properties.Strings.FishPanelDrag, () => string.Empty);
            _fishImgSize = settings.DefineSetting("FishImgWidth", 30, () => Properties.Strings.FishPanelSize, () => string.Empty);
            _showRarityBorder = settings.DefineSetting("ShowRarityBorder", true, () => Properties.Strings.SettingsRarity, () => Properties.Strings.RarityDescription);
            _fishPanelOrientation = settings.DefineSetting("FishPanelOrientation", _fishPanelOrientations.First(), () => Properties.Strings.Orientation, () => Properties.Strings.OrientationDescription);
            _fishPanelDirection = settings.DefineSetting("FishPanelDirection", _fishPanelDirections.First(), () => Properties.Strings.Direction, () => Properties.Strings.SettingsDirectionDescription);
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
            _fishPanelTooltipDisplay = settings.DefineSetting("FishPanelTooltipDisplay", "#1\n@2\n@3\n@4\n@5\n@6\n@7", () => Properties.Strings.TooltipDisplay, () =>
                                                              $"{Properties.Strings.Default}: #1\\n@2\\n@3\\n@4\\n@5\\n@6\\n@7\n" +
                                                              Properties.Strings.SimpleTooltip +
                                                              Properties.Strings.CompactTooltip +
                                                              Properties.Strings.FishPanelTooltipDescription +
                                                              $"@#1: {Properties.Strings.FishName}\n" +
                                                              $"@#2: {Properties.Strings.FishFavoredBait}\n" +
                                                              $"@#3: {Properties.Strings.FishTimeOfDay}\n" +
                                                              $"@#4: {Properties.Strings.FishFishingHole}\n" +
                                                              $"@#5: {Properties.Strings.Achievement}\n" +
                                                              $"@#6: {Properties.Strings.SettingsRarity}\n" +
                                                              $"@7:  {Properties.Strings.ReasonForHiding}\n" +
                                                              $"@#8: {Properties.Strings.FishyNotes}\n" +
                                                              $"(\\n {Properties.Strings.AddsNewLines})");
            _fishPanelTooltipDisplay.SettingChanged += this.OnUpdateSettings;
            // Bait Settings
            _baitPanelLoc = settings.DefineSetting("BaitPanelLoc", new Point(100, 100), () => Properties.Strings.BaitPanelLocation, () => string.Empty);
            _dragBaitPanel = settings.DefineSetting("BaitPanelDrag", false, () => Properties.Strings.BaitPanelDrag, () => string.Empty);
            _baitImgSize = settings.DefineSetting("BaitImgWidth", 30, () => Properties.Strings.BaitPanelSize, () => string.Empty);
            _baitPanelLoc.SettingChanged += this.OnUpdateSettings;
            _dragBaitPanel.SettingChanged += this.OnUpdateSettings;
            _baitImgSize.SettingChanged += this.OnUpdateSettings;
            _baitImgSize.SetRange(16, 96);
            // Time of Day Settings
            _timeOfDayPanelLoc = settings.DefineSetting("TimeOfDayPanelLoc", new Point(100, 100), () => Properties.Strings.TimeOfDayPanelLoc, () => string.Empty);
            _dragTimeOfDayClock = settings.DefineSetting("TimeOfDayPanelDrag", false, () => Properties.Strings.TimeOfDayPanelDrag, () => Properties.Strings.TimeOfDayPanelDragDescription);
            _timeOfDayImgSize = settings.DefineSetting("TimeImgWidth", 64, () => Properties.Strings.TimeOfDaySize, () => string.Empty);
            _settingClockLabel = settings.DefineSetting("ClockLabel", false, () => Properties.Strings.TimeOfDayHideLabel, () => Properties.Strings.TimeOfDayHideLabelDescription);
            _settingClockAlign = settings.DefineSetting("TimeLabelAlign", "Bottom", () => Properties.Strings.TimeOfDayLabelPosition, () => Properties.Strings.TimeOfDayLabelPositionDescription);
            _hideTimeOfDay = settings.DefineSetting("HideTimeOfDay", false, () => Properties.Strings.TimeOfDayHide, () => Properties.Strings.TimeOfDayHideDescription);
            _timeOfDayPanelLoc.SettingChanged += this.OnUpdateClockLocation;
            _dragTimeOfDayClock.SettingChanged += this.OnUpdateClockSettings;
            _timeOfDayImgSize.SetRange(16, 96);
            _timeOfDayImgSize.SettingChanged += this.OnUpdateClockSize;
            _dragTimeOfDayClock.SettingChanged += this.OnUpdateClockSettings;
            _hideTimeOfDay.SettingChanged += this.OnUpdateClockSettings;
            _settingClockAlign.SettingChanged += this.OnUpdateClockLabelAlign;
            _settingClockLabel.SettingChanged += this.OnUpdateHideClockLabel;
            // Common settings
            _hideInCombat = settings.DefineSetting("HideInCombat", false, () => Properties.Strings.HideInCombat, () => Properties.Strings.HideInCombatDescription);
            _hideInCombat.SettingChanged += this.OnUpdateFishSettings;
        }

        protected override void Initialize()
        {
            this.Gw2ApiManager.SubtokenUpdated += this.OnApiSubTokenUpdated;
            this.catchableFish = new List<Fish>();
            this.catchable = new List<Fish>();
            this.sharkBait = new OrderedDictionary();
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
            _imgBaitAny = this.ContentsManager.GetTexture(@"Nightcrawler.png");
            _imgBaitFishEgg = this.ContentsManager.GetTexture(@"Fish_Egg.png");
            _imgBaitGlowWorm = this.ContentsManager.GetTexture(@"Glow_Worm.png");
            _imgBaitFreshwaterMinnow = this.ContentsManager.GetTexture(@"Minnow.png");
            _imgBaitLavaBeetle = this.ContentsManager.GetTexture(@"Lava_Beetle.png");
            _imgBaitLeech = this.ContentsManager.GetTexture(@"Leech.png");
            _imgBaitLightningBug = this.ContentsManager.GetTexture(@"Lightning_Bug.png");
            _imgBaitMackerel = this.ContentsManager.GetTexture(@"Mackerel.png");
            _imgBaitNightcrawler = this.ContentsManager.GetTexture(@"Nightcrawler.png");
            _imgBaitRamshornSnail = this.ContentsManager.GetTexture(@"Ramshorn_Snail.png");
            _imgBaitSardine = this.ContentsManager.GetTexture(@"Sardine.png");
            _imgBaitScorpion = this.ContentsManager.GetTexture(@"Scorpion.png");
            _imgBaitShrimpling = this.ContentsManager.GetTexture(@"Shrimpling.png");
            _imgBaitSparkflyLarva = this.ContentsManager.GetTexture(@"Sparkfly_Larva.png");

            this._allFishList = new List<Fish>();

            // Load fish.json data
            using (StreamReader r = new StreamReader(this.ContentsManager.GetFileStream(@"fish.json")))
            {
                string json = r.ReadToEnd();
                this._allFishList.AddRange(JsonConvert.DeserializeObject<List<Fish>>(json));
                Logger.Debug("Fish list: " + string.Join(", ", this._allFishList.Select(fish => fish.Name)));
            }
            FishingBuddyModule._useAPIToken = true;
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

            this._timeOfDayClock.RightMouseButtonPressed += this.OnClockRightClick;

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
            UpdateCadenceUtil.UpdateAsyncWithCadence(this.GetCurrentMapFishingInfo, gameTime, this.INTERVAL_UPDATE_FISH, ref this._lastUpdateFish);

            var showUi = this.UiIsAvailable && !this.HidingInCombat;

            if (showUi)
            {
                this.GetCurrentMapTime();
                if (!_hideTimeOfDay.Value) this._timeOfDayClock.Show();
            }
            else
            {
                this._timeOfDayClock.Hide();
            }

            if (showUi && !this._fishAndBaitAreManuallyHidden)
            {
                _fishPanel.Show();
                _baitPanel.Show();
            } else
            {
                _fishPanel.Hide();
                _baitPanel.Hide();
            }

            if (this._draggingFishPanel)
            {
                Point fishPanelMoveOffset = InputService.Input.Mouse.Position - this._dragFishPanelStart;
                _fishPanel.Location += fishPanelMoveOffset;

                this._dragFishPanelStart = InputService.Input.Mouse.Position;
            }
            if (this._draggingBaitPanel) {
               Point baitPanelMoveOffset = InputService.Input.Mouse.Position - this._dragBaitPanelStart;
               _baitPanel.Location += baitPanelMoveOffset;

               this._dragBaitPanelStart = InputService.Input.Mouse.Position;
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
            // Bait Settings
            _baitPanelLoc.SettingChanged -= this.OnUpdateSettings;
            _dragBaitPanel.SettingChanged -= this.OnUpdateSettings;
            _baitImgSize.SettingChanged -= this.OnUpdateSettings;
            _baitPanel?.Dispose();
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

            _timeOfDayClock.RightMouseButtonPressed -= this.OnClockRightClick;

            GameService.Gw2Mumble.CurrentMap.MapChanged -= this.OnMapChanged;
            if (this._timeOfDayClock != null)
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
            await this.GetCurrentMapFishingInfo();
            this.DrawIcons();
        }

        // TODO move this to Clock.cs
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

        // TODO move this to Clock.cs
        private void OnUpdateClockLabelAlign(object sender = null, ValueChangedEventArgs<string> e = null)
            => this._timeOfDayClock.LabelVerticalAlignment = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), _settingClockAlign.Value);

        // TODO move this to Clock.cs
        private void OnUpdateHideClockLabel(object sender = null, ValueChangedEventArgs<bool> e = null)
            => this._timeOfDayClock.HideLabel = _settingClockLabel.Value;

        // TODO move this to Clock.cs
        private void OnUpdateClockLocation(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            // Offscreen reset
            if (_timeOfDayPanelLoc.Value.X < 0)
                _timeOfDayPanelLoc.Value = new Point(0, _timeOfDayPanelLoc.Value.Y);
            if (_timeOfDayPanelLoc.Value.Y < 0)
                _timeOfDayPanelLoc.Value = new Point(_timeOfDayPanelLoc.Value.X, 0);
            this._timeOfDayClock.Location = _timeOfDayPanelLoc.Value;
        }

        // TODO move this to Clock.cs
        private void OnUpdateClockSize(object sender = null, ValueChangedEventArgs<int> e = null)
            => this._timeOfDayClock.Size = new Point(_timeOfDayImgSize.Value);

        private void OnClockRightClick(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            _fishAndBaitAreManuallyHidden = !_fishAndBaitAreManuallyHidden;
        }

        // TODO move this to Fish.cs
        protected void DrawIcons()
        {
            // Setup fish panel
            _fishPanel?.Dispose();
            int fishPanelRows = Clamp((int)Math.Ceiling((double)this.catchableFish.Count() / 2), 1, 7);
            int fishPanelColumns = Clamp((int)Math.Ceiling((double)this.catchableFish.Count() / fishPanelRows), 1, 7);
            // swap row column if necessary
            if (fishPanelRows < fishPanelColumns) { int swap = fishPanelRows; fishPanelRows = fishPanelColumns; fishPanelColumns = swap; }
            if (Equals(_fishPanelOrientation.Value, Properties.Strings.Horizontal)) { int swap = fishPanelRows; fishPanelRows = fishPanelColumns; fishPanelColumns = swap; }
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
            if (Equals(_fishPanelDirection.Value, Properties.Strings.Top_right)) {
                x = _fishPanel.Size.X - _fishImgSize.Value; xStart = x;
            } else if (Equals(_fishPanelDirection.Value, Properties.Strings.Bottom_left)) {
                y = _fishPanel.Size.Y - _fishImgSize.Value; yStart = y;
            } else if (Equals(_fishPanelDirection.Value, Properties.Strings.Bottom_right)) {
                x = _fishPanel.Size.X - _fishImgSize.Value; xStart = x;
                y = _fishPanel.Size.Y - _fishImgSize.Value; yStart = y;
            }

            // Add fish to fish panel
            foreach (Fish fish in catchable)
            {
                string fishTooltip = Fish.BuildFishTooltip(fish);
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
                if (Equals(_fishPanelDirection.Value, Properties.Strings.Top_left) || Equals(_fishPanelDirection.Value, Properties.Strings.Bottom_left)) x += _fishImgSize.Value;
                // Build Right -> Left
                if (Equals(_fishPanelDirection.Value, Properties.Strings.Top_right) || Equals(_fishPanelDirection.Value, Properties.Strings.Bottom_right)) x -= _fishImgSize.Value;
                if (count == fishPanelColumns)
                {
                    x = xStart;
                    // Build Top -> Bottom
                    if (Equals(_fishPanelDirection.Value, Properties.Strings.Top_left) || Equals(_fishPanelDirection.Value, Properties.Strings.Top_right)) y += _fishImgSize.Value;
                    // Build Bottom -> Top
                    if (Equals(_fishPanelDirection.Value, Properties.Strings.Bottom_left) || Equals(_fishPanelDirection.Value, Properties.Strings.Bottom_right)) y -= _fishImgSize.Value;
                    count = 0;
                }
                count++;
            }

            // Setup bait panel
            _baitPanel?.Dispose();
            int baitPanelColumns = 3;
            x = 0; y = 0; count = 1; xStart = x; yStart = y;
            _baitPanel = new ClickThroughPanel() {
                Parent = GameService.Graphics.SpriteScreen,
                Location = _baitPanelLoc.Value,
                Size = new Point(_baitImgSize.Value * 3),
                Capture = _dragBaitPanel.Value
            };
            Logger.Debug($"Bait Panel Size; {_baitPanel.Size}");

            // Add bait to bait panel
            foreach (FishBait bait in sharkBait.Keys) {
                // Only display "Any" bait when any is only
                if (bait == FishBait.Any && sharkBait.Count > 1) continue;
                string baitTooltip = FishingBait.BuildBaitTooltip(bait, (List<Fish.FishingHole>)sharkBait[bait]);
                // Bait image
                new ClickThroughImage {
                    Parent = _baitPanel,
                    Texture = FishingBait.Bait[bait].IconImg,
                    Size = new Point(_baitImgSize.Value),
                    Location = new Point(x, y),
                    ZIndex = 0,
                    Capture = _dragBaitPanel.Value,
                    BasicTooltipText = baitTooltip,
                    Opacity = 1.0f
                };
                x += _baitImgSize.Value;
                if (count == baitPanelColumns) {
                    x = xStart;
                    y += _baitImgSize.Value;
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
                    // TODO here is where to validate panel is clampped to screen
                    _fishPanelLoc.Value = _fishPanel.Location;
                    _fishPanel.ShowTint = false;
                };
            }

            if (_dragBaitPanel.Value) {
                _baitPanel.Capture = true;
                _baitPanel.LeftMouseButtonPressed += delegate {
                    this._draggingBaitPanel = true;
                    this._dragBaitPanelStart = InputService.Input.Mouse.Position;
                    _baitPanel.ShowTint = true;
                };
                _baitPanel.LeftMouseButtonReleased += delegate {
                    this._draggingBaitPanel = false;
                    // TODO here is where to validate panel is clampped to screen
                    _baitPanelLoc.Value = _baitPanel.Location;
                    _baitPanel.ShowTint = false;
                };
            }
        }

        // TODO move this to Fish.cs
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

        // TODO move this to a helper
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
            await this.GetCurrentMapFishingInfo();
            this.DrawIcons();
        }

        private async void OnTimeOfDayChanged(object sender = null, ValueChangedEventArgs<string> e = null)
        {
            await this.GetCurrentMapFishingInfo();
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
                FishingBuddyModule._useAPIToken = false;
                return;
            }

            try
            {
                await this.GetCurrentMapFishingInfo();
                this.DrawIcons();
                FishingBuddyModule._useAPIToken = true;
            }
            catch (Exception)
            {
                Logger.Debug("Failed to get info from api.");
            }
        }

        // TODO maybe move this to Fish or FishingMaps?
        private async Task GetCurrentMapFishingInfo(GameTime gameTime)
        {
            await this.GetCurrentMapFishingInfo();
            this.DrawIcons();
        }
        private async Task GetCurrentMapFishingInfo(CancellationToken cancellationToken = default)
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
                        FishingBuddyModule._useAPIToken = true;

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
                        FishingBuddyModule._useAPIToken = false;
                    }
                } catch (Exception ex) {
                    Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                    FishingBuddyModule._useAPIToken = false;
                }

                // Refresh info
                this.catchableFish.Clear();
                this.sharkBait.Clear();

                // Achievement Ids from current map
                List<int> achievementsInMap = new List<int>();
                List<int> verifyMapAchievable = new List<int>();

                // Get player's current map if necessary
                if (_currentMap is null)
                {
                    try {
                        _currentMap = await this._mapRepository.GetItem(GameService.Gw2Mumble.CurrentMap.Id);
                    } catch (Exception ex) {
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
                if (achievementsInMap.Count == 0) { Logger.Debug($"No achievable fish in map: {_currentMap.Id}"); return; }
                Logger.Debug($"All map achievements: {string.Join(", ", achievementsInMap)}");

                if (FishingBuddyModule._useAPIToken)
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
                        if (currentAccountAchievement is null) { Logger.Debug($"Requested achievement by id is null, account achievement id: {accountAchievement.Id}"); continue; }
                        if (currentAccountAchievement.Bits is null) { Logger.Debug($"Requested achievement bits are null, account achievement id: {accountAchievement.Id}"); continue; }
                        foreach (AchievementBit bit in currentAccountAchievement.Bits)
                        {
                            if (bit is null) { Logger.Debug($"Bit in {currentAccountAchievement.Id} is null"); continue; }
                            if (_ignoreCaughtFish.Value && accountAchievement.Bits != null && accountAchievement.Bits.Contains(bitsCounter)) { bitsCounter++; continue; }
                            this.AddCatchableFish(((AchievementItemBit)bit).Id, currentAccountAchievement, accountAchievement.Bits != null && accountAchievement.Bits.Contains(bitsCounter));
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
                        if (currentAchievement is null) { Logger.Debug($"Requested achievement by id is null, achievement id: {achievementId}"); continue; }
                        foreach (AchievementBit bit in currentAchievement.Bits)
                        {
                            if (bit is null) { Logger.Debug($"Bit in {currentAchievement.Id} is null"); continue; }
                            this.AddCatchableFish(((AchievementItemBit)bit).Id, currentAchievement, false);
                        }
                    }
                }
                if (!_displayUncatchableFish.Value) this.catchableFish = this.catchableFish.Where(phish => phish.Visible).ToList();
                Logger.Debug("Shown fish in current map count: " + this.catchableFish.Count());

                //TODO give setting options to sort fish???
                // For now sort uncaught -> caught -> uncatchable -> rarity -> name
                // Thoughts: is there an order panel / drag drop type?
                // ! descending order @ ascending order
                // "1": Uncaught; "2": Caught; "3": Rarity; "4" Name; bait/time/hole/???
                // default: !1@2@3@4
                catchable = this.catchableFish.OrderByDescending(f => f.Visible).
                                               ThenBy(f => f.Caught && f.Visible).
                                               ThenBy(f => f.Rarity).
                                               ThenBy(f => f.Name);

                var visibleFish = (from fish in catchable where fish.Visible select fish);
                var baits = visibleFish.DistinctBy(f => f.Bait).OrderByDescending(f => f.Rarity);
                foreach (Fish fishy in baits)
                {
                    var holes = (from fish in visibleFish where fishy.Name == fish.Name select fish.Hole).Distinct();
                    sharkBait.Add(fishy.Bait, holes.ToList());
                }
            }
            catch (Exception ex) { Logger.Debug(ex, $"Unknown exception getting current map ({_currentMap.Name} {_currentMap.Id}) info"); }
            finally { this._updateFishSemaphore.Release(); }
        }

        private async void AddCatchableFish(int fishItemId, Achievement achievement, bool caught)
        {
            Item fish = await this.RequestItem(fishItemId);
            if (fish == null) { Logger.Debug($"Skipping fish due to API issue. id: '{fishItemId}'"); return; }
            Logger.Debug($"Found Fish '{fish.Name}' id: '{fish.Id}'");
            // Get first fish in all fish list that matches name
            var fishIdMatch = this._allFishList.Where(phish => phish.ItemId == fish.Id);

            Fish ghoti = fishIdMatch.Count() != 0 ? fishIdMatch.First() : null;
            if (ghoti is null) { Logger.Debug($"Missing fish from all fish list: name: '{fish.Name}' id: '{fish.Id}'"); return; }
            ghoti.Caught = caught;//accountAchievement.Bits != null && accountAchievement.Bits.Contains(bitsCounter);
            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
            ghoti.Visible = ghoti.Time == Fish.TimeOfDay.Any ||
                this._timeOfDayClock.TimePhase.Equals(Properties.Strings.Dawn) || this._timeOfDayClock.TimePhase.Equals(Properties.Strings.Dusk) ||
                Equals(ghoti.Time.ToString(), this._timeOfDayClock.TimePhase);
            // TODO AutoMapper merge here instead of all these sets? https://github.com/AutoMapper/AutoMapper
            ghoti.Name = fish.Name; ghoti.Icon = fish.Icon; ghoti.ItemId = fish.Id; ghoti.Achievement = achievement.Name; ghoti.AchievementId = achievement.Id;
            ghoti.Rarity = fish.Rarity; ghoti.ChatLink = fish.ChatLink; ghoti.IconImg = this.RequestItemIcon(fish);

            // Only add if no special cases or fits in special case
            if (ghoti.Locations is null || ghoti.Locations.Contains(_currentMap.Id))
            { this.catchableFish.Add(ghoti); }
            else { Logger.Debug($"Skipping {fish.Name} {fish.Id}, not available in current map."); }
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
