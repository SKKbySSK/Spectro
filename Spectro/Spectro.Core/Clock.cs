using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Spectro.Core
{
    public abstract class Clock
    {
        public event EventHandler Tick;

        public bool IsStarted { get; protected set; }

        public abstract void Start();

        public abstract void Stop();

        protected Task OnTick()
        {
            return Task.Run(() => { Tick?.Invoke(this, EventArgs.Empty); });
        }
    }
    
    public class IntervalClock : Clock 
    {
        public IntervalClock(TimeSpan interval)
        {
            Interval = interval;
        }
        
        public TimeSpan Interval { get; set; }
        
        public override void Start()
        {
            IsStarted = true;
            Task.Run(() =>
            {
                var sw = new Stopwatch();
                while (IsStarted)
                {
                    OnTick();
                    Thread.Sleep(Interval);
                }
            });
        }

        public override void Stop()
        {
            IsStarted = false;
        }
    }
}