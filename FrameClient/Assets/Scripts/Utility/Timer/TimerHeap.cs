using System;
using System.Diagnostics;

namespace Util
{
    /// <summary>
    /// 定时触发器
    /// </summary>
    public class TimerHeap
    {
        private static uint m_nNextTimerId;
        private static uint m_unTick;
        private static KeyedPriorityQueue<uint, AbsTimerData, ulong> m_queue;
        private static Stopwatch m_stopWatch;
        private static readonly object m_queueLock = new object();

        /// <summary>
        /// 私有构造函数，封闭实例化。
        /// </summary>
        private TimerHeap() { }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        static TimerHeap()
        {
            m_queue = new KeyedPriorityQueue<uint, AbsTimerData, ulong>();
            m_stopWatch = new Stopwatch();
        }

        /// <summary>
        /// 添加定时对象
        /// </summary>
        /// <param name="start">延迟启动时间。（毫秒）</param>
        /// <param name="interval">重复间隔，为零不重复。（毫秒）</param>
        /// <param name="handler">定时处理方法</param>
        /// <returns>定时对象Id</returns>
        public static uint AddTimer(uint start, int interval, Action handler)
        {
            //起始时间会有一个tick的误差,tick精度越高,误差越低
            var p = GetTimerData(new TimerData(), start, interval);
            p.Action = handler;
            return AddTimer(p);
        }

        /// <summary>
        /// 添加定时对象
        /// </summary>
        /// <typeparam name="T">参数类型1</typeparam>
        /// <param name="start">延迟启动时间。（毫秒）</param>
        /// <param name="interval">重复间隔，为零不重复。（毫秒）</param>
        /// <param name="handler">定时处理方法</param>
        /// <param name="arg1">参数1</param>
        /// <returns>定时对象Id</returns>
        public static uint AddTimer<T>(uint start, int interval, Action<T> handler, T arg1)
        {
            var p = GetTimerData(new TimerData<T>(), start, interval);
            p.Action = handler;
            p.Arg1 = arg1;
            return AddTimer(p);
        }

        /// <summary>
        /// 添加定时对象
        /// </summary>
        /// <typeparam name="T">参数类型1</typeparam>
        /// <typeparam name="U">参数类型2</typeparam>
        /// <param name="start">延迟启动时间。（毫秒）</param>
        /// <param name="interval">重复间隔，为零不重复。（毫秒）</param>
        /// <param name="handler">定时处理方法</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <returns>定时对象Id</returns>
        public static uint AddTimer<T, U>(uint start, int interval, Action<T, U> handler, T arg1, U arg2)
        {
            var p = GetTimerData(new TimerData<T, U>(), start, interval);
            p.Action = handler;
            p.Arg1 = arg1;
            p.Arg2 = arg2;
            return AddTimer(p);
        }

        /// <summary>
        /// 添加定时对象
        /// </summary>
        /// <typeparam name="T">参数类型1</typeparam>
        /// <typeparam name="U">参数类型2</typeparam>
        /// <typeparam name="V">参数类型3</typeparam>
        /// <param name="start">延迟启动时间。（毫秒）</param>
        /// <param name="interval">重复间隔，为零不重复。（毫秒）</param>
        /// <param name="handler">定时处理方法</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        /// <returns>定时对象Id</returns>
        public static uint AddTimer<T, U, V>(uint start, int interval, Action<T, U, V> handler, T arg1, U arg2, V arg3)
        {
            var p = GetTimerData(new TimerData<T, U, V>(), start, interval);
            p.Action = handler;
            p.Arg1 = arg1;
            p.Arg2 = arg2;
            p.Arg3 = arg3;
            return AddTimer(p);
        }

        /// <summary>
        /// 删除定时对象
        /// </summary>
        /// <param name="timerId">定时对象Id</param>
        public static void DelTimer(uint timerId)
        {
            lock (m_queueLock)
                m_queue.Remove(timerId);
        }

        /// <summary>
        /// 周期调用触发任务
        /// </summary>
        public static void Tick()
        {
            m_unTick += (uint)m_stopWatch.ElapsedMilliseconds;
            m_stopWatch.Reset();
            m_stopWatch.Start();

            while (m_queue.Count != 0)
            {
                AbsTimerData p;
                lock (m_queueLock)
                    p = m_queue.Peek();
                if (m_unTick < p.UnNextTick)
                {
                    break;
                }
                lock (m_queueLock)
                    m_queue.Dequeue();
                if (p.NInterval > 0)
                {
                    p.UnNextTick += (ulong)p.NInterval;
                    lock (m_queueLock)
                        m_queue.Enqueue(p.NTimerId, p, p.UnNextTick);
                    p.DoAction();
                }
                else
                {
                    p.DoAction();
                }
            }
        }

        /// <summary>
        /// 重置定时触发器
        /// </summary>
        public static void Reset()
        {
            m_unTick = 0;
            m_nNextTimerId = 0;
            lock (m_queueLock)
                while (m_queue.Count != 0)
                    m_queue.Dequeue();
        }

        private static uint AddTimer(AbsTimerData p)
        {
            lock (m_queueLock)
                m_queue.Enqueue(p.NTimerId, p, p.UnNextTick);
            return p.NTimerId;
        }

        private static T GetTimerData<T>(T p, uint start, int interval) where T : AbsTimerData
        {
            p.NInterval = interval;
            p.NTimerId = ++m_nNextTimerId;
            p.UnNextTick = m_unTick + 1 + start;
            return p;
        }
    }
}