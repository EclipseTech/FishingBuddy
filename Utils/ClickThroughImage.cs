using Blish_HUD.Controls;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class ClickThroughImage : Image
    {
        public bool capture { get; set; }
        public ClickThroughImage (bool captureInput = false) : base()
        {
            capture = captureInput;
        }

        protected override CaptureType CapturesInput()
        {
            return capture ? CaptureType.Mouse : CaptureType.DoNotBlock;
        }
    }
}
