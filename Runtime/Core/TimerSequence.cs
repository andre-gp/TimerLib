using System;
using System.Collections.Generic;

namespace TimerLib
{
    public class TimerSequence
    {
        private interface IStep
        {
            Timer Run(Action onDone);
        }

        private class WaitStep : IStep
        {
            private readonly float duration;
            public WaitStep(float duration) => this.duration = duration;

            public Timer Run(Action onDone)
            {
                var t = Timer.Create(duration).OnComplete(onDone);
                t.Activate();
                return t;
            }
        }

        private class CallStep : IStep
        {
            private readonly Action callback;
            public CallStep(Action callback) => this.callback = callback;

            public Timer Run(Action onDone)
            {
                callback?.Invoke();
                onDone?.Invoke();
                return null;
            }
        }

        private class TimerStep : IStep
        {
            private readonly Timer timer;
            public TimerStep(Timer timer) => this.timer = timer;

            public Timer Run(Action onDone)
            {
                timer.OnComplete(onDone);
                timer.Activate();
                return timer;
            }
        }

        private readonly List<IStep> steps = new();
        private Action onCompleteCallback;
        private Timer activeTimer;
        private int currentIndex = -1;
        private bool isKilled;

        public static TimerSequence Create() => new TimerSequence();

        public TimerSequence Wait(float duration)
        {
            steps.Add(new WaitStep(duration));
            return this;
        }

        public TimerSequence Call(Action callback)
        {
            steps.Add(new CallStep(callback));
            return this;
        }

        public TimerSequence Append(Timer timer)
        {
            steps.Add(new TimerStep(timer));
            return this;
        }

        public TimerSequence OnComplete(Action callback)
        {
            onCompleteCallback = callback;
            return this;
        }

        public TimerSequence Start()
        {
            Advance();
            return this;
        }

        public TimerSequence Pause()
        {
            activeTimer?.Pause();
            return this;
        }

        public TimerSequence Resume()
        {
            activeTimer?.Resume();
            return this;
        }

        public void Kill()
        {
            isKilled = true;
            activeTimer?.Kill();
            activeTimer = null;
        }

        private void Advance()
        {
            if (isKilled) return;

            currentIndex++;

            if (currentIndex >= steps.Count)
            {
                onCompleteCallback?.Invoke();
                return;
            }

            activeTimer = steps[currentIndex].Run(Advance);
        }
    }
}
