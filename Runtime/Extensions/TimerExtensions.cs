using UnityEngine;

namespace TimerLib
{
    public static class TimerExtensions
    {
        public static Timer StartTimer(this MonoBehaviour source, float duration) => Timer.StartTimer(duration, source);

        public static void KillTimers(this MonoBehaviour source) => TimerManager.KillBySource(source);

        public static void CompleteTimers(this MonoBehaviour source) => TimerManager.CompleteBySource(source);
    }
}
