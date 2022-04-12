using Blish_HUD.Controls;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class ClickThroughImage : Image
    {
        public bool Capture { get; set; }
        public ClickThroughImage(bool captureInput = false) : base()
        {
            this.Capture = captureInput;
        }

        // TODO change this to delegate or add delegate for this
        protected override CaptureType CapturesInput()
        {
            return this.Capture ? CaptureType.Mouse : CaptureType.DoNotBlock;
        }
    }
}
