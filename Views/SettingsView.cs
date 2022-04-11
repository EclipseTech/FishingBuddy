using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Blish_HUD.Settings.UI.Views;
using Blish_HUD.Graphics.UI;


namespace Eclipse1807.BlishHUD.FishingBuddy.Views
{
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

            IView settingFishSize_View = SettingView.FromType(FishingBuddyModule._fishImgSize, buildPanel.Width);
            ViewContainer settingFishSize_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingFishDrag_Container.Bottom + 8),
                Parent = parentPanel
            };
            settingFishSize_Container.Show(settingFishSize_View);

            IView settingClockDrag_View = SettingView.FromType(FishingBuddyModule._dragTimeOfDayClock, buildPanel.Width);
            ViewContainer settingClock_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingFishSize_Container.Bottom + 5),
                Parent = parentPanel
            };
            settingClock_Container.Show(settingClockDrag_View);

            IView settingClockShow_View = SettingView.FromType(FishingBuddyModule._hideTimeOfDay, buildPanel.Width);
            ViewContainer settingClockShow_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(160, settingFishSize_Container.Bottom + 5),
                Parent = parentPanel
            };
            settingClockShow_Container.Show(settingClockShow_View);

            IView settingClockSize_View = SettingView.FromType(FishingBuddyModule._timeOfDayImgSize, buildPanel.Width);
            ViewContainer settingClockSize_Container = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                Location = new Point(10, settingClock_Container.Bottom + 5),
                Parent = parentPanel
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

            //IView settingClockServer_View = SettingView.FromType(FishingBuddyModule._settingClockServer, buildPanel.Width);
            //ViewContainer settingClockServer_Container = new ViewContainer()
            //{
            //    WidthSizingMode = SizingMode.Fill,
            //    Location = new Point(310, settingClockLocal_Container.Location.Y),
            //    Parent = parentPanel
            //};
            //settingClockServer_Container.Show(settingClockServer_View);
            //
            //IView settingClockDayNight_View = SettingView.FromType(FishingBuddyModule._settingClockDayNight, buildPanel.Width);
            //ViewContainer settingClockDayNight_Container = new ViewContainer()
            //{
            //    WidthSizingMode = SizingMode.Fill,
            //    Location = new Point(460, settingClockLocal_Container.Location.Y),
            //    Parent = parentPanel
            //};
            //settingClockDayNight_Container.Show(settingClockDayNight_View);
            //
            //IView settingClock24H_View = SettingView.FromType(FishingBuddyModule._settingClock24H, buildPanel.Width);
            //ViewContainer settingClock24H_Container = new ViewContainer()
            //{
            //    WidthSizingMode = SizingMode.Fill,
            //    Location = new Point(10, settingClockLocal_Container.Bottom + 5),
            //    Parent = parentPanel
            //};
            //settingClock24H_Container.Show(settingClock24H_View);
            //
            //IView settingClockHideLabel_View = SettingView.FromType(FishingBuddyModule._settingClockHideLabel, buildPanel.Width);
            //ViewContainer settingClockHideLabel_Container = new ViewContainer()
            //{
            //    WidthSizingMode = SizingMode.Fill,
            //    Location = new Point(160, settingClock24H_Container.Location.Y),
            //    Parent = parentPanel
            //};
            //settingClockHideLabel_Container.Show(settingClockHideLabel_View);
            //
            //
            //Label settingClockFontSize_Label = new Label()
            //{
            //    Location = new Point(10, settingClock24H_Container.Bottom + 10),
            //    Width = 75,
            //    AutoSizeHeight = false,
            //    WrapText = false,
            //    Parent = parentPanel,
            //    Text = "Font Size: ",
            //};
            //Dropdown settingClockFontSize_Select = new Dropdown()
            //{
            //    Location = new Point(settingClockFontSize_Label.Right + 8, settingClockFontSize_Label.Top - 4),
            //    Width = 50,
            //    Parent = parentPanel,
            //};
            //foreach (var s in FishingBuddyModule._fontSizes)
            //{
            //    settingClockFontSize_Select.Items.Add(s);
            //}
            //settingClockFontSize_Select.SelectedItem = FishingBuddyModule._settingClockFontSize.Value;
            //settingClockFontSize_Select.ValueChanged += delegate
            //{
            //    FishingBuddyModule._settingClockFontSize.Value = settingClockFontSize_Select.SelectedItem;
            //};
            //
            //Label settingClockLabelAlign_Label = new Label()
            //{
            //    Location = new Point(10, settingClockFontSize_Label.Bottom + 10),
            //    Width = 75,
            //    AutoSizeHeight = false,
            //    WrapText = false,
            //    Parent = parentPanel,
            //    Text = "Label Align: ",
            //};
            //Dropdown settingClockLabelAlign_Select = new Dropdown()
            //{
            //    Location = new Point(settingClockLabelAlign_Label.Right + 8, settingClockLabelAlign_Label.Top - 4),
            //    Width = 75,
            //    Parent = parentPanel,
            //};
            //foreach (var s in FishingBuddyModule._fontAlign)
            //{
            //    settingClockLabelAlign_Select.Items.Add(s);
            //}
            //settingClockLabelAlign_Select.SelectedItem = FishingBuddyModule._settingClockLabelAlign.Value;
            //settingClockLabelAlign_Select.ValueChanged += delegate
            //{
            //    FishingBuddyModule._settingClockLabelAlign.Value = settingClockLabelAlign_Select.SelectedItem;
            //};
            //
            //
            //Label settingClockTimeAlign_Label = new Label()
            //{
            //    Location = new Point(settingClockLabelAlign_Select.Right + 20, settingClockLabelAlign_Label.Top),
            //    Width = 75,
            //    AutoSizeHeight = false,
            //    WrapText = false,
            //    Parent = parentPanel,
            //    Text = "Time Align: ",
            //};
            //Dropdown settingClockTimeAlign_Select = new Dropdown()
            //{
            //    Location = new Point(settingClockTimeAlign_Label.Right + 8, settingClockTimeAlign_Label.Top - 4),
            //    Width = 75,
            //    Parent = parentPanel,
            //};
            //foreach (var s in FishingBuddyModule._fontAlign)
            //{
            //    settingClockTimeAlign_Select.Items.Add(s);
            //}
            //settingClockTimeAlign_Select.SelectedItem = FishingBuddyModule._settingClockTimeAlign.Value;
            //settingClockTimeAlign_Select.ValueChanged += delegate
            //{
            //    FishingBuddyModule._settingClockTimeAlign.Value = settingClockTimeAlign_Select.SelectedItem;
            //};
            //
            //IView settingClockDrag_View = SettingView.FromType(FishingBuddyModule._settingClockDrag, buildPanel.Width);
            //ViewContainer settingClockDrag_Container = new ViewContainer()
            //{
            //    WidthSizingMode = SizingMode.Fill,
            //    Location = new Point(10, settingClockTimeAlign_Label.Bottom + 6),
            //    Parent = parentPanel
            //};
            //settingClockDrag_Container.Show(settingClockDrag_View);
        }
    }
}