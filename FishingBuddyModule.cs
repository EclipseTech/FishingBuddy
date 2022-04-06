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


// TODO display timeofday countdown timer
// TODO should other map Ids not show any fishing info, or show open water info? or saltwater/world class info? hide in instances?
// TODO cache fishing images from api, save / download icons to directory cache & get from cache before web, Download / Use Icon w/ GetRenderServiceTexture ex: GameService.Content.GetRenderServiceTexture(fish.Icon);
// TODO should be caching map info too
// TODO in bounds checking for UI elemends, ex: https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L64
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
        private Texture2D _imgDawn;
        private Texture2D _imgDay;
        private Texture2D _imgDusk;
        private Texture2D _imgNight;
        private Texture2D _imgBorder;

        // Turned off until can get character fishing details
        //private ClickThroughImage _lure;
        //private ClickThroughImage _bait;
        private ClickThroughImage _dawn;
        private ClickThroughImage _day;
        private ClickThroughImage _dusk;
        private ClickThroughImage _night;

        private AsyncCache<int, Map> _mapRepository;
        //private AsyncCache<int, Achievement> _achievementRepository;
        //private AsyncCache<int, Item> _itemRepository;

        private ClickThroughPanel _fishPanel;
        private bool _draggingFishPanel;
        private Point _dragFishPanelStart = Point.Zero;
        private ClickThroughPanel _timeOfDayPanel;
        private bool _draggingTimeOfDayPanel;
        private Point _dragTimeOfDayPanelStart = Point.Zero;
        public static SettingEntry<bool> _dragFishPanel;
        public static SettingEntry<int> _fishImgWidth;
        private static SettingEntry<Point> _fishPanelLoc;
        public static SettingEntry<bool> _dragTimeOfDayPanel;
        public static SettingEntry<int> _timeOfDayImgWidth;
        private static SettingEntry<Point> _timeOfDayPanelLoc;
        public static SettingEntry<bool> _ignoreCaughtFish;
        public static SettingEntry<bool> _includeWorldClass;
        public static SettingEntry<bool> _includeSaltwater;
        private List<Fish> catchableFish;
        private FishingMaps fishingMaps;
        private IEnumerable<AccountAchievement> accountFishingAchievements;

        private List<Fish> _allFishList;
        private Map _currentMap;
        private bool _useAPIToken;
        private readonly SemaphoreSlim _updateFishSemaphore = new SemaphoreSlim(1, 1);

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
            _ignoreCaughtFish = settings.DefineSetting("IgnoreCaughtFish", true, () => "Ignore Caught Fish", () => "Ignore fish already counted towards achievements");
            _ignoreCaughtFish.SettingChanged += OnUpdateFish;
            _includeSaltwater = settings.DefineSetting("IncludeSaltwater", false, () => "Include Saltwater Fish", () => "Include Saltwater Fisher fish");
            _includeSaltwater.SettingChanged += OnUpdateFish;
            _includeWorldClass = settings.DefineSetting("IncludeWorldClass", false, () => "Include World Class Fish", () => "Include World Class Fisher fish");
            _includeWorldClass.SettingChanged += OnUpdateFish;
            _fishPanelLoc = settings.DefineSetting("FishPanelLoc", new Point(160, 100), () => "Fish Location", () => "");
            _fishPanelLoc.SettingChanged += OnUpdateSettings;
            _dragFishPanel = settings.DefineSetting("FishPanelDrag", false, () => "Drag Fish", () => "");
            _dragFishPanel.SettingChanged += OnUpdateSettings;
            _fishImgWidth = settings.DefineSetting("FishImgWidth", 30, () => "Fish Size", () => "");
            _fishImgWidth.SetRange(16, 96);
            _fishImgWidth.SettingChanged += OnUpdateSettings;
            _timeOfDayPanelLoc = settings.DefineSetting("TimeOfDayPanelLoc", new Point(100, 100), () => "Time of Day Details Location", () => "");
            _timeOfDayPanelLoc.SettingChanged += OnUpdateSettings;
            _dragTimeOfDayPanel = settings.DefineSetting("TimeOfDayPanelDrag", false, () => "Drag Time of Day Details", () => "");
            _dragTimeOfDayPanel.SettingChanged += OnUpdateSettings;
            _timeOfDayImgWidth = settings.DefineSetting("TimeImgWidth", 64, () => "Time of Day Size", () => "");
            _timeOfDayImgWidth.SetRange(16, 96);
            _timeOfDayImgWidth.SettingChanged += OnUpdateSettings;
        }

        protected override void Initialize()
        {
            Gw2ApiManager.SubtokenUpdated += OnApiSubTokenUpdated;
            catchableFish = new List<Fish>();
            fishingMaps = new FishingMaps();
            _mapRepository = new AsyncCache<int, Map>(RequestMap);
            //_achievementRepository = new AsyncCache<int, Achievement>(RequestAchivement);
            //_itemRepository = new AsyncCache<int, Item>(RequestItem);
            //_imgLure = ContentsManager.GetTexture(@"lure.png");
            //_imgBait = ContentsManager.GetTexture(@"bait.png");
            _imgDawn = ContentsManager.GetTexture(@"dawn.png");
            _imgDay = ContentsManager.GetTexture(@"day.png");
            _imgDusk = ContentsManager.GetTexture(@"dusk.png");
            _imgNight = ContentsManager.GetTexture(@"night.png");
            _imgBorder = ContentsManager.GetTexture(@"border.png");

            _dawn = new ClickThroughImage();
            _day = new ClickThroughImage();
            _dusk = new ClickThroughImage();
            _night = new ClickThroughImage();

            _allFishList = new List<Fish>();

            using (StreamReader r = new StreamReader(ContentsManager.GetFileStream(@"fish.json")))
            {
                string json = r.ReadToEnd();
                _allFishList.AddRange(JsonConvert.DeserializeObject<List<Fish>>(json));
                Logger.Debug("fish list: " + string.Join(", ", _allFishList.Select(fish => fish.name)));
            }
            _useAPIToken = true;
            Logger.Debug($"Use API Token: {_useAPIToken}");
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            GetCurrentMapTime();
            GameService.Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            DrawIcons();
        }

        private double _runningTime;
        protected override async void Update(GameTime gameTime)
        {
            _runningTime += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_runningTime > 3 * 60000)
            {
                // 3 min timer
                _runningTime -= 3 * 60000;
                //ScreenNotification.ShowNotification("The examples module shows this message every 3 min!", ScreenNotification.NotificationType.Warning);
                await getCurrentMapsFish();
                DrawIcons();
            }

            GetCurrentMapTime();
            if (GameService.GameIntegration.Gw2Instance.IsInGame && !GameService.Gw2Mumble.UI.IsMapOpen)
            {
                _timeOfDayPanel.Show();
                _fishPanel.Show();
            }
            else
            {
                _timeOfDayPanel.Hide();
                _fishPanel.Hide();
            }
            if (_draggingTimeOfDayPanel)
            {
                var nOffset = InputService.Input.Mouse.Position - _dragTimeOfDayPanelStart;
                _timeOfDayPanel.Location += nOffset;

                _dragTimeOfDayPanelStart = InputService.Input.Mouse.Position;
            }
            if (_draggingFishPanel)
            {
                var nOffset = InputService.Input.Mouse.Position - _dragFishPanelStart;
                _fishPanel.Location += nOffset;

                _dragFishPanelStart = InputService.Input.Mouse.Position;
            }

        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload here
            _timeOfDayPanel?.Dispose();
            _timeOfDayPanelLoc.SettingChanged -= OnUpdateSettings;
            _dragTimeOfDayPanel.SettingChanged -= OnUpdateSettings;
            _timeOfDayImgWidth.SettingChanged -= OnUpdateSettings;
            _fishPanel?.Dispose();
            _fishPanelLoc.SettingChanged -= OnUpdateSettings;
            _dragFishPanel.SettingChanged -= OnUpdateSettings;
            _fishImgWidth.SettingChanged -= OnUpdateSettings;
            _ignoreCaughtFish.SettingChanged -= OnUpdateFish;
            _includeSaltwater.SettingChanged -= OnUpdateFish;
            _includeWorldClass.SettingChanged -= OnUpdateFish;

            Gw2ApiManager.SubtokenUpdated -= OnApiSubTokenUpdated;

            // All static members must be manually unset
        }

        protected virtual void OnUpdateSettings(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            GetCurrentMapTime();
            DrawIcons();
        }
        protected virtual void OnUpdateSettings(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            GetCurrentMapTime();
            DrawIcons();
        }
        protected virtual void OnUpdateSettings(object sender = null, ValueChangedEventArgs<int> e = null)
        {
            GetCurrentMapTime();
            DrawIcons();
        }

        protected virtual async void OnUpdateFish(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            await getCurrentMapsFish();
            OnUpdateSettings(sender, e);
        }

        protected void DrawIcons()
        {
            _timeOfDayPanel?.Dispose();
            _fishPanel?.Dispose();

            _timeOfDayPanel = new ClickThroughPanel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = _timeOfDayPanelLoc.Value,
                Size = new Point(_timeOfDayImgWidth.Value),
                capture = _dragTimeOfDayPanel.Value
            };
            int imgPadding = 3;
            int fishPanelRows = Clamp((int)Math.Ceiling((double)catchableFish.Count() / 2), 1, 7);
            int fishPanelColumns = Clamp((int)Math.Ceiling((double)catchableFish.Count() / fishPanelRows), 1, 7);
            // swap row column if necessary
            if (fishPanelRows < fishPanelColumns) { int swap = fishPanelRows; fishPanelRows = fishPanelColumns; fishPanelColumns = swap; }
            _fishPanel = new ClickThroughPanel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = _fishPanelLoc.Value,
                Size = new Point(fishPanelColumns * (_fishImgWidth.Value + imgPadding * 2), fishPanelRows * (_fishImgWidth.Value + imgPadding * 2)),
                capture = _dragFishPanel.Value
            };
            Logger.Debug($"Fish Panel Size; Rows: {fishPanelRows} Columns: {fishPanelColumns}, {_fishPanel.Size}");

            // Turned off until can get character fishing details
            //_lure = new ClickThroughImage
            //{
            //    Parent = _fishingPanel,
            //    Texture = _imgLure,
            //    Size = new Point(30, 30),
            //    Location = new Point(0, 0),
            //    Opacity = 1.0f,
            //};

            // Turned off until can get character fishing details
            //_bait = new ClickThroughImage
            //{
            //    Parent = _fishingPanel,
            //    Texture = _imgBait,
            //    Size = new Point(30, 30),
            //    Location = new Point(30, 0),
            //    Opacity = 1.0f,
            //};

            _dawn = new ClickThroughImage
            {
                Parent = _timeOfDayPanel,
                Texture = _imgDawn,
                Size = new Point(_timeOfDayImgWidth.Value),
                Location = new Point(0),
                Opacity = 1.0f,
                BasicTooltipText = "Dawn",
                Visible = timeOfDay == "Dawn",
                capture = _dragTimeOfDayPanel.Value
            };

            _day = new ClickThroughImage
            {
                Parent = _timeOfDayPanel,
                Texture = _imgDay,
                Size = new Point(_timeOfDayImgWidth.Value),
                Location = new Point(0),
                Opacity = 1.0f,
                BasicTooltipText = "Day",
                Visible = timeOfDay == "Day",
                capture = _dragTimeOfDayPanel.Value
            };

            _dusk = new ClickThroughImage
            {
                Parent = _timeOfDayPanel,
                Texture = _imgDusk,
                Size = new Point(_timeOfDayImgWidth.Value),
                Location = new Point(0),
                Opacity = 1.0f,
                BasicTooltipText = "Dusk",
                Visible = timeOfDay == "Dusk",
                capture = _dragTimeOfDayPanel.Value
            };

            _night = new ClickThroughImage
            {
                Parent = _timeOfDayPanel,
                Texture = _imgNight,
                Size = new Point(_timeOfDayImgWidth.Value),
                Location = new Point(0),
                Opacity = 1.0f,
                BasicTooltipText = "Night",
                Visible = timeOfDay == "Night",
                capture = _dragTimeOfDayPanel.Value
            };

            int x = imgPadding; int y = imgPadding; int count = 1;
            foreach (Fish fish in catchableFish)
            {
                string openWater = fish.openWater ? ", Open Water" : "";
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    Texture = GameService.Content.GetRenderServiceTexture(fish.icon),
                    Size = new Point(_fishImgWidth.Value),
                    Location = new Point(x, y),
                    BasicTooltipText = $"{fish.name}\n" +
                                       $"Fishing Hole: {fish.fishingHole}{openWater}\n" +
                                       $"Favored Bait: {fish.bait}\n" +
                                       $"Time of Day: {(fish.timeOfDay == Fish.TimeOfDay.DuskDawn ? "Dusk/Dawn" : fish.timeOfDay.ToString())}\n" +
                                       $"Achievement: {fish.achievement}",
                    ZIndex = 1,
                    capture = _dragFishPanel.Value
                };
                // TODO color border or tooltip text based on rarity
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    Texture = GetImageBorder(fish.rarity),
                    Size = new Point(_fishImgWidth.Value + imgPadding * 2),
                    Location = new Point(x - imgPadding, y - imgPadding),
                    ZIndex = 0,
                    capture = _dragFishPanel.Value
                };
                x += _fishImgWidth.Value + imgPadding;
                if (count == fishPanelColumns) { x = imgPadding; y += _fishImgWidth.Value + imgPadding; count = 0; }
                count++;
            }

            if (_dragTimeOfDayPanel.Value)
            {
                _timeOfDayPanel.capture = true;
                _timeOfDayPanel.LeftMouseButtonPressed += delegate
                {
                    _draggingTimeOfDayPanel = true;
                    _dragTimeOfDayPanelStart = InputService.Input.Mouse.Position;
                    _timeOfDayPanel.ShowTint = true;
                };
                _timeOfDayPanel.LeftMouseButtonReleased += delegate
                {
                    _draggingTimeOfDayPanel = false;
                    _timeOfDayPanelLoc.Value = _timeOfDayPanel.Location;
                    _timeOfDayPanel.ShowTint = false;
                };
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

        private AsyncTexture2D GetImageBorder(string rarity)
        {
            return _imgBorder;
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
            await getCurrentMapsFish();
            DrawIcons();
        }

        private string _timeOfDay = "";
        private string timeOfDay
        {
            get { return _timeOfDay; }
            set
            {
                if (!Equals(_timeOfDay, value))
                {
                    Logger.Debug($"Time of day changed {timeOfDay} -> {value}");
                    _timeOfDay = value;
                    TimeOfDayChanged();
                }
            }
        }
        private async void TimeOfDayChanged()
        {
            await getCurrentMapsFish();
            switch (timeOfDay)
            {
                case "Dawn":
                    _night.Visible = false;
                    _dawn.Visible = true;
                    break;
                case "Day":
                    _dawn.Visible = false;
                    _day.Visible = true;
                    break;
                case "Dusk":
                    _day.Visible = false;
                    _dusk.Visible = true;
                    break;
                case "Night":
                    _dusk.Visible = false;
                    _night.Visible = true;
                    break;
            }
            DrawIcons();
        }

        private void GetCurrentMapTime()
        {
            timeOfDay = TyriaTime.CurrentMapTime(GameService.Gw2Mumble.CurrentMap.Id);
        }

        private async void OnApiSubTokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            if (Gw2ApiManager.HasPermissions(Gw2ApiManager.Permissions) == false)
            {
                Logger.Debug("API permissions are missing");
                _useAPIToken = false;
                Logger.Debug($"Use API Token: {_useAPIToken}");
                return;
            }

            try
            {
                await getCurrentMapsFish();
                DrawIcons();
                _useAPIToken = true;
                Logger.Debug($"Use API Token: {_useAPIToken}");
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
                        accountFishingAchievements = from achievement in accountAchievements where FishingMaps.FISHER_ACHIEVEMENT_IDS.Contains(achievement.Id) && !achievement.Done select achievement;
                        _useAPIToken = true;
                        Logger.Debug($"Use API Token: {_useAPIToken}");

                        // Extra info, probably remove this later
                        var currentAchievementIds = accountFishingAchievements.Select(achievement => achievement.Id);
                        var currentProgress = accountFishingAchievements.Select(achievement => achievement.Current);
                        var progressMax = accountFishingAchievements.Select(achievement => achievement.Max);
                        var currentOfMax = currentProgress.Zip(progressMax, (current, max) => current + "/" + max);
                        Logger.Debug("Fishing achievement Ids: " + string.Join(", ", currentAchievementIds));
                        Logger.Debug("Fishing achievement progress: " + string.Join(", ", currentOfMax));
                        // End Extra info
                    }
                    else
                    {
                        Logger.Debug("API permissions are missing");
                        _useAPIToken = false;
                        Logger.Debug($"Use API Token: {_useAPIToken}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                    _useAPIToken = false;
                    Logger.Debug($"Use API Token: {_useAPIToken}");
                }

                // Refresh catchable fish
                catchableFish.Clear();
                // Achievement Ids from current map
                List<int> achievementsInMap = new List<int>();

                // Get palyer's current map
                if (_currentMap == null)
                {
                    try
                    {
                        _currentMap = await _mapRepository.GetItem(GameService.Gw2Mumble.CurrentMap.Id);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug(ex, "Couldn't get player's current map.");
                        return;
                    }
                }
                if (!fishingMaps.mapAchievements.ContainsKey(_currentMap.Id)) return;

                achievementsInMap.AddRange(fishingMaps.mapAchievements[_currentMap.Id]);
                if (_includeSaltwater.Value) achievementsInMap.AddRange(FishingMaps.SaltwaterFisher);
                if (_includeWorldClass.Value) achievementsInMap.AddRange(FishingMaps.WorldClassFisher);
                Logger.Debug($"All map achievements: {string.Join(", ", achievementsInMap)}");

                if (_ignoreCaughtFish.Value && _useAPIToken)
                {
                    var currentMapAchievable = from achievement in accountFishingAchievements where achievementsInMap.Contains(achievement.Id) select achievement;
                    Logger.Debug($"Current map achieveable: {string.Join(", ", currentMapAchievable.Select(achievement => achievement.Id))}");
                    // Counter to help facilitate ignoring already caught fish
                    int bitsCounter = 0;
                    foreach (AccountAchievement accountAchievement in currentMapAchievable)
                    {
                        Achievement currentAchievement = await RequestAchievement(accountAchievement.Id);
                        if (currentAchievement == null) continue;
                        foreach (AchievementBit bit in currentAchievement.Bits)
                        {
                            if (bit == null) { Logger.Debug($"Bit in {currentAchievement.Id} is null"); continue; }
                            if (accountAchievement.Bits != null && accountAchievement.Bits.Contains(bitsCounter)) { bitsCounter++; continue; }
                            int itemId = ((AchievementItemBit)bit).Id;
                            Item fish = await RequestItem(itemId);
                            // Get first fish in all fish list that matches name
                            var fishNameMatch = _allFishList.Where(phish => phish.name == fish.Name);
                            Fish ghoti = fishNameMatch.Count() != 0 ? fishNameMatch.First() : null;
                            if (ghoti == null) { Logger.Debug($"Missing fish from all fish list: {fish.Name}"); continue; }
                            // TODO option to fade (opacity) as well as remove
                            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
                            if (ghoti.timeOfDay != Fish.TimeOfDay.Any &&
                                !(timeOfDay.Equals("Dawn") || timeOfDay.Equals("Dusk")) &&
                                !Equals(ghoti.timeOfDay.ToString(), timeOfDay))
                            { bitsCounter++; continue; }
                            ghoti.icon = fish.Icon; ghoti.itemId = fish.Id; ghoti.achievementId = currentAchievement.Id;
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
                            if (ghoti == null) { Logger.Debug($"Missing fish from all fish list: {fish.Name}"); continue; }
                            // TODO option to fade (opacity) as well as remove
                            // Filter by time of day if fish's time of day == tyria's time of day. Dawn & Dusk count as Any
                            if (ghoti.timeOfDay != Fish.TimeOfDay.Any &&
                                !(timeOfDay.Equals("Dawn") || timeOfDay.Equals("Dusk")) &&
                                !Equals(ghoti.timeOfDay.ToString(), timeOfDay))
                            { continue; }
                            ghoti.icon = fish.Icon; ghoti.itemId = fish.Id; ghoti.achievementId = currentAchievement.Id;
                            catchableFish.Add(ghoti);
                        }
                    }
                }
                Logger.Debug("Shown catchable fish in current map count: " + catchableFish.Count());
            }
            catch (Exception ex) { throw; }
            finally { _updateFishSemaphore.Release(); }
        }

        // based on https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs
        private async Task<Map> RequestMap(int id)
        {
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
    }
}
