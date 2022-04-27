using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    // Based on https://github.com/manlaan/BlishHud-Clock/
    class Clock : Container
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(Clock));

        private string _timePhase = "";
        public string TimePhase
        {
            get { return this._timePhase; }
            set
            {
                if (!Equals(this.TimePhase, value))
                {
                    Logger.Debug($"Time of day changed {this.TimePhase} -> {value}");
                    this.OnTimeOfDayChanged(new ValueChangedEventArgs<string>(this.TimePhase, value));
                }
            }
        }

        public bool HideLabel = false;
        public bool Drag = false;
        // TODO deal with resizing label/font on resize based on time panel size ~12-16 size font
        public ContentService.FontSize Font_Size = ContentService.FontSize.Size14;
        public int LabelVerticalAlignment = 0;

        private static BitmapFont _font;
        private Point _dragStart = Point.Zero;
        private bool _dragging;

        internal ClickThroughImage _dawn;
        internal ClickThroughImage _day;
        internal ClickThroughImage _dusk;
        internal ClickThroughImage _night;
        internal ClickThroughImage _currentTime;

        public event EventHandler<ValueChangedEventArgs<string>> TimeOfDayChanged;

        public DateTime NextPhaseTime = DateTime.Now;
        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(5);
        private double timeSinceUpdate;

        public Clock()
        {
            this.Location = new Point(50);
            this.Size = new Point(0);
            this.Visible = true;
            this.Padding = Thickness.Zero;

            this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;

            this._dawn = new ClickThroughImage
            {
                Parent = this,
                Texture = FishingBuddyModule._imgDawn,
                Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value),
                Location = new Point(0, 20),
                Opacity = 1.0f,
                BasicTooltipText = "Dawn",
                Visible = this.TimePhase == "Dawn",
                Capture = Drag,
            };
            Resized += delegate { this._dawn.Size = new Point(this.Size.X); };

            this._day = new ClickThroughImage
            {
                Parent = this,
                Texture = FishingBuddyModule._imgDay,
                Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value),
                Location = new Point(0, 20),
                Opacity = 1.0f,
                BasicTooltipText = "Day",
                Visible = this.TimePhase == "Day",
                Capture = Drag
            };
            Resized += delegate { this._day.Size = new Point(this.Size.X); };

            this._dusk = new ClickThroughImage
            {
                Parent = this,
                Texture = FishingBuddyModule._imgDusk,
                Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value),
                Location = new Point(0, 20),
                Opacity = 1.0f,
                BasicTooltipText = "Dusk",
                Visible = this.TimePhase == "Dusk",
                Capture = Drag
            };
            Resized += delegate { this._dusk.Size = new Point(this.Size.X); };

            this._night = new ClickThroughImage
            {
                Parent = this,
                Texture = FishingBuddyModule._imgNight,
                Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value),
                Location = new Point(0, 20),
                Opacity = 1.0f,
                BasicTooltipText = "Night",
                Visible = this.TimePhase == "Night",
                Capture = Drag
            };
            Resized += delegate { this._night.Size = new Point(this.Size.X); };
            this._currentTime = this._day;

            this.CalcTimeTilNextPhase();
        }

        protected override CaptureType CapturesInput() => this.Drag ? CaptureType.Mouse : CaptureType.Filter;

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            if (this.Drag)
            {
                this._dragging = true;
                this._dragStart = Input.Mouse.Position;
            }
            base.OnLeftMouseButtonPressed(e);
        }

        protected override void OnLeftMouseButtonReleased(MouseEventArgs e)
        {
            if (this.Drag)
            {
                this._dragging = false;
                FishingBuddyModule._timeOfDayPanelLoc.Value = this.Location;
            }
            base.OnLeftMouseButtonReleased(e);
        }

        private Boolean IsPointInBounds(Point point)
        {
            Point windowSize = GameService.Graphics.SpriteScreen.Size;

            return point.X > 0 &&
                    point.Y > 0 &&
                    point.X < windowSize.X &&
                    point.Y < windowSize.Y;
        }

        //TODO fix mouse, see: https://discord.com/channels/531175899588984842/534492173362528287/962805066673299457
        public override void UpdateContainer(GameTime gameTime)
        {
            UpdateCadenceUtil.UpdateWithCadence(this.CalcTimeTilNextPhase, gameTime, this.updateInterval.TotalMilliseconds, ref this.timeSinceUpdate);
            if (this._dragging)
            {
                this._dawn.Capture = this.Drag;
                this._day.Capture = this.Drag;
                this._dusk.Capture = this.Drag;
                this._night.Capture = this.Drag;
                if (this.IsPointInBounds(Input.Mouse.Position))
                {
                    Point nOffset = Input.Mouse.Position - this._dragStart;
                    this.Location += nOffset;
                }
                else
                {
                    this._dragging = false;
                    FishingBuddyModule._timeOfDayPanelLoc.Value = this.Location;
                }
                this._dragStart = Input.Mouse.Position;
            }
            else
            {
                this._dawn.Capture = this.Drag;
                this._day.Capture = this.Drag;
                this._dusk.Capture = this.Drag;
                this._night.Capture = this.Drag;
            }
        }

        public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (!this.HideLabel)
            {
                _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, this.Font_Size, ContentService.FontStyle.Regular);

                TimeSpan timeTilNextPhase = TyriaTime.TimeTilNextPhase(FishingBuddyModule._currentMap);
                if (timeTilNextPhase <= TimeSpan.Zero) return;
                string timeStr = $"{(int)timeTilNextPhase.TotalMinutes:D2}:{timeTilNextPhase:ss}";
                this.Size = new Point(
                    Math.Max((int)_font.MeasureString(timeStr).Width, this.Size.X),
                    (int)_font.MeasureString(timeStr).Height + FishingBuddyModule._timeOfDayImgSize.Value + 40
                    );

                spriteBatch.DrawStringOnCtrl(this,
                    timeStr,
                    _font,
                    new Rectangle(0, this.LabelVerticalAlignment, this.Width, this.Height),
                    Color.White,
                    false,
                    true,
                    1,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Top
                    );
            }
        }

        public void CalcTimeTilNextPhase(GameTime gameTime = default)
        {
            Logger.Debug($"Calculating time til next phase; currently: {this.TimePhase}");
            this.NextPhaseTime = TyriaTime.NextPhaseTime(FishingBuddyModule._currentMap);
            Logger.Debug($"Next phase time: {DateTime.Now}->{this.NextPhaseTime}");
        }

        protected virtual void OnTimeOfDayChanged(ValueChangedEventArgs<string> e)
        {
            this._timePhase = e.NewValue;
            switch (this.TimePhase)
            {
                case "Dawn":
                    this._currentTime.Visible = false;
                    this._currentTime = this._dawn;
                    this._currentTime.Visible = true;
                    break;
                case "Day":
                    this._currentTime.Visible = false;
                    this._currentTime = this._day;
                    this._currentTime.Visible = true;
                    break;
                case "Dusk":
                    this._currentTime.Visible = false;
                    this._currentTime = this._dusk;
                    this._currentTime.Visible = true;
                    break;
                case "Night":
                    this._currentTime.Visible = false;
                    this._currentTime = this._night;
                    this._currentTime.Visible = true;
                    break;
            }
            this.CalcTimeTilNextPhase();
            TimeOfDayChanged?.Invoke(this, e);
        }
    }
}
