namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;

    // Based on https://github.com/manlaan/BlishHud-Clock/
    class Clock : Container
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(Clock));

        private string _timePhase = string.Empty;
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
        public VerticalAlignment LabelVerticalAlignment = VerticalAlignment.Bottom;

        private static BitmapFont _font;
        private Point _dragStart = Point.Zero;
        private bool _dragging;

        internal ClickThroughImage _dawn;
        internal ClickThroughImage _day;
        internal ClickThroughImage _dusk;
        internal ClickThroughImage _night;
        internal ClickThroughImage _currentTime;

        public event EventHandler<ValueChangedEventArgs<string>> TimeOfDayChanged;

        public Clock()
        {
            this.Location = new Point(50);
            this.Visible = true;
            this.Padding = Thickness.Zero;
            _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, this.Font_Size, ContentService.FontStyle.Regular);

            this._dawn = new ClickThroughImage
            {
                Parent = this,
                Texture = FishingBuddyModule._imgDawn,
                Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value),
                Location = new Point(0, _font.LineHeight),
                Opacity = 1.0f,
                BasicTooltipText = Properties.Strings.Dawn,
                Visible = this.TimePhase == Properties.Strings.Dawn,
                Capture = Drag,
            };
            Resized += delegate { this._dawn.Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value); };

            this._day = new ClickThroughImage
            {
                Parent = this,
                Texture = FishingBuddyModule._imgDay,
                Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value),
                Location = new Point(0, _font.LineHeight),
                Opacity = 1.0f,
                BasicTooltipText = Properties.Strings.Day,
                Visible = this.TimePhase == Properties.Strings.Day,
                Capture = Drag
            };
            Resized += delegate { this._day.Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value); };

            this._dusk = new ClickThroughImage
            {
                Parent = this,
                Texture = FishingBuddyModule._imgDusk,
                Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value),
                Location = new Point(0, _font.LineHeight),
                Opacity = 1.0f,
                BasicTooltipText = Properties.Strings.Dusk,
                Visible = this.TimePhase == Properties.Strings.Dusk,
                Capture = Drag
            };
            Resized += delegate { this._dusk.Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value); };

            this._night = new ClickThroughImage
            {
                Parent = this,
                Texture = FishingBuddyModule._imgNight,
                Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value),
                Location = new Point(0, _font.LineHeight),
                Opacity = 1.0f,
                BasicTooltipText = Properties.Strings.Night,
                Visible = this.TimePhase == Properties.Strings.Night,
                Capture = Drag
            };
            Resized += delegate { this._night.Size = new Point(FishingBuddyModule._timeOfDayImgSize.Value); };
            this._currentTime = this._day;
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
                TimeSpan timeTilNextPhase = TyriaTime.TimeTilNextPhase(FishingBuddyModule._currentMap);
                string timeStr = timeTilNextPhase.Hours > 0 ? timeTilNextPhase.ToString("h\\:mm\\:ss") : timeTilNextPhase.ToString("mm\\:ss");
                this.Size = new Point(
                    Math.Max((int)_font.MeasureString(timeStr).Width, FishingBuddyModule._timeOfDayImgSize.Value),
                    FishingBuddyModule._timeOfDayImgSize.Value + (_font.LineHeight * 2)
                    );

                if (timeTilNextPhase <= TimeSpan.Zero) return;
                spriteBatch.DrawStringOnCtrl(this,
                    timeStr,
                    _font,
                    new Rectangle(0, 0, this.Width, this.Height),
                    Color.White,
                    false,
                    true,
                    1,
                    HorizontalAlignment.Center,
                    LabelVerticalAlignment
                    );
            }
        }

        protected virtual void OnTimeOfDayChanged(ValueChangedEventArgs<string> e)
        {
            this._timePhase = e.NewValue;
            if (this.TimePhase == Properties.Strings.Dawn)
            {
                this._currentTime.Visible = false;
                this._currentTime = this._dawn;
                this._currentTime.Visible = true;
            }
            else if (this.TimePhase == Properties.Strings.Day)
            {
                this._currentTime.Visible = false;
                this._currentTime = this._day;
                this._currentTime.Visible = true;
            }
            else if (this.TimePhase == Properties.Strings.Dusk)
            {
                this._currentTime.Visible = false;
                this._currentTime = this._dusk;
                this._currentTime.Visible = true;
            }
            else if (this.TimePhase == Properties.Strings.Night)
            {
                this._currentTime.Visible = false;
                this._currentTime = this._night;
                this._currentTime.Visible = true;
            }
            TimeOfDayChanged?.Invoke(this, e);
        }
    }
}
