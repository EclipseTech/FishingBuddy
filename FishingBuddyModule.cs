using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Eclipse1807.BlishHUD.FishingBuddy.Utils;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

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

        //https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/Utils/AsyncCache.cs
        private AsyncCache<int, Map> _mapRepository;
        //private AsyncCache<int, Achievement> _achievementRepository;
        //private AsyncCache<int, Item> _itemRepository;

        private ClickThroughPanel _fishingPanel;
        private bool _draggingFishingPanel;
        private Point _dragFishingPanelStart = Point.Zero;
        private ClickThroughPanel _fishPanel;
        private bool _draggingFishPanel;
        private Point _dragFishPanelStart = Point.Zero;
        private static SettingEntry<Point> _fishPanelLoc;
        private static SettingEntry<Point> _fishingPanelLoc;
        public static SettingEntry<bool> _dragFishingPanel;
        public static SettingEntry<int> _fishingImgWidth;
        public static SettingEntry<bool> _dragFishPanel;
        public static SettingEntry<int> _fishImgWidth;
        public static SettingEntry<bool> _ignoreCaughtFish;
        public static SettingEntry<bool> _includeWorldClass;
        public static SettingEntry<bool> _includeSaltwater;
        private List<Fish> catchableFish;
        private FishingMaps fishingMaps;
        private IEnumerable<AccountAchievement> accountFishingAchievements;

        // https://wiki.guildwars2.com/wiki/API:2/account/achievements
        private readonly int[] FISHER_ACHIEVEMENT_IDS = new int[] { 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402, 6224, 6110, 6471, 6393 };

        private static List<Fish> _fishList;


        // TODO fix issue in fish.json location": "\"Draconis Mons", "achievement": " Fireheart Rise\"",
        // TODO make API permissions optional, it should still show current map available based on timeofday ignoring achievement progress
        // TODO on timeofday change switch catchable list
        // TODO refresh catchable list on timer? or remove caught in zone somehow?
        // TODO should other map Ids not show any fishing info, or show open water info? or saltwater/world class info?
        // TODO watch inventory for changes to fish / remove from fish list?
        // TODO cache fishing images from api
        // TODO should be caching map info too
        // TODO in bounds checking for UI elemends, ex: https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L64
        // TODO display timeofday countdown timer
        // TODO notifications? on dawn https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs#L108 ?15 sec til?
        // TODO add item id to fish.json
        // TODO add achievement id to fish.json
        // TODO Add caught fish counter (count per rarity & ? count per type of fish ? per zone ? per session ? per hour ?)
        // TODO BLOCKED get/display equipped lure & bait w/ #s (optional w/ mouseover info)
        // TODO BLOCKED bait & lure icons via api... get bait & lure type/count from api? is this even detailed anywhere? or no api yet for this?


        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public FishingBuddyModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void DefineSettings(SettingCollection settings)
        {
            _ignoreCaughtFish = settings.DefineSetting("IgnoreCaughtFish", true, () => "Ignore Caught Fish", () => "Ignore fish already counted towards achievements");
            _ignoreCaughtFish.SettingChanged += UpdateSettings;
            _includeSaltwater = settings.DefineSetting("IncludeSaltwater", false, () => "Include Saltwater Fish", () => "Include Saltwater Fisher fish");
            _includeSaltwater.SettingChanged += UpdateSettings;
            _includeWorldClass = settings.DefineSetting("IncludeWorldClass", false, () => "Include World Class Fish", () => "Include World Class Fisher fish");
            _includeWorldClass.SettingChanged += UpdateSettings;
            _fishingPanelLoc = settings.DefineSetting("FishingPanelLoc", new Point(100, 100), () => "Fishing Details Location", () => "");
            _fishingPanelLoc.SettingChanged += UpdateSettings;
            _dragFishingPanel = settings.DefineSetting("FishingPanelDrag", false, () => "Drag Fishing Details", () => "");
            _dragFishingPanel.SettingChanged += UpdateSettings;
            _fishingImgWidth = settings.DefineSetting("FishingImgWidth", 30, () => "Width", () => "");
            _fishingImgWidth.SetRange(16, 96);
            _fishingImgWidth.SettingChanged += UpdateSettings;
            _fishPanelLoc = settings.DefineSetting("FishPanelLoc", new Point(160, 100), () => "Fish Location", () => "");
            _fishPanelLoc.SettingChanged += UpdateSettings;
            _dragFishPanel = settings.DefineSetting("FishPanelDrag", false, () => "Drag Fish", () => "");
            _dragFishPanel.SettingChanged += UpdateSettings;
            _fishImgWidth = settings.DefineSetting("FishImgWidth", 30, () => "Width", () => "");
            _fishImgWidth.SetRange(16, 96);
            _fishImgWidth.SettingChanged += UpdateSettings;
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

            _fishList = new List<Fish>();

            using (StreamReader r = new StreamReader(ContentsManager.GetFileStream(@"fish.json")))
            {
                string json = r.ReadToEnd();
                _fishList.AddRange(JsonConvert.DeserializeObject<List<Fish>>(json));
                Logger.Debug("fish list: " + string.Join(", ", _fishList.Select(fish => fish.name)));
            }
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);
            
            TimeOfDay();
            GameService.Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            DrawIcons();
        }

        //private double _runningTime;
        protected override void Update(GameTime gameTime)
        {
            //_runningTime += gameTime.ElapsedGameTime.TotalMilliseconds;
            //if (_runningTime > 30000)
            //{
            //    _runningTime -= 30000;
            //    //ScreenNotification.ShowNotification("The examples module shows this message every 60 seconds!", ScreenNotification.NotificationType.Warning);
            //    DrawIcons();
            //}

            TimeOfDay();
            if (GameService.GameIntegration.Gw2Instance.IsInGame && !GameService.Gw2Mumble.UI.IsMapOpen)
            {
                _fishingPanel.Show();
                _fishPanel.Show();
            }
            else
            {
                _fishingPanel.Hide();
                _fishPanel.Hide();
            }
            if (_draggingFishingPanel)
            {
                var nOffset = InputService.Input.Mouse.Position - _dragFishingPanelStart;
                _fishingPanel.Location += nOffset;
            
                _dragFishingPanelStart = InputService.Input.Mouse.Position;
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
            _fishingPanel?.Dispose();
            _fishingPanelLoc.SettingChanged -= UpdateSettings;
            _dragFishingPanel.SettingChanged -= UpdateSettings;
            _fishingImgWidth.SettingChanged -= UpdateSettings;
            _fishPanel?.Dispose();
            _fishPanelLoc.SettingChanged -= UpdateSettings;
            _dragFishPanel.SettingChanged -= UpdateSettings;
            _fishImgWidth.SettingChanged -= UpdateSettings;
            _includeSaltwater.SettingChanged -= UpdateSettings;
            _includeWorldClass.SettingChanged -= UpdateSettings;
            _ignoreCaughtFish.SettingChanged -= UpdateSettings;

            Gw2ApiManager.SubtokenUpdated -= OnApiSubTokenUpdated;

            // All static members must be manually unset
        }

        // TODO update current map fish on setting change await getCurrentMapsFish();
        private void UpdateSettings(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            TimeOfDay();
            DrawIcons();
        }
        private void UpdateSettings(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            TimeOfDay();
            DrawIcons();
        }
        private void UpdateSettings(object sender = null, ValueChangedEventArgs<int> e = null)
        {
            TimeOfDay();
            DrawIcons();
        }


        protected void DrawIcons()
        {
            _fishingPanel?.Dispose();
            _fishPanel?.Dispose();

            _fishingPanel = new ClickThroughPanel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = _fishingPanelLoc.Value,
                Size = new Point(_fishingImgWidth.Value),
                capture = _dragFishingPanel.Value
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
            Logger.Debug($"Rows: {fishPanelRows} Columns: {fishPanelColumns}, {_fishPanel.Size}");

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
                Parent = _fishingPanel,
                Texture = _imgDawn,
                Size = new Point(_fishingImgWidth.Value),
                Location = new Point(0),
                Opacity = 1.0f,
                BasicTooltipText = "Dawn",
                Visible = _prevTimeOfDay == "Dawn",
                capture = _dragFishingPanel.Value
            };

            _day = new ClickThroughImage
            {
                Parent = _fishingPanel,
                Texture = _imgDay,
                Size = new Point(_fishingImgWidth.Value),
                Location = new Point(0),
                Opacity = 1.0f,
                BasicTooltipText = "Day",
                Visible = _prevTimeOfDay == "Day",
                capture = _dragFishingPanel.Value
            };

            _dusk = new ClickThroughImage
            {
                Parent = _fishingPanel,
                Texture = _imgDusk,
                Size = new Point(_fishingImgWidth.Value),
                Location = new Point(0),
                Opacity = 1.0f,
                BasicTooltipText = "Dusk",
                Visible = _prevTimeOfDay == "Dusk",
                capture = _dragFishingPanel.Value
            };

            _night = new ClickThroughImage
            {
                Parent = _fishingPanel,
                Texture = _imgNight,
                Size = new Point(_fishingImgWidth.Value),
                Location = new Point(0),
                Opacity = 1.0f,
                BasicTooltipText = "Night",
                Visible = _prevTimeOfDay == "Night",
                capture = _dragFishingPanel.Value
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
                    BasicTooltipText = $"{fish.name}\nFishing Hole: {fish.fishingHole}{openWater}\nFavored Bait: {fish.bait}\nTime of Day: {fish.timeOfDay.ToString()}\nAchievement: {fish.achievement}",
                    ZIndex = 1,
                    capture = _dragFishPanel.Value
                };
                // TODO color border or tooltip text based on rarity
                new ClickThroughImage
                {
                    Parent = _fishPanel,
                    Texture = _imgBorder,
                    Size = new Point(_fishImgWidth.Value + imgPadding*2),
                    Location = new Point(x - imgPadding, y - imgPadding),
                    ZIndex = 0,
                    capture = _dragFishPanel.Value
                };
                x += _fishImgWidth.Value + imgPadding;
                if (count == fishPanelColumns) { x = imgPadding; y += _fishImgWidth.Value + imgPadding; count = 0; }
                count++;
            }

            if (_dragFishingPanel.Value)
            {
                _fishingPanel.capture = true;
                _fishingPanel.LeftMouseButtonPressed += delegate {
                    _draggingFishingPanel = true;
                    _dragFishingPanelStart = InputService.Input.Mouse.Position;
                    _fishingPanel.ShowTint = true;
                };
                _fishingPanel.LeftMouseButtonReleased += delegate {
                    _draggingFishingPanel = false;
                    _fishingPanelLoc.Value = _fishingPanel.Location;
                    _fishingPanel.ShowTint = false;
                };
            }
            if (_dragFishPanel.Value)
            {
                _fishPanel.capture = true;
                _fishPanel.LeftMouseButtonPressed += delegate {
                    _draggingFishPanel = true;
                    _dragFishPanelStart = InputService.Input.Mouse.Position;
                    _fishPanel.ShowTint = true;
                };
                _fishPanel.LeftMouseButtonReleased += delegate {
                    _draggingFishPanel = false;
                    _fishPanelLoc.Value = _fishPanel.Location;
                    _fishPanel.ShowTint = false;
                };
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
            Map currentMap = await _mapRepository.GetItem(e.Value);
            Logger.Debug($"Current map {currentMap.Name} {currentMap.Id}");
            if (currentMap == null || currentMap.Id == _prevMapId)
                return;
        
            _prevMapId = currentMap.Id;

            await getCurrentMapsFish();
            DrawIcons();
        }

        private string _prevTimeOfDay = "";
        private string _currTimeOfDay = "";
        private void TimeOfDay()
        {
            _currTimeOfDay = TyriaTime.CurrentMapTime(GameService.Gw2Mumble.CurrentMap.Id);
            //TODO notify event time of day changed to update catchable fish list
            if (_prevTimeOfDay == _currTimeOfDay) return;
            switch (_currTimeOfDay)
            {
                case "Dawn":
                    _prevTimeOfDay = _currTimeOfDay;
                    _night.Visible = false;
                    _dawn.Visible = true;
                    break;
                case "Day":
                    _prevTimeOfDay = _currTimeOfDay;
                    _dawn.Visible = false;
                    _day.Visible = true;
                    break;
                case "Dusk":
                    _prevTimeOfDay = _currTimeOfDay;
                    _day.Visible = false;
                    _dusk.Visible = true;
                    break;
                case "Night":
                    _prevTimeOfDay = _currTimeOfDay;
                    _dusk.Visible = false;
                    _night.Visible = true;
                    break;
            }
        }

        private async void OnApiSubTokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            //if (Gw2ApiManager.HasPermissions(new[] { TokenPermission.Account, TokenPermission.Progression }))
            if (Gw2ApiManager.HasPermissions(Gw2ApiManager.Permissions) == false)
            {
                Logger.Debug("API permissions are missing");
                return;
            }

            try
            {
                await getCurrentMapsFish();
                DrawIcons();
            }
            catch (Exception)
            {
                Logger.Debug("Failed to get info from api.");
            }
        }

        private async Task getCurrentMapsFish()
        {
            // Get all account achievements
            var accountAchievements = await Gw2ApiManager.Gw2ApiClient.V2.Account.Achievements.GetAsync();
            //TODO if a fish is caught or an achievement finishes, need to refresh lists
            // Get just the not done fishing achievements
            accountFishingAchievements = from achievement in accountAchievements where FISHER_ACHIEVEMENT_IDS.Contains(achievement.Id) && !achievement.Done select achievement;

            // Extra info, probably remove this later
            var currentAchievementIds = accountFishingAchievements.Select(achievement => achievement.Id);
            var currentProgress = accountFishingAchievements.Select(achievement => achievement.Current);
            var progressMax = accountFishingAchievements.Select(achievement => achievement.Max);
            var currentOfMax = currentProgress.Zip(progressMax, (current, max) => current + "/" + max);
            Logger.Debug("Fishing achievement Ids: " + string.Join(", ", currentAchievementIds));
            Logger.Debug("Fishing achievement progress: " + string.Join(", ", currentOfMax));
            // End Extra info

            // Counter to help facilitate ignoring already caught fish
            int bitsCounter = 0;
            // Refresh uncaught fish
            catchableFish.Clear();
            // get palyer's current map
            Map currentMap = await _mapRepository.GetItem(GameService.Gw2Mumble.CurrentMap.Id);
            if (!fishingMaps.mapAchievements.ContainsKey(currentMap.Id)) return;
            // Achievement Ids from current map
            List<int> achievementsInMap = fishingMaps.mapAchievements[currentMap.Id];
            if (_includeSaltwater.Value) achievementsInMap.AddRange(fishingMaps.SaltwaterFisher);
            if (_includeWorldClass.Value) achievementsInMap.AddRange(fishingMaps.WorldClassFisher);
            var currentMapAchievable = from achievement in accountFishingAchievements where achievementsInMap.Contains(achievement.Id) select achievement;
            Logger.Debug($"Current map achieveable: {string.Join(", ", currentMapAchievable.Select(achievement => achievement.Id))}");
            foreach (AccountAchievement achievement in currentMapAchievable)
            {
                Achievement currentAchievement = await RequestAchievement(achievement.Id);
                // TODO fix bug when currentAchievement is null... RequestAchievement doesn't seem to be retrying... none of these achievements should be null
                if (currentAchievement == null) continue;
                foreach (AchievementBit bit in currentAchievement.Bits)
                {
                    if (bit == null) continue;
                    if (_ignoreCaughtFish.Value && achievement.Bits != null && achievement.Bits.Contains(bitsCounter)) { bitsCounter++; continue; }
                    int itemId = ((AchievementItemBit)bit).Id;
                    Logger.Debug("  Item Id: " + itemId);
                    Item fish = await RequestItem(itemId);
                    Logger.Debug("Current item: " + itemId + " " + fish.Name + " " + fish.Rarity);
                    // Filter by time of day
                    Fish ghoti = _fishList.Where(phish => phish.name == fish.Name).First();
                    // TODO option to fade (opacity) vs remove
                    if (ghoti.timeOfDay != Fish.TimeOfDay.Any && !ghoti.timeOfDay.ToString().Equals(_currTimeOfDay)) { bitsCounter++; continue; }
                    ghoti.icon = fish.Icon; ghoti.itemId = fish.Id; ghoti.achievementId = achievement.Id;
                    catchableFish.Add(ghoti);
                    bitsCounter++;
                }
                bitsCounter = 0;
            }
            Logger.Debug("Uncaught fish in current map count: " + catchableFish.Count());

            // TODO save downloaded icons to directory cache & get from cache
            // TODO Download / Use Icon w/ GetRenderServiceTexture ex: GameService.Content.GetRenderServiceTexture(fish.Icon);
            //foreach (Item fish in catchableFish)
            //{
            //    if (fish == null) continue;
            //    Logger.Debug("    icon: " + fish.Icon);
            //}
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
