using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Eclipse1807.BlishHUD.FishingBuddy.Utils
{
    // Modified From: https://stackoverflow.com/a/54119639
    public class CountdownTimer : IDisposable
    {
        public Stopwatch _stopWatch = new Stopwatch();

        public Action TimeChanged;
        public Action CountdownFinished;

        public bool IsRunning => this.timer.Enabled;

        public int StepMs
        {
            get => this.timer.Interval;
            set => this.timer.Interval = value;
        }

        private readonly Timer timer = new Timer();

        private TimeSpan _max = TimeSpan.FromMilliseconds(30000);

        public TimeSpan TimeLeft => (this._max.TotalMilliseconds - this._stopWatch.ElapsedMilliseconds) > 0 ? TimeSpan.FromMilliseconds(this._max.TotalMilliseconds - this._stopWatch.ElapsedMilliseconds) : TimeSpan.FromMilliseconds(0);

        private bool _mustStop => (this._max.TotalMilliseconds - this._stopWatch.ElapsedMilliseconds) < 0;

        public string TimeLeftStr => this.TimeLeft.ToString("mm':'ss");

        public string TimeLeftMsStr => this.TimeLeft.ToString("mm':'ss'.'fff");

        private void TimerTick(object sender, EventArgs e)
        {
            this.TimeChanged?.Invoke();

            if (this._mustStop)
            {
                this.CountdownFinished?.Invoke();
                this._stopWatch.Stop();
                this.timer.Enabled = false;
            }
        }

        public CountdownTimer(int min, int sec)
        {
            this.SetTime(min, sec);
            this.Init();
        }

        public CountdownTimer(TimeSpan ts)
        {
            if (ts == TimeSpan.Zero) return;
            this.SetTime(ts);
            this.Init();
        }

        public CountdownTimer() => this.Init();

        private void Init()
        {
            this.StepMs = 1000;
            this.timer.Tick += new EventHandler(this.TimerTick);
        }

        public void SetTime(TimeSpan ts)
        {
            this._max = ts;
            this.TimeChanged?.Invoke();
        }

        public void SetTime(int min, int sec = 0) => this.SetTime(TimeSpan.FromSeconds((min * 60) + sec));

        public void Start()
        {
            this.timer.Start();
            this._stopWatch.Start();
        }

        public void Pause()
        {
            this.timer.Stop();
            this._stopWatch.Stop();
        }

        public void Stop()
        {
            this.Reset();
            this.Pause();
        }

        public void Reset() => this._stopWatch.Reset();

        public void Restart()
        {
            this._stopWatch.Reset();
            this.timer.Start();
        }

        public void Dispose() => this.timer.Dispose();
    }
}
// Example usage:
//    CountdownTimer timer = new CountdownTimer();
//    //set to 30 mins
//    timer.SetTime(30, 0);
//    timer.Start();
//    //update label text
//    timer.TimeChanged += () => Label1.Text = timer.TimeLeftMsStr;
//    // show messageBox on timer = 00:00.000
//    timer.CountdownFinished += () => MessageBox.Show("Timer finished the work!");
//    //timer step. By default is 1 second
//    timer.StepMs = 77; // for nice milliseconds time switch
