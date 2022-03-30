using Blish_HUD;
using Blish_HUD.Controls;
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
using System.IO;

namespace Eclipse1807.BlishHUD.FishingBuddy
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class FishingBuddyModule : Blish_HUD.Modules.Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(FishingBuddyModule));

        internal static FishingBuddyModule ModuleInstance;

        private Texture2D _imgLure;
        private Texture2D _imgBait;
        private Texture2D _imgDawn;
        private Texture2D _imgDay;
        private Texture2D _imgDusk;
        private Texture2D _imgNight;

        private Image _lure;
        private Image _bait;
        private Image _dawn;
        private Image _day;
        private Image _dusk;
        private Image _night;

        //https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/Utils/AsyncCache.cs
        private AsyncCache<int, Map> _mapRepository;
        //private AsyncCache<int, Achievement> _achievementRepository;
        //private AsyncCache<int, Item> _itemRepository;

        private Panel _fishingPanel;
        private bool _draggingFishingPanel;
        private Point _dragFishingPanelStart = Point.Zero;
        public static SettingEntry<bool> _dragFishingPanel;
        private SettingEntry<Point> _fishingPanelLoc;
        private Panel _fishPanel;
        private bool _draggingFishPanel;
        private Point _dragFishPanelStart = Point.Zero;
        public static SettingEntry<bool> _dragFishPanel;
        private SettingEntry<Point> _fishPanelLoc;
        public static SettingEntry<bool> _ignoreCaughtFish;
        private bool ignoreCaught;
        private List<Item> uncaughtFish;
        private FishingMaps fishingMaps;
        private IEnumerable<AccountAchievement> accountFishingAchievements;
        // TODO load fish from json?
        //IList<Fish> fishList = JsonConvert.DeserializeObject<dynamic>(@"ref/fishList.json");
        // all fishing: 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402, 6224, 6110, 6471, 6393
        //https://api.guildwars2.com/v2/achievements?ids=6068,6106,6109,6110,6111,6153,6179,6192,6201,6224,6227,6250,6258,6263,6264,6279,6284,6317,6330,6336,6339,6342,6344,6363,6393,6402,6439,6466,6471,6475,6478,6484,6489,6505,6506,6509

        // https://wiki.guildwars2.com/wiki/API:2/account/achievements
        private readonly int[] FISHING_ACHIEVEMENT_IDS = new int[] { 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402, 6224, 6110, 6471, 6393 };

        // TODO it doesn't seem like achievement or item info from API gives timeofday/bait/fishing hole info...
        //      or achievements give which maps count in which region... make data classes & mappings?
        // TODO should other map Ids not show any fishing info, or show open water info? or saltwater/world class info?
        // TODO refresh catchable list on timer? or remove caught in zone somehow?
        // TODO watch inventory for changes to fish / remove from fish list?
        // TODO on timeofday change switch catchable list
        // TODO BLOCKED get/display equipped lure & bait w/ #s (optional w/ mouseover info)
        // TODO BLOCKED bait & lure icons via api... get bait & lure type/count from api? is this even detailed anywhere? or no api yet for this?
        // TODO cache fishing images from api
        // TODO should be caching map info too
        // TODO in bounds https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L64
        // TODO display timeofday countdown timer
        // TODO  options to include salt water / world class fisher
        // TODO notifications? on dawn https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs#L108 ?15 sec til?
        // TODO later: caught fish counter (count per rarity & ? count per type of fish ? per zone ? per session ? per hour ?)


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
            _ignoreCaughtFish = settings.DefineSetting("IgnoreCaughtFish", true, "Ignore Caught Fish", "Ignore fish already counted towards achievements");
            _fishingPanelLoc = settings.DefineSetting("FishingPanelLoc", new Point(100, 100), "Fishing Details Location", "");
            _fishingPanelLoc.SettingChanged += UpdateSettings;
            _dragFishingPanel = settings.DefineSetting("FishingPanelDrag", false, "Drag Fishing Details", "");
            _dragFishingPanel.SettingChanged += UpdateSettings;
            _fishPanelLoc = settings.DefineSetting("FishPanelLoc", new Point(160, 100), "Fish Location", "");
            _fishPanelLoc.SettingChanged += UpdateSettings;
            _dragFishPanel = settings.DefineSetting("FishPanelDrag", false, "Drag Fish", "");
            _dragFishPanel.SettingChanged += UpdateSettings;
        }

        protected override void Initialize()
        {
            Gw2ApiManager.SubtokenUpdated += OnApiSubTokenUpdated;
            ignoreCaught = _ignoreCaughtFish.Value;
            uncaughtFish = new List<Item>();
            fishingMaps = new FishingMaps();
            _mapRepository = new AsyncCache<int, Map>(RequestMap);
            //_achievementRepository = new AsyncCache<int, Achievement>(RequestAchivement);
            //_itemRepository = new AsyncCache<int, Item>(RequestItem);
            _imgLure = ContentsManager.GetTexture(@"lure.png");
            _imgBait = ContentsManager.GetTexture(@"bait.png");
            _imgDawn = ContentsManager.GetTexture(@"dawn.png");
            _imgDay = ContentsManager.GetTexture(@"day.png");
            _imgDusk = ContentsManager.GetTexture(@"dusk.png");
            _imgNight = ContentsManager.GetTexture(@"night.png");
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            GameService.Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            DrawIcons();
            TimeOfDay();
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        //private double _runningTime;
        protected override void Update(GameTime gameTime)
        {
            //_runningTime += gameTime.ElapsedGameTime.TotalMilliseconds;
            //
            //if (_runningTime > 60000)
            //{
            //    _runningTime -= 60000;
            //    ScreenNotification.ShowNotification("The examples module shows this message every 60 seconds!", ScreenNotification.NotificationType.Warning);
            //}

            // TODO this probably doesn't need on every update?
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
            _fishPanel?.Dispose();
            _fishPanelLoc.SettingChanged -= UpdateSettings;
            _dragFishPanel.SettingChanged -= UpdateSettings;

            Gw2ApiManager.SubtokenUpdated -= OnApiSubTokenUpdated;

            // All static members must be manually unset
        }

        private void UpdateSettings(object sender = null, ValueChangedEventArgs<Point> e = null)
        {
            DrawIcons();
        }
        private void UpdateSettings(object sender = null, ValueChangedEventArgs<bool> e = null)
        {
            DrawIcons();
        }


        protected void DrawIcons()
        {
            _fishingPanel?.Dispose();
            _fishPanel?.Dispose();

            _fishingPanel = new Panel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = _fishingPanelLoc.Value,
                Size = new Point(60, 60),
            };
            _fishPanel = new Panel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = _fishPanelLoc.Value,
                Size = new Point(90, 210),
            };

            _lure = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgLure,
                Size = new Point(30, 30),
                Location = new Point(0, 0),
                Opacity = 1.0f,
            };

            _bait = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgBait,
                Size = new Point(30, 30),
                Location = new Point(30, 0),
                Opacity = 1.0f,
            };

            _day = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgDay,
                Size = new Point(30, 30),
                Location = new Point(0, 30),
                Opacity = 1.0f,
                BasicTooltipText = "Day",
                Visible = _prevTimeOfDay == "Day"
            };

            _dawn = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgDawn,
                Size = new Point(30, 30),
                Location = new Point(0, 30),
                Opacity = 1.0f,
                BasicTooltipText = "Dawn",
                Visible = _prevTimeOfDay == "Dawn"
            };

            _dusk = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgDusk,
                Size = new Point(30, 30),
                Location = new Point(0, 30),
                Opacity = 1.0f,
                BasicTooltipText = "Dusk",
                Visible = _prevTimeOfDay == "Dusk"
            };

            _night = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgNight,
                Size = new Point(30, 30),
                Location = new Point(0, 30),
                Opacity = 1.0f,
                BasicTooltipText = "Night",
                Visible = _prevTimeOfDay == "Night"
            };

            // TODO should be able to click through if not dragging (if click doesn't do anything)
            //protected override CaptureType CapturesInput() => CaptureType.DoNotBlock; or something
            if (_dragFishingPanel.Value)
            {
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

        private int _prevMapId;
        private async void OnMapChanged(object o, ValueEventArgs<int> e)
        {
            Logger.Debug("Map Changed");
            Map currentMap = await _mapRepository.GetItem(e.Value);
            if (currentMap == null || currentMap.Id == _prevMapId)
                return;
        
            _prevMapId = currentMap.Id;

            //TODO do something with this info, reload fish list
            //var mapName = currentMap.Name;
            //var mapRegion = currentMap.RegionName;
            //var mapId = currentMap.Id;
            Logger.Debug($"Current map {currentMap.Name} {currentMap.Id}");

            await getCurrentMapsFish();
        }

        private string _prevTimeOfDay = "";
        private void TimeOfDay()
        {
            string timeofday = TyriaTime.CurrentMapTime(GameService.Gw2Mumble.CurrentMap.Id);
            if (_prevTimeOfDay == timeofday) return;
            switch (timeofday)
            {
                case "Dawn":
                    _prevTimeOfDay = timeofday;
                    _night.Visible = false;
                    _dawn.Visible = true;
                    break;
                case "Day":
                    _prevTimeOfDay = timeofday;
                    _dawn.Visible = false;
                    _day.Visible = true;
                    break;
                case "Dusk":
                    _prevTimeOfDay = timeofday;
                    _day.Visible = false;
                    _dusk.Visible = true;
                    break;
                case "Night":
                    _prevTimeOfDay = timeofday;
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

                // TODO save downloaded icons to directory cache & get from cache
                // TODO Download / Use Icon w/ GetRenderServiceTexture ex: GameService.Content.GetRenderServiceTexture(fish.Icon);
                foreach (Item fish in uncaughtFish)
                {
                    if (fish != null)
                    {
                        Logger.Debug("    icon: " + fish.Icon);
                    }
                    else Logger.Debug("null in fish list");
                }
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
            accountFishingAchievements = from achievement in accountAchievements where FISHING_ACHIEVEMENT_IDS.Contains(achievement.Id) && !achievement.Done select achievement;

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
            uncaughtFish.Clear();
            // get palyer's current map
            Map currentMap = await _mapRepository.GetItem(GameService.Gw2Mumble.CurrentMap.Id);
            // Achievement Ids from current map
            List<int> achievementsInMap = fishingMaps.mapAchievements[currentMap.Id];
            // if (includeWorldClass) achievementsInMap.AddRange(FishingMaps.SaltwaterFisher);
            // if (includeSaltwater) achievementsInMap.AddRange(FishingMaps.WorldClassFisher);
            var currentMapAchievable = from achievement in accountFishingAchievements where achievementsInMap.Contains(achievement.Id) select achievement;
            foreach (AccountAchievement achievement in currentMapAchievable)
            {
                Achievement currentAchievement = await RequestAchievement(achievement.Id);
                // TODO fix bug when currentAchievement is null... RequestAchievement doesn't seem to be retrying... none of these achievements should be null
                if (currentAchievement == null) continue;
                foreach (AchievementBit bit in currentAchievement.Bits)
                {
                    if (bit == null) continue;
                    if (ignoreCaught && achievement.Bits != null && achievement.Bits.Contains(bitsCounter)) { bitsCounter++; continue; }
                    int itemId = ((AchievementItemBit)bit).Id;
                    Logger.Debug("  Item Id: " + itemId);
                    Item fish = await RequestItem(itemId);
                    Logger.Debug("Current item: " + itemId + " " + fish.Name + " " + fish.Rarity);
                    uncaughtFish.Add(fish);
                    bitsCounter++;
                }
                bitsCounter = 0;
            }
            Logger.Debug("Uncaught fish in current map count: " + uncaughtFish.Count());
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
