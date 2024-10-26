using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Timers
{
    // The TimerManager manages all scheduled timers. This includes both the regular execution of timers, as well as the cleanup of timers after garbage collection.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9999999)]
    public class TimersManager : MonoBehaviour
    {
        // Ensure we only have a single instance of the TimersManager loaded (singleton pattern).
        private static TimersManager m_instance = null;

        // A map of weak references. When an object is garbage collected, all its timers are automatically removed.
        private static IDictionary<int, Timer> m_Timers = new Dictionary<int, Timer>();

        // Whether the game is paused
        private static bool m_bPaused = false;

        private List<int> cachedKeys = new List<int>(500);

        private static void Init()
        {
            if (!m_instance)
            {
                m_instance = new GameObject("TimersManager").AddComponent<TimersManager>();
                DontDestroyOnLoad(m_instance.gameObject);
            }
        }

        private void Start()
        {
            if (m_instance != this)
                DestroyImmediate(gameObject);
        }

        private void Update()
        {
            if (m_bPaused)
                return;

            int count = -1;
            foreach (int id in m_Timers.Keys)
            {
                if (++count < cachedKeys.Count)
                    cachedKeys[count] = id;
                else
                    cachedKeys.Add(id);
            }

            for (int i = 0; i <= count; ++i)
            {
                var id = cachedKeys[i];
                if (m_Timers.TryGetValue(id, out var timer))
                {
                    timer.Update();
                    if (timer.ShouldClear)
                        m_Timers.Remove(id);
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            m_bPaused = pauseStatus;
        }

        /// <summary>
        /// Set timer
        /// </summary>owner
        /// <param name="timer">Timer to add</param>
        /// <param name="overrideOld">If true(default), then it overrides the previously set timer that points to the same action. Timers with the same ID (HashCode) will be overriden regardless.</param>
        /// <return>The id of the timer</return>
        public static int SetTimer(Timer timer, bool overrideOld = true)
        {
            Init();
            if (timer == null)
            {
                Debug.LogException(new Exception("Trying to set an invalid null target"));
                return 0;
            }

            if (timer.Action != null && timer.Interval > 0f && timer.LoopsCount > 0)
            {
                ClearTimer(timer.Id);
                if (overrideOld)
                    ClearTimer(timer.Action);
                m_Timers.Add(timer.Id, timer);
                return timer.Id;
            }
            return 0;
        }

        /// <summary>
        /// Set a timer that loops LoopCount times
        /// </summary>
        /// <param name="owner">The object that contains the timer. Required in order to remove the timer if the object is destroyed.</param>
        /// <param name="interval">Interval(in seconds) between loops</param>
        /// <param name="loopsCount">How many times to loop</param>
        /// <param name="action">Delegate</param>
        /// <param name="overrideOld">If true(default), then it overrides the previously set timer that points to the same action.</param>
        /// <return>The id of the timer</return>
        public static int SetTimer(object owner, float interval, uint loopsCount, Action action, bool unscaledTime = false, bool overrideOld = true)
        {
            loopsCount = System.Math.Max(loopsCount, 1);
            var timer = new Timer(owner, interval, loopsCount, unscaledTime, action);
            return SetTimer(timer, overrideOld);
        }

        /// <summary>
        /// Set a timer that activates only once.
        /// </summary>
        /// <param name="owner">The object that contains the timer. Required in order to remove the timer if the object is destroyed.</param>
        /// <param name="interval">Interval(in seconds) between loops</param>
        /// <param name="action">Delegate</param>
        /// <param name="overrideOld">If true(default), then it overrides the previously set timer that points to the same action.</param>
        /// <return>The id of the timer</return>
        public static int SetTimer(object owner, float interval, Action action, bool unscaledTime = false, bool overrideOld = true)
        {
            var timer = new Timer(owner, interval, 1, unscaledTime, action);
            return SetTimer(timer, overrideOld);
        }

        /// <summary>
        /// Set an infinitely loopable timer
        /// </summary>
        /// <param name="owner">The object that contains the timer. Required in order to remove the timer if the object is destroyed.</param>
        /// <param name="interval">Interval(in seconds)</param>
        /// <param name="unityAction">Delegate</param>
        /// <param name="overrideOld">If true(default), then it overrides the previously set timer that points to the same action.</param>
        /// <return>The id of the timer</return>
        public static int SetLoopableTimer(object owner, float interval, Action action, bool unscaledTime = false, bool overrideOld = true)
        {
            var timer = new Timer(owner, interval, Timer.INFINITE_LOOPS, unscaledTime, action);
            return SetTimer(timer, overrideOld: overrideOld);
        }

        /// <summary>
        /// Add a list of timers. Works great with List<Timer> in inspector. See 'TimersList.cs' for an example.
        /// </summary>
        /// <param name="owner">Owner of timers. This should be the object that have these timers. Required in order to remove the timers if the object is destroyed.</param>
        /// <param name="timers">Timers list</param>
        /// <param name="overrideOld">If true(default), then it overrides the previously set timers that point to the same action. Timers with the same ID will be overriden regardless.</param>
        /// <return>The ids of the timers</return>
        public static IEnumerable<int> AddTimers(object owner, IEnumerable<Timer.Descriptor> timers, bool overrideOld = true)
        {
            foreach (var timerDescriptor in timers)
            {
                var timer = Timer.FromDescriptor(owner, timerDescriptor);
                yield return SetTimer(timer, overrideOld);
            }
        }

        /// <summary>
        /// Add a list of timers. Works great with List<Timer> in inspector. See 'TimersList.cs' for an example.
        /// </summary>
        /// <param name="owner">Owner of timers. This should be the object that have these timers. Required in order to remove the timers if the object is destroyed.</param>
        /// <param name="timers">Timers list</param>
        /// <param name="overrideOld">If true(default), then it overrides the previously set timers that point to the same action. Timers with the same ID (HashCode) will be overriden regardless.</param>
        public static void AddTimers(IEnumerable<Timer> timers, bool overrideOld = true)
        {
            foreach (Timer timer in timers)
                SetTimer(timer, overrideOld);
        }

        /// <summary>
        /// Remove a certain timer
        /// </summary>
        /// <param name="action">Action</param>
        public static void ClearTimer(Action action)
        {
            Init();
            KeyValuePair<int, Timer> item;
            while ((item = m_Timers.FirstOrDefault(x => x.Value.Action == action)).Value != null)
                m_Timers.Remove(item.Key);
        }

        /// <summary>
        /// Remove a certain timer
        /// </summary>
        /// <param name="action">Action</param>
        public static void ClearTimer(int id)
        {
            Init();
            m_Timers.Remove(id);
        }

        /// <summary>
        /// Get all timers that have the referenced owner
        /// </summary>owner
        /// <param name="owner">Owner reference</param>
        public static IEnumerable<Timer> GetTimersByOwner(object owner)
        {
            Init();
            foreach (var item in m_Timers.Where(x => x.Value.Owner == owner))
                yield return item.Value;
        }

        /// <summary>
        /// Get timer by name (which is the delegate's name)
        /// </summary>
        /// <param name="action">Action</param>
        public static IEnumerable<Timer> GetTimersByAction(Action action)
        {
            Init();
            foreach (var item in m_Timers.Where(x => x.Value.Action == action))
                yield return item.Value;
        }

        /// <summary>
        /// Get timer by name (which is the delegate's name)
        /// </summary>
        /// <param name="id">Timer id</param>
        public static Timer GetTimer(int id)
        {
            Init();
            if (m_Timers.ContainsKey(id))
                return m_Timers[id];

            return null;
        }

        /// <summary>
        /// Get timer interval. Returns 0 if not found.
        /// </summary>
        /// <param name="id">Timer id</param>
        public static float Interval(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? 0f : timer.Interval; }

        /// <summary>
        /// Get total loops count (INFINITE (which is uint.MaxValue) if is constantly looping) 
        /// </summary>
        /// <param name="id">Timer id</param>
        public static uint LoopsCount(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? 0 : timer.LoopsCount; }

        /// <summary>
        /// Get how many loops were completed
        /// </summary>
        /// <param name="id">Timer id</param>
        public static uint CurrentLoopsCount(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? 0 : timer.CurrentLoopsCount; }

        /// <summary>
        /// Get how many loops remained to completion
        /// </summary>
        /// <param name="id">Timer id</param>
        public static uint RemainingLoopsCount(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? 0 : timer.RemainingLoopsCount; }

        /// <summary>
        /// Get total remaining time
        /// </summary>
        /// <param name="id">Timer id</param>
        public static float RemainingTime(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? -1f : timer.RemainingTime; }

        /// <summary>
        /// Get total elapsed time
        /// </summary>
        /// <param name="id">Timer id</param>
        public static float ElapsedTime(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? -1f : timer.ElapsedTime; }

        /// <summary>
        /// Get elapsed time in current loop
        /// </summary>
        /// <param name="id">Timer id</param>
        public static float CurrentCycleElapsedTime(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? -1f : timer.CurrentCycleElapsedTime; }

        /// <summary>
        /// Get remaining time in current loop
        /// </summary>
        /// <param name="id">Timer id</param>
        public static float CurrentCycleRemainingTime(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? -1f : timer.CurrentCycleRemainingTime; }

        /// <summary>
        /// Verifies whether the timer exits
        /// </summary>
        /// <param name="id">Timer id</param>
        public static bool IsTimerActive(int id) { Init(); Timer timer = GetTimer(id); return timer != null; }

        /// <summary>
        /// Checks if the timer is paused
        /// </summary>
        /// <param name="id">Timer id</param>
        public static bool IsTimerPaused(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? false : timer.IsPaused; }

        /// <summary>
        /// Pause / Unpause timer
        /// </summary>
        /// <param name="id">Timer id</param>
        /// <param name="bPause">true - pause, false - unpause</param>
        public static void SetPaused(int id, bool bPause) { Init(); Timer timer = GetTimer(id); if (timer != null) timer.SetPaused(bPause); }

        /// <summary>
        /// Get total duration, (INFINITE if it's constantly looping)
        /// </summary>
        /// <param name="id">Timer id</param>
        public static float Duration(int id) { Init(); Timer timer = GetTimer(id); return timer == null ? 0f : timer.Duration; }
    }
}