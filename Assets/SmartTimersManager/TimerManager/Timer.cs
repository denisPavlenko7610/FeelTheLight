using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

[assembly: InternalsVisibleToAttribute("TimersManager")]
namespace Timers
{
    public class Timer
    {
        [Serializable]
        public struct Descriptor
        {
            [SerializeField]
            private bool m_InfiniteLoops;

            [SerializeField]
            private uint m_LoopsCount;

            [SerializeField]
            private float m_Interval;

            [SerializeField]
            private bool m_UnscaledTime;

            [SerializeField]
            private UnityEvent m_Event;

            public float Interval => m_Interval;
            public bool UnscaledTime => m_UnscaledTime;
            public uint LoopsCount => m_InfiniteLoops ? Timer.INFINITE_LOOPS : m_LoopsCount;
            public UnityEvent Event => m_Event;
        }

        public const uint INFINITE_LOOPS = uint.MaxValue;
        private float m_Interval = 0;
        private uint m_LoopsCount = 1;

        private event Action m_Action = null;
        private WeakReference m_Owner = null;
        private bool m_bIsPaused = false;
        private bool m_UnscaledTime = false;
        private uint m_CurrentLoopsCount = 0;
        private float m_ElapsedTime = 0;
        private float m_CurrentCycleElapsedTime = 0;

        internal void Update()
        {
            if (m_bIsPaused || !IsValid(Owner))
                return;

            if (m_Action == null || m_Interval < 0)
            {
                m_Interval = 0;
                return;
            }

            if (m_CurrentLoopsCount >= m_LoopsCount && m_LoopsCount != INFINITE_LOOPS)
            {
                m_ElapsedTime = m_Interval * m_LoopsCount;
                m_CurrentCycleElapsedTime = m_Interval;
            }
            else
            {
                m_ElapsedTime += m_UnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (m_LoopsCount != INFINITE_LOOPS)
                    m_ElapsedTime = Mathf.Min(m_ElapsedTime, m_Interval * m_LoopsCount);

                m_CurrentCycleElapsedTime = Mathf.Min(m_Interval, m_ElapsedTime - m_CurrentLoopsCount * m_Interval);
                if (m_CurrentCycleElapsedTime == m_Interval)
                {
                    m_CurrentLoopsCount++;
                    m_CurrentCycleElapsedTime = 0f;
                    m_Action?.Invoke();
                }
            }
        }

        public static Timer FromDescriptor(object owner, Timer.Descriptor descriptor)
        {
            return new Timer(
                owner: owner,
                interval: descriptor.Interval,
                loopsCount: Math.Max(1, descriptor.LoopsCount),
                unscaledTime: descriptor.UnscaledTime,
                action: () => descriptor.Event?.Invoke()
            );
        }

        public Timer(object owner, float interval, uint loopsCount, bool unscaledTime, Action action)
        {
            if (owner == null)
            {
                Debug.LogException(new Exception("Timer requre a valid owner, got null"));
                return;
            }

            if (interval < 0)
                interval = 0;

            m_UnscaledTime = unscaledTime;
            m_Owner = new WeakReference(owner);
            m_Interval = interval;
            m_LoopsCount = System.Math.Max(loopsCount, 1);
            m_Action = action;
        }

        ~Timer()
        {
            m_Action = null;
        }

        /// <summary>
        /// Timer ID
        /// </summary>
        public int Id => this.GetHashCode();

        /// <summary>
        /// Get interval
        /// </summary>
        public object Owner => m_Owner.Target;

        /// <summary>
        /// Get interval
        /// </summary>
        public float Interval => m_Interval;

        /// <summary>
        /// Get total loops count (INFINITE (which is uint.MaxValue) if is constantly looping) 
        /// </summary>
        public uint LoopsCount => m_LoopsCount;

        /// <summary>
        /// Get how many loops were completed
        /// </summary>
        public uint CurrentLoopsCount => m_CurrentLoopsCount;

        /// <summary>
        /// Get how many loops remained to completion
        /// </summary>
        public uint RemainingLoopsCount => LoopsCount - CurrentLoopsCount;

        /// <summary>
        /// Get total duration, (INFINITE if it's constantly looping)
        /// </summary>
        public float Duration => (LoopsCount == INFINITE_LOOPS) ? Mathf.Infinity : (LoopsCount * Interval);

        /// <summary>
        /// Get the delegate to execute
        /// </summary>
        public Action Action => m_Action;

        /// <summary>
        /// Get total remaining time
        /// </summary>
        public float RemainingTime => (LoopsCount == INFINITE_LOOPS && Interval > 0f) ? Mathf.Infinity : Mathf.Max(LoopsCount * Interval - ElapsedTime, 0f);

        /// <summary>
        /// Get total elapsed time
        /// </summary>
        public float ElapsedTime => m_ElapsedTime;

        /// <summary>
        /// Get elapsed time in current loop
        /// </summary>
        public float CurrentCycleElapsedTime => m_CurrentCycleElapsedTime;

        /// <summary>
        /// Get remaining time in current loop
        /// </summary>
        public float CurrentCycleRemainingTime => Mathf.Max(Interval - CurrentCycleElapsedTime, 0);

        /// <summary>
        /// Checks whether this timer is ok to be removed
        /// </summary>
        public bool ShouldClear => m_Action == null || RemainingTime == 0 || !IsValid(Owner);

        /// <summary>
        /// Checks if the timer is paused
        /// </summary>
        public bool IsPaused => m_bIsPaused;

        /// <summary>
        /// Pause / Inpause timer
        /// </summary>
        public void SetPaused(bool bPause) { m_bIsPaused = bPause; }




        /// <summary>
        ///     Compare frequency (calls per second)
        /// </summary>
        public static bool operator >(Timer A, Timer B) { return (A == null || B == null) ? true : A.Interval < B.Interval; }

        /// <summary>
        ///     Compare frequency (calls per second)
        /// </summary>
        public static bool operator <(Timer A, Timer B) { return (A == null || B == null) ? true : A.Interval > B.Interval; }

        /// <summary>
        ///     Compare frequency (calls per second)
        /// </summary>
        public static bool operator >=(Timer A, Timer B) { return (A == null || B == null) ? true : A.Interval <= B.Interval; }

        /// <summary>
        ///     Compare frequency (calls per second)
        /// </summary>
        public static bool operator <=(Timer A, Timer B) { return (A == null || B == null) ? true : A.Interval >= B.Interval; }

        static bool IsValid(object obj)
        {
            return obj is UnityEngine.Object unityObj
                ? unityObj
                : obj != null;
        }
    }
}