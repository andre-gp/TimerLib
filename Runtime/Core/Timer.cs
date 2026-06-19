using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimerLib
{
    public class Timer
    {
        private enum TimerState { Running, Paused, Complete, Killed }

        #region Public API
        public float Progress => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
        public float Elapsed => elapsed;
        public float Remaining => Mathf.Max(0f, duration - elapsed);
        public int LoopIndex => loopIndex;
        public bool IsPlaying => state == TimerState.Running;
        public bool IsComplete => state == TimerState.Complete;
        public bool IsPaused => state == TimerState.Paused;

        #region Control
        public Timer Pause()
        {
            if (state == TimerState.Running) 
                state = TimerState.Paused;

            return this;
        }

        public Timer Resume()
        {
            if (state == TimerState.Paused) 
                state = TimerState.Running;

            return this;
        }

        public void Kill()
        {
            if (state is TimerState.Complete or TimerState.Killed) 
                return;

            state = TimerState.Killed;
            onKill?.Invoke();
            TimerManager.Unregister(this);
        }

        public void Complete()
        {
            if (state is TimerState.Complete or TimerState.Killed) 
                return;

            elapsed = duration;
            state = TimerState.Complete;
            onComplete?.Invoke();
            TimerManager.Unregister(this);
        }

        /// <summary>
        /// Ticks the current timer. You should only call this for manually updated timers.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (state != TimerState.Running) 
                return;

            if (hasSource && source == null)
            {
                state = TimerState.Killed;
                onKill?.Invoke();
                TimerManager.Unregister(this);
                return;
            }

            if (!hasStarted)
            {
                hasStarted = true;
                onStart?.Invoke();
                if (state != TimerState.Running) 
                    return;
            }

            elapsed += deltaTime;

            bool finished = elapsed >= duration;
            if (finished) elapsed = duration;

            onUpdate?.Invoke(this);

            if (state != TimerState.Running) 
                return;

            if (!finished) 
                return;

            bool hasMoreLoops = loops < 0 || loopIndex < loops;

            if (hasMoreLoops)
            {
                loopIndex++;
                onLoop?.Invoke(loopIndex);
                if (state != TimerState.Running) return;
                elapsed = 0f;
            }
            else
            {
                state = TimerState.Complete;
                onComplete?.Invoke();
                TimerManager.Unregister(this);
            }
        }
        #endregion

        #region Callbacks
        public Timer OnStart(Action callback) { onStart += callback; return this; }
        public Timer OnUpdate(Action<Timer> callback) { onUpdate += callback; return this; }
        public Timer OnLoop(Action<int> callback) { onLoop += callback; return this; }
        public Timer OnComplete(Action callback) { onComplete += callback; return this; }
        public Timer OnKill(Action callback) { onKill += callback; return this; }
        #endregion

        #region Configuration
        public Timer SetUpdateType(UpdateType type)
        {
            if (updateType == type || state is TimerState.Complete or TimerState.Killed) return this;
            var old = updateType;
            updateType = type;
            TimerManager.RequestTypeChange(this, old);
            return this;
        }

        public Timer SetRealTime(bool realTime = true) { useRealTime = realTime; return this; }
        /// <summary>Sets the number of extra loops after the first run. Use -1 for infinite loops.</summary>
        public Timer SetLoops(int loops) { this.loops = loops; return this; }
        #endregion

        #region Static Entry Points
        public static Timer StartTimer(float duration, UpdateType updateType = UpdateType.Normal)
        {
            var timer = new Timer(duration, null, updateType);
            TimerManager.Register(timer);
            return timer;
        }

        public static Timer Create(float duration, UpdateType updateType = UpdateType.Normal)
            => new Timer(duration, null, updateType);

        internal static Timer StartTimer(float duration, Object source, UpdateType updateType = UpdateType.Normal)
        {
            var timer = new Timer(duration, source, updateType);
            TimerManager.Register(timer);
            return timer;
        }
        #endregion

        #endregion

        #region Private API

        private readonly float duration;
        private readonly Object source;
        private readonly bool hasSource;

        private TimerState state = TimerState.Running;
        private float elapsed;
        private int loopIndex;
        private bool hasStarted;

        private int loops = 0;
        private bool useRealTime;
        private UpdateType updateType;

        private Action onStart;
        private Action<Timer> onUpdate;
        private Action<int> onLoop;
        private Action onComplete;
        private Action onKill;


        internal Object Source => source;
        internal bool UsesRealTime() => useRealTime;
        internal UpdateType GetUpdateType() => updateType;
        internal void Activate() => TimerManager.Register(this);

        private Timer(float duration, Object source, UpdateType updateType)
        {
            this.duration = Mathf.Max(0f, duration);
            this.source = source;
            hasSource = !ReferenceEquals(source, null);
            this.updateType = updateType;
        }

        #endregion
    }
}
