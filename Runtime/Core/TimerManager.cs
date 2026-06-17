using System.Collections.Generic;
using UnityEngine;

namespace TimerLib
{
    public class TimerManager : MonoBehaviour
    {
        private static TimerManager instance;

        private readonly List<Timer> pending = new();
        private readonly List<Timer> normal = new();
        private readonly List<Timer> late = new();
        private readonly List<Timer> fixedTimers = new();
        private readonly List<Timer> manual = new();
        private readonly List<Timer> toRemove = new();

        public static TimerManager Instance
        {
            get
            {
                if (instance == null) 
                    CreateInstance();

                return instance;
            }
        }

        private static void CreateInstance()
        {
            var go = new GameObject("[TimerManager]");
            go.AddComponent<TimerManager>(); // Awake sets instance and DontDestroyOnLoad
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        internal static void Register(Timer timer)
        {
            Instance.pending.Add(timer);
        }

        internal static void Unregister(Timer timer)
        {
            instance?.toRemove.Add(timer);
        }

        internal static void KillBySource(Object source)
        {
            if (instance == null) return;
            instance.ApplyToSource(source, t => t.Kill());
        }

        internal static void CompleteBySource(Object source)
        {
            if (instance == null) return;
            instance.ApplyToSource(source, t => t.Complete());
        }

        private void ApplyToSource(Object source, System.Action<Timer> action)
        {
            ApplyInList(pending, source, action);
            ApplyInList(normal, source, action);
            ApplyInList(late, source, action);
            ApplyInList(fixedTimers, source, action);
            ApplyInList(manual, source, action);
        }

        private static void ApplyInList(List<Timer> list, Object source, System.Action<Timer> action)
        {
            foreach (var timer in list)
            {
                if (timer.Source == source)
                    action(timer);
            }
        }

        private void Update()
        {
            FlushRemovals();
            FlushPending();
            TickList(normal, Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            FlushRemovals();
            TickList(late, Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            FlushRemovals();
            FlushPending();
            TickList(fixedTimers, Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
        }

        private void FlushPending()
        {
            foreach (var timer in pending)
            {
                switch (timer.GetUpdateType())
                {
                    case UpdateType.Normal:
                        normal.Add(timer); 
                        break;
                    case UpdateType.Late:
                        late.Add(timer); 
                        break;
                    case UpdateType.Fixed:
                        fixedTimers.Add(timer); 
                        break;
                    case UpdateType.Manual:
                        manual.Add(timer); 
                        break;
                }
            }

            pending.Clear();
        }

        private void FlushRemovals()
        {
            if (toRemove.Count == 0)
                return;

            foreach (var timer in toRemove)
            {
                if (pending.Remove(timer))
                    continue;

                switch (timer.GetUpdateType())
                {
                    case UpdateType.Normal:
                        normal.Remove(timer); 
                        break;
                    case UpdateType.Late:
                        late.Remove(timer); 
                        break;
                    case UpdateType.Fixed:
                        fixedTimers.Remove(timer); 
                        break;
                    case UpdateType.Manual: 
                        manual.Remove(timer);
                        break;
                }
            }

            toRemove.Clear();
        }

        private static void TickList(List<Timer> list, float dt, float unscaledDt)
        {
            foreach (var timer in list)
                timer.Tick(timer.UsesRealTime() ? unscaledDt : dt);
        }
    }
}
