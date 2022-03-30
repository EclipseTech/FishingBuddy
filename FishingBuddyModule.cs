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
using Blish_HUD.Overlay.UI.Views;
using Blish_HUD.Content;

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
        private readonly int FISHING_ACHIEVEMENT_CATEGORY_ID = 317;
        private bool ignoreCaught;
        // Ascalonian Fisher 6330 Avid Ascalonian Fisher 6484
        // Krytan Fisher 6068 Avid Krytan Fisher 6263
        // Maguuma Fisher 6344 Avid Maguuma Fisher 6475
        // Shiverpeaks Fisher 6179 Avid Shiverpeaks Fisher 6153
        // Orrian Fisher 6363 Avid Orrian Fisher 6227
        // Desert Fisher 6317 Avid Desert Fisher 6509
        // Desert Isles Fisher 6106 Avid Desert Isles Fisher 6250
        // Ring of Fire Fisher 6489 Avid Ring of Fire Fisher 6339
        // Seitung Province Fisher 6336 Avid Seitung Province Fisher 6264 
        // Kaineng Fisher 6342 Avid Kaineng Fisher 6192
        // Echovald Wilds Fisher 6258 Avid Echovald Wilds Fisher 6466
        // Dragon's End Fisher 6506 Avid Dragon's End Fisher 6402
        // my current: x6068, x6106, x6179, x6258, x6264, x6317, x6330, (done)6336, x6342, x6344, x6363, x6489, x6506
        // all fishing: 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402
        // Note: Achievements have 'bits' of type 'Item' for the fish "items"
        // All from fishing achievement category 317
        //https://api.guildwars2.com/v2/achievements?ids=6068,6106,6109,6110,6111,6153,6179,6192,6201,6224,6227,6250,6258,6263,6264,6279,6284,6317,6330,6336,6339,6342,6344,6363,6393,6402,6439,6466,6471,6475,6478,6484,6489,6505,6506,6509
        // https://wiki.guildwars2.com/wiki/API:2/account/achievements for each id check 'done' or get progress
        private int[] FISHING_ACHIEVEMENT_IDS = new int[] { 6330, 6484, 6068, 6263, 6344, 6475, 6179, 6153, 6363, 6227, 6317, 6509, 6106, 6250, 6489, 6339, 6336, 6264, 6342, 6192, 6258, 6466, 6506, 6402 };

        // TODO it doesn't seem like achievement or item info from API gives timeofday/bait/fishing hole info...
        //      or achievements give which maps count in which region... 

        // TODO get these via json from fishing achievements
        // TODO Ascalon
        // TODO Krytan
        // TODO Maguuma
        // TODO Shiverpeaks
        // TODO Orrian
        //private int[] DESERT_FISH_IDS = new int[] {96445, 96367, 97744, 97848, 96769, 96724, 97466, 96349, 96308, 96676, 97109, 96428, 96094, 97763, 96854, 97755, 95859, 95608, 97187, 97145, 95929};
        //private int[] DESERT_ISLES_FISH_IDS = new int[] {97369, 96085, 95794, 97756, 97746, 96513, 96225, 95890, 96397, 97844, 97001, 97443, 95849, 97489};
        // TODO Ring of Fire
        //private int[] SEITUNG_FISH_IDS = new int[] {95894, 96350, 97278, 96425, 97604, 95603, 97753, 97865, 96719, 95936, 97692, 97722, 96523, 95926, 96757, 96071, 96944, 97061, 97714, 96318, 97181};
        //private int[] KAINENG_FISH_IDS = new int[] {96297, 97885, 97074, 95875, 96985, 96105, 95609, 97584, 96176, 96942, 96226, 96672, 95843, 97004, 96532, 97121, 96931, 97887, 97479, 97163, 96081};
        //private int[] ECHOVALD_WILDS_FISH_IDS = new int[] {96807, 96195, 96017, 96834, 95861, 95584, 97716, 96096, 95596, 96310, 96792, 97329, 95765, 97559};
        //private int[] DRAGONS_END_FISH_IDS = new int[] {97240, 97814, 97853, 97183, 96443, 96181, 96817, 96913, 95729, 95670, 96076, 97794, 95699, 95632};
        // fish data https://github.com/patrick-petersen/gw2-fishing/blob/4616c7021368b8b9811d4ca441398c2a3cda5697/src/api/FishData.tsx



        // TODO init get fishing achievements to get fish list (how to get this list...?)
        //      GameServices.Gw2Mumble.CurrentMap.CurrentMap
        // TODO show catchable fish list: filter fish https://gitlab.com/Dabsy/gw2fish/-/blob/main/src/helpers/fish.js
        // TODO watch inventory for changes to fish / remove from fish list
        // TODO on map change switch catchable list https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs#L150
        //      GameServices.Gw2Mumble.CurrentMap.OnMapChanged
        // TODO on timeofday change switch catchable list
        // TODO bait & lure icons via api... get bait & lure type/count from api?
        // https://wiki.guildwars2.com/wiki/Jade_Fishing_Lure https://api.guildwars2.com/v2/items?ids=97012&lang=en
        // TODO show zone fishing achievement & fish per zone not caught
        // TODO on fish caught, hide if not hidden
        // TODO get/display equipped lure & bait w/ #s (optional w/ mouseover info)
        // TODO in bounds https://github.com/manlaan/BlishHud-Clock/blob/main/Control/DrawClock.cs#L64
        // TODO display timeofday countdown timer
        // TODO option to show all catchable regardless of achievement & options to include salt water / world class fisher
        //https://github.com/patrick-petersen/gw2-fishing/blob/master/src/api/FishData.tsx
        // TODO later: fish counter (count per rarity & ? count per type of fish ? per zone ? per session ? per hour ?
        // TODO cache fishing images from api
        // TODO notification on dawn https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs#L108 ?15 sec til? 
        //var achieve = await _achievementRepository.GetItem(FISHING_ACHIEVEMENT_CATEGORY_ID);
        // TODO should be caching map info too



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
            ignoreCaught = true;
            //_achievementRepository = new AsyncCache<int, Achievement>(RequestAchivement);
            //_itemRepository = new AsyncCache<int, Item>(RequestItem);
            _imgLure = ContentsManager.GetTexture(@"lure.png");
            _imgBait = ContentsManager.GetTexture(@"bait.png");
            _imgDawn = ContentsManager.GetTexture(@"dawn.png");
            _imgDay = ContentsManager.GetTexture(@"day.png");
            _imgDusk = ContentsManager.GetTexture(@"dusk.png");
            _imgNight = ContentsManager.GetTexture(@"night.png");
            DrawIcons();

            _mySimpleWindowContainer = new MyContainer()
            {
                BackgroundColor = Microsoft.Xna.Framework.Color.Black,
                HeightSizingMode = SizingMode.AutoSize,
                WidthSizingMode = SizingMode.AutoSize,
                Location = new Point(200, 200),
                Parent = GameService.Graphics.SpriteScreen
            };

            _mySecondLabel = new Label() // this label will be used to display the character names requested from the API
            {
                Text = "getting data from api...",
                TextColor = Microsoft.Xna.Framework.Color.DarkGray,
                Font = GameService.Content.DefaultFont32,
                ShowShadow = true,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                Location = new Point(2, 50),
                Parent = _mySimpleWindowContainer
            };
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            DrawIcons();
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

                _dragFishingPanelStart = InputService.Input.Mouse.Position;
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
                Visible = false
            };

            _dawn = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgDawn,
                Size = new Point(30, 30),
                Location = new Point(0, 30),
                Opacity = 1.0f,
                BasicTooltipText = "Dawn",
                Visible = false
            };

            _dusk = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgDusk,
                Size = new Point(30, 30),
                Location = new Point(0, 30),
                Opacity = 1.0f,
                BasicTooltipText = "Dusk",
                Visible = false
            };

            _night = new Image
            {
                Parent = _fishingPanel,
                Texture = _imgNight,
                Size = new Point(30, 30),
                Location = new Point(0, 30),
                Opacity = 1.0f,
                BasicTooltipText = "Night",
                Visible = false,
            };

            // TODO should be able to click through if not dragging (if click doesn't do anything)
            //protected override CaptureType CapturesInput() => CaptureType.DoNotBlock;
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
                    _draggingFishingPanel = true;
                    _dragFishingPanelStart = InputService.Input.Mouse.Position;
                    _fishPanel.ShowTint = true;
                };
                _fishPanel.LeftMouseButtonReleased += delegate {
                    _draggingFishingPanel = false;
                    _fishPanelLoc.Value = _fishPanel.Location;
                    _fishPanel.ShowTint = false;
                };
            }
        }
        
        ////https://github.com/agaertner/Blish-HUD-Modules-Releases/blob/main/Regions%20Of%20Tyria%20Module/RegionsOfTyriaModule.cs
        //private async Task<Map> RequestMap(int id)
        //{
        //    try
        //    {
        //        return await Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(id).ContinueWith(task => task.IsFaulted || !task.IsCompleted ? null : task.Result);
        //    }
        //    catch (Gw2Sharp.WebApi.Exceptions.BadRequestException bre)
        //    {
        //        Logger.Debug(bre.Message);
        //        return null;
        //    }
        //    catch (Gw2Sharp.WebApi.Exceptions.UnexpectedStatusException use)
        //    {
        //        Logger.Debug(use.Message);
        //        return null;
        //    }
        //}

        private string _prevTimeOfDay = "";

        private void TimeOfDay()
        {
            string timeofday = TyriaTime.CurrentMapTime(GameService.Gw2Mumble.CurrentMap.Id);
            switch (timeofday)
            {
                case "Dawn":
                    if (_prevTimeOfDay != timeofday)
                    {
                        _prevTimeOfDay = timeofday;
                        _night.Visible = false;
                        _dawn.Visible = true;
                    }
                    break;
                case "Day":
                    if (_prevTimeOfDay != timeofday)
                    {
                        _prevTimeOfDay = timeofday;
                        _dawn.Visible = false;
                        _day.Visible = true;
                    }
                    break;
                case "Dusk":
                    if (_prevTimeOfDay != timeofday)
                    {
                        _prevTimeOfDay = timeofday;
                        _day.Visible = false;
                        _dusk.Visible = true;
                    }
                    break;
                case "Night":
                    if (_prevTimeOfDay != timeofday)
                    {
                        _prevTimeOfDay = timeofday;
                        _dusk.Visible = false;
                        _night.Visible = true;
                    }
                    break;
            }
        }

        // Some API requests need an api key. e.g. accessing account data like inventory or bank content
        // Blish hud gives you an api subToken you can use instead of the real api key the user entered in blish.
        // But this api subToken may not be available when your module is loaded.
        // Because of that api requests, which require an api key, may fail when they are called in Initialize() or LoadAsync().
        // Or the user can delete the api key or add a new api key with the wrong permissions while your module is already running.
        // You can react to that by subscribing to Gw2ApiManager.SubtokenUpdated. This event will be raised when your module gets the api subToken or
        // when the user adds a new API key.
        private AsyncTexture2D testure;
        private Image _testIcon;
        private async void OnApiSubTokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            // check if api subToken has the permissions you need for your request: Gw2ApiManager.HasPermissions() 
            // Make sure that you added the api key permissions you need in the manifest.json.
            // e.g. the api request further down in this code needs the "characters" permission.
            // You can get the api permissions inside the manifest.json with Gw2ApiManager.Permissions
            // if the Gw2ApiManager.HasPermissions returns false it can also mean, that your module did not get the api subtoken yet or the user removed
            // the api key from blish hud. Because of that it is best practice to call .HasPermissions before every api request which requires an api key
            // and not only rely on Gw2ApiManager.SubtokenUpdated 
            //if (Gw2ApiManager.HasPermissions(new[] { TokenPermission.Account, TokenPermission.Progression }))
            if (Gw2ApiManager.HasPermissions(Gw2ApiManager.Permissions) == false)
            {
                Logger.Debug("API permissions are missing");
                return;
            }

            // even when the api request and api subToken are okay, the api requests can still fail for various reasons.
            // Examples are timeouts or the api is down or the api randomly responds with an error code instead of the correct response.
            // Because of that use try catch when doing api requests to catch api request exceptions.
            // otherwise api request exceptions can crash your module and blish hud.
            try
            {
                // Get all account achievements
                var accountAchievements = await Gw2ApiManager.Gw2ApiClient.V2.Account.Achievements.GetAsync();
                //TODO if an achievement finishes, need to refresh
                // Get just the not done fishing achievements
                var fishingAchievements = from achievement in accountAchievements where FISHING_ACHIEVEMENT_IDS.Contains(achievement.Id) && !achievement.Done select achievement;
                // Extra info, probably remove this later
                var currentAchievementIds = fishingAchievements.Select(achievement => achievement.Id);
                var currentProgress = fishingAchievements.Select(achievement => achievement.Current);
                var progressMax = fishingAchievements.Select(achievement => achievement.Max);
                var currentOfMax = currentProgress.Zip(progressMax, (current, max) => current + "/" + max);
                Logger.Debug("Fishing achievement Ids: " + string.Join(", ", currentAchievementIds));
                Logger.Debug("Fishing achievement progress: " + string.Join(", ", currentOfMax));
                string test = "Fishing achievement Ids: " + string.Join(", ", currentAchievementIds) + "\n";
                test += "Fishing achieve progress: " + string.Join(", ", currentOfMax) + "\n";
                // End Extra info
                // Counter to help facilitate ignoring already caught fish
                int bitsCounter = 0;
                List<Item> uncaughtFish = new List<Item>();
                //TODO filter to zone
                foreach (AccountAchievement achievement in fishingAchievements)
                {
                    Achievement currentAchievement = await RequestAchivement(achievement.Id);
                    foreach (AchievementBit bit in currentAchievement.Bits)
                    {
                        if (bit == null) continue;
                        if (ignoreCaught && achievement.Bits != null && achievement.Bits.Contains(bitsCounter)) { bitsCounter++; continue; }
                        int itemId = ((AchievementItemBit)bit).Id;
                        Logger.Debug("  Item Id: " + itemId);
                        //test += itemId + ", ";
                        Item fish = await RequestItem(itemId);
                        Logger.Debug("Current item: " + itemId + " " + fish.Name + " " + fish.Rarity);
                        test += fish.Name + ":" + fish.Rarity + ", ";
                        uncaughtFish.Add(fish);
                        bitsCounter++;
                    }
                    test += "\n";
                    bitsCounter = 0;
                }
                Logger.Debug("uncaughtFish count: " + uncaughtFish.Count());
                test += "\n";
                test += "uncaughtFish count: " + uncaughtFish.Count() + "\n";
                _mySecondLabel.Text = test;

                // TODO GetHashCode save to mapping or something...
                // TODO save downloaded icons to directory cache & get from cache
                // TODO test icon
                bool pick = false;
                Item firstFish = null;
                foreach (Item fish in uncaughtFish) {
                    if (fish != null)
                    {
                        Logger.Debug("    icon: " + fish.Icon);
                        if (!pick) { pick = true;  firstFish = fish; }
                    }
                    else Logger.Debug("null in fish list");
                }
                if (firstFish != null)
                {
                    testure = GameService.Content.GetRenderServiceTexture(firstFish.Icon);
                    Logger.Debug("first fish: " + firstFish.Icon + " " + firstFish.ChatLink);
                    _testIcon = new Image
                    {
                        Parent = _fishPanel,
                        Texture = testure,
                        Size = new Point(30, 30),
                        Location = new Point(0, 0),
                        Opacity = 1.0f,
                        Visible = true
                    };
                }
            }
            catch (Exception)
            {
                // this is just an example for logging.
                // You do not have to log api response exception. Just make sure that your module has no issue with failing api requests
                Logger.Debug("Failed to get info from api.");
            }
        }

        private async Task<Achievement> RequestAchivement(int id)
        {
            Achievement achievement = null;
            Task<Achievement> achievementTask = Gw2ApiManager.Gw2ApiClient.V2.Achievements.GetAsync(id);
            try
            {
                // TODO instead of await each call. queue/addtolist each task, Task.WaitAll(queue/list), requeue nulls?
                await RetryHelper.RetryAsync(5, achievementTask, async () => { achievement = await achievementTask.ContinueWith(task => task.IsFaulted || !task.IsCompleted ? null : task.Result); }, () => achievementTask != null && achievement != null && achievementTask.Result != null);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
            return achievement;
        }

        private async Task<Item> RequestItem(int id)
        {
            Item item = null;
            Task<Item> itemTask = Gw2ApiManager.Gw2ApiClient.V2.Items.GetAsync(id);
            try
            {
                await RetryHelper.RetryAsync(5, itemTask, async () => { item = await itemTask.ContinueWith(task => task.IsFaulted || !task.IsCompleted ? null : task.Result); }, () => itemTask != null && item != null && itemTask.Result != null);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to query Guild Wars 2 API.");
                return null;
            }
            return item;
        }

        private MyContainer _mySimpleWindowContainer;
        private Label _mySecondLabel;
        // Load content and more here. This call is asynchronous, so it is a good time to run
        // any long running steps for your module including loading resources from file or ref.
        protected override async Task LoadAsync()
        {
            // Get your manifest registered directories with the DirectoriesManager
            foreach (string directoryName in this.DirectoriesManager.RegisteredDirectories)
            {
                string fullDirectoryPath = DirectoriesManager.GetFullDirectoryPath(directoryName);
                var allFiles = Directory.EnumerateFiles(fullDirectoryPath, "*", SearchOption.AllDirectories).ToList();

                // example of how to log something in the blishhud.XXX-XXX.log file in %userprofile%\Documents\Guild Wars 2\addons\blishhud\logs
                Logger.Info($"'{directoryName}' can be found at '{fullDirectoryPath}' and has {allFiles.Count} total files within it.");
            }
        }
    }
}
