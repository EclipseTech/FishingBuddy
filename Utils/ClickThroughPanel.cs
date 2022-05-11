namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    using Blish_HUD.Controls;

    class ClickThroughPanel : Panel
    {
        public bool Capture { get; set; }
        public ClickThroughPanel(bool captureInput = false) => this.Capture = captureInput;

        // TODO change this to delegate or add delegate for this
        protected override CaptureType CapturesInput() => this.Capture ? CaptureType.Mouse : CaptureType.None;
    }
}
