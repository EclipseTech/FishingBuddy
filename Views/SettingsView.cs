namespace Eclipse1807.BlishHUD.FishingBuddy.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Settings.UI.Views;
    using Microsoft.Xna.Framework;

    public class SettingsView : View
    {
        protected override void Build(Container buildPanel)
        {
            Panel parentPanel = new Panel()
            {
                Parent = buildPanel,
                Height = buildPanel.Height,
                HeightSizingMode = SizingMode.AutoSize,
                WidthSizingMode = SizingMode.AutoSize
                //Width = 700,
            };

            IView settingFishCaught_View = SettingView.FromType(FishingBuddyModule._ignoreCaughtFish, buildPanel.Width);
            ViewContainer settingFishCaught_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, 10),
                Parent = parentPanel
            };
            settingFishCaught_Container.Show(settingFishCaught_View);

            IView settingFishWorldClass_View = SettingView.FromType(FishingBuddyModule._includeWorldClass, buildPanel.Width);
            ViewContainer settingFishWorldClass_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(160, settingFishCaught_Container.Location.Y),
                Parent = parentPanel
            };
            settingFishWorldClass_Container.Show(settingFishWorldClass_View);

            IView settingFishSaltwater_View = SettingView.FromType(FishingBuddyModule._includeSaltwater, buildPanel.Width);
            ViewContainer settingFishSaltwater_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(310, settingFishCaught_Container.Location.Y),
                Parent = parentPanel
            };
            settingFishSaltwater_Container.Show(settingFishSaltwater_View);

            Label settingFishPanelOrientation_Label = new Label()
            {
                Location = new Point(470, settingFishCaught_Container.Location.Y),
                Width = 75,
                AutoSizeHeight = false,
                WrapText = false,
                Parent = parentPanel,
                Text = $"{Properties.Strings.Orientation}: ",
            };
            Dropdown settingFishPanelOrientation_Dropdown = new Dropdown()
            {
                Location = new Point(settingFishPanelOrientation_Label.Right + 8, settingFishPanelOrientation_Label.Top - 4),
                Width = 100,
                Parent = parentPanel,
            };
            foreach (string s in FishingBuddyModule._fishPanelOrientations)
            {
                settingFishPanelOrientation_Dropdown.Items.Add(Properties.Strings.ResourceManager.GetString(s, Properties.Strings.Culture));
            }
            settingFishPanelOrientation_Dropdown.SelectedItem = FishingBuddyModule._fishPanelOrientation.Value;
            settingFishPanelOrientation_Dropdown.ValueChanged += delegate
            {
                FishingBuddyModule._fishPanelOrientation.Value = settingFishPanelOrientation_Dropdown.SelectedItem;
            };

            IView settingFishDrag_View = SettingView.FromType(FishingBuddyModule._dragFishPanel, buildPanel.Width);
            ViewContainer settingFishDrag_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingFishCaught_Container.Bottom + 5),
                Parent = parentPanel
            };
            settingFishDrag_Container.Show(settingFishDrag_View);

            IView settingFishRarity_View = SettingView.FromType(FishingBuddyModule._showRarityBorder, buildPanel.Width);
            ViewContainer settingFishRarity_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(160, settingFishCaught_Container.Bottom + 5),
                Parent = parentPanel
            };
            settingFishRarity_Container.Show(settingFishRarity_View);

            IView settingFishUncatchable_View = SettingView.FromType(FishingBuddyModule._displayUncatchableFish, buildPanel.Width);
            ViewContainer settingFishUncatchable_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(310, settingFishCaught_Container.Bottom + 5),
                Parent = parentPanel
            };
            settingFishUncatchable_Container.Show(settingFishUncatchable_View);

            Label settingFishPanelDirection_Label = new Label()
            {
                Location = new Point(470, settingFishCaught_Container.Bottom + 5),
                Width = 75,
                AutoSizeHeight = false,
                WrapText = false,
                Parent = parentPanel,
                Text = $"{Properties.Strings.Direction}: ",
            };
            Dropdown settingFishPanelDirection_Dropdown = new Dropdown()
            {
                Location = new Point(settingFishPanelDirection_Label.Right + 8, settingFishPanelDirection_Label.Top - 4),
                Width = 100,
                Parent = parentPanel,
            };
            foreach (string s in FishingBuddyModule._fishPanelDirections)
            {
                settingFishPanelDirection_Dropdown.Items.Add(Properties.Strings.ResourceManager.GetString(s, Properties.Strings.Culture));
            }
            settingFishPanelDirection_Dropdown.SelectedItem = FishingBuddyModule._fishPanelDirection.Value;
            settingFishPanelDirection_Dropdown.ValueChanged += delegate
            {
                FishingBuddyModule._fishPanelDirection.Value = settingFishPanelDirection_Dropdown.SelectedItem;
            };

            IView settingFishSize_View = SettingView.FromType(FishingBuddyModule._fishImgSize, buildPanel.Width);
            ViewContainer settingFishSize_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingFishDrag_Container.Bottom + 8),
                Parent = parentPanel
            };
            settingFishSize_Container.Show(settingFishSize_View);

            IView settingFishTooltip_View = SettingView.FromType(FishingBuddyModule._fishPanelTooltipDisplay, buildPanel.Width);
            ViewContainer settingFishTooltip_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingFishSize_Container.Bottom + 5),
                Parent = parentPanel
            };
            settingFishTooltip_Container.Show(settingFishTooltip_View);

            IView settingClockDrag_View = SettingView.FromType(FishingBuddyModule._dragTimeOfDayClock, buildPanel.Width);
            ViewContainer settingClock_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingFishTooltip_Container.Bottom + 8),
                Parent = parentPanel
            };
            settingClock_Container.Show(settingClockDrag_View);

            IView settingClockShow_View = SettingView.FromType(FishingBuddyModule._hideTimeOfDay, buildPanel.Width);
            ViewContainer settingClockShow_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(160, settingClock_Container.Top),
                Parent = parentPanel
            };
            settingClockShow_Container.Show(settingClockShow_View);

            IView settingTimeLabel_View = SettingView.FromType(FishingBuddyModule._settingClockLabel, buildPanel.Width);
            ViewContainer settingTimeLabel_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(310, settingClock_Container.Top),
                Parent = parentPanel
            };
            settingTimeLabel_Container.Show(settingTimeLabel_View);

            Label settingTimeLabelAlign_Label = new Label()
            {
                Location = new Point(470, settingClock_Container.Top),
                Width = 75,
                AutoSizeHeight = false,
                WrapText = false,
                Parent = parentPanel,
                Text = $"{Properties.Strings.LabelAlign}: ",
            };
            Dropdown settingimeLabelAlign_Dropdown = new Dropdown()
            {
                Location = new Point(settingTimeLabelAlign_Label.Right + 8, settingTimeLabelAlign_Label.Top - 4),
                Width = 100,
                Parent = parentPanel,
            };
            foreach (string s in FishingBuddyModule._verticalAlignmentOptions)
            {
                settingimeLabelAlign_Dropdown.Items.Add(s);
            }
            settingimeLabelAlign_Dropdown.SelectedItem = FishingBuddyModule._settingClockAlign.Value;
            settingimeLabelAlign_Dropdown.ValueChanged += delegate
            {
                FishingBuddyModule._settingClockAlign.Value = settingimeLabelAlign_Dropdown.SelectedItem;
            };

            IView settingClockSize_View = SettingView.FromType(FishingBuddyModule._timeOfDayImgSize, buildPanel.Width);
            ViewContainer settingClockSize_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingClock_Container.Bottom + 8),
                Parent = parentPanel,
            };
            settingClockSize_Container.Show(settingClockSize_View);

            IView settingCombat_View = SettingView.FromType(FishingBuddyModule._hideInCombat, buildPanel.Width);
            ViewContainer settingCombat_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingClockSize_Container.Bottom + 5),
                Parent = parentPanel
            };
            settingCombat_Container.Show(settingCombat_View);
        }
    }
}
