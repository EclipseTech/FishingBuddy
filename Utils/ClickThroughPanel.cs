using Blish_HUD.Controls;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    class ClickThroughPanel : Panel
    {
        public bool capture { get; set; }
        public ClickThroughPanel (bool captureInput = false)
        {
            capture = captureInput;
        }

        protected override CaptureType CapturesInput()
        {
            return capture ? CaptureType.Mouse : CaptureType.None;
        }
    }
}
