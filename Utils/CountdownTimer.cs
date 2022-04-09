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

        public bool IsRunning => timer.Enabled;

        public int StepMs
        {
            get => timer.Interval;
            set => timer.Interval = value;
        }

        private Timer timer = new Timer();

        private TimeSpan _max = TimeSpan.FromMilliseconds(30000);

        public TimeSpan TimeLeft => (_max.TotalMilliseconds - _stopWatch.ElapsedMilliseconds) > 0 ? TimeSpan.FromMilliseconds(_max.TotalMilliseconds - _stopWatch.ElapsedMilliseconds) : TimeSpan.FromMilliseconds(0);

        private bool _mustStop => (_max.TotalMilliseconds - _stopWatch.ElapsedMilliseconds) < 0;

        public string TimeLeftStr => TimeLeft.ToString("mm':'ss");

        public string TimeLeftMsStr => TimeLeft.ToString("mm':'ss'.'fff");

        private void TimerTick(object sender, EventArgs e)
        {
            TimeChanged?.Invoke();

            if (_mustStop)
            {
                CountdownFinished?.Invoke();
                _stopWatch.Stop();
                timer.Enabled = false;
            }
        }

        public CountdownTimer(int min, int sec)
        {
            SetTime(min, sec);
            Init();
        }

        public CountdownTimer(TimeSpan ts)
        {
            if (ts == TimeSpan.Zero) return;
            SetTime(ts);
            Init();
        }

        public CountdownTimer()
        {
            Init();
        }

        private void Init()
        {
            StepMs = 1000;
            timer.Tick += new EventHandler(TimerTick);
        }

        public void SetTime(TimeSpan ts)
        {
            _max = ts;
            TimeChanged?.Invoke();
        }

        public void SetTime(int min, int sec = 0) => SetTime(TimeSpan.FromSeconds(min * 60 + sec));

        public void Start()
        {
            timer.Start();
            _stopWatch.Start();
        }

        public void Pause()
        {
            timer.Stop();
            _stopWatch.Stop();
        }

        public void Stop()
        {
            Reset();
            Pause();
        }

        public void Reset()
        {
            _stopWatch.Reset();
        }

        public void Restart()
        {
            _stopWatch.Reset();
            timer.Start();
        }

        public void Dispose() => timer.Dispose();
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