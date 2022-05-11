namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    using Blish_HUD.Controls;

    class ClickThroughImage : Image
    {
        public bool Capture { get; set; }
        public ClickThroughImage(bool captureInput = false) : base() => this.Capture = captureInput;

        // TODO change this to delegate or add delegate for this
        protected override CaptureType CapturesInput() => this.Capture ? CaptureType.Mouse : CaptureType.DoNotBlock;
    }
}
