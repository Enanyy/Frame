/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Timer.cs
//	?VisionPunk, Minea Softworks. All Rights Reserved.
//
//	description:	vp_Timer is a script extension for delaying (scheduling) methods
//					in Unity. it supports arguments, delegates, repetition with
//					intervals, infinite repetition, pausing and canceling, and uses
//					an object pool in order to feed the garbage collector with an
//					absolute minimum amount of data
//
/////////////////////////////////////////////////////////////////////////////////

//#define DEBUG	// uncomment to display timers in the Unity Editor Hierarchy

using UnityEngine;
using System;
using System.Collections.Generic;


#if (UNITY_EDITOR && DEBUG)
using System.Diagnostics;
#endif


public class vp_Timer : MonoBehaviour
{

    private static GameObject m_MainObject = null;

    private static List<Event> m_Active = new List<Event>();
    private static List<Event> m_Pool = new List<Event>();

    private static Event m_NewEvent = null;
    private static int m_EventCount = 0;

    // variables for the Update method
    private static int m_EventBatch = 0;
    private static int m_EventIterator = 0;

    /// <summary>
    /// the maximum amount of callbacks that the timer system will be
    /// allowed to execute in one frame. reducing this may improve
    /// performance with extreme amounts (hundreds) of active timers
    /// but may also lead to some events getting delayed for a few
    /// frames
    /// </summary>
    public static int MaxEventsPerFrame = 500;

    /// <summary>
    /// timer event callback for methods with no parameters
    /// </summary>
    public delegate void Callback();

    /// <summary>
    /// timer event callback for methods with parameters
    /// </summary>
    public delegate void ArgCallback(object args);

    /// <summary>
    /// data struct for use by the editor class
    /// </summary>
    public struct Stats
    {
        public int Created;
        public int Inactive;
        public int Active;
    }


    /// <summary>
    /// returns false if this script was not added via the 'vp_Timer.In'
    /// method - e.g. you may have dragged it to a gameobject in
    /// the Inspector which won't work
    /// </summary>
    public bool WasAddedCorrectly
    {
        get
        {
            if (!Application.isPlaying)
                return false;
            if (gameObject != m_MainObject)
                return false;
            return true;
        }
    }


    /// <summary>
    /// in Awake a check is made to see if the vp_Timer component
    /// was added correctly. if not it will be destroyed
    /// </summary>
    private void Awake()
    {

        if (!WasAddedCorrectly)
        {
            Destroy(this);
            return;
        }

    }


    /// <summary>
    /// in Update the active list is looped every frame, executing
    /// any timer events for which time is up. Update also detects
    /// paused and canceled events
    /// </summary>
    private void Update()
    {

        //	NOTE: this method never processes more than 'MaxEventsPerFrame',
        // in order to avoid performance problems with excessive amounts of
        // timers. this may lead to events being delayed a few frames.
        // if experiencing delayed events 1) try to cut back on the amount
        // of timers created simultaneously, and 2) increase 'MaxEventsPerFrame'

        // execute any active events that are due, but only check
        // up to max events per frame for performance
        m_EventBatch = 0;
        while ((vp_Timer.m_Active.Count > 0) && m_EventBatch < MaxEventsPerFrame)
        {

            // if we reached beginning of list, halt until next frame
            if (m_EventIterator < 0)
            {
                // this has two purposes: 1) preventing multiple iterations
                // per frame if our event count is below the maximum, and
                // 2) preventing reaching index -1
                m_EventIterator = vp_Timer.m_Active.Count - 1;
                break;
            }

            // prevent index out of bounds
            if (m_EventIterator > vp_Timer.m_Active.Count - 1)
                m_EventIterator = vp_Timer.m_Active.Count - 1;

            // execute all due events
            if (Time.time >= vp_Timer.m_Active[m_EventIterator].DueTime ||	// time is up
                vp_Timer.m_Active[m_EventIterator].Id == 0)					// event has been canceled ('Execute' will kill it)
                vp_Timer.m_Active[m_EventIterator].Execute();
            else
            {
                // handle pausing
                if (vp_Timer.m_Active[m_EventIterator].Paused)
                    vp_Timer.m_Active[m_EventIterator].DueTime += Time.deltaTime;
                else
                    // log lifetime
                    vp_Timer.m_Active[m_EventIterator].LifeTime += Time.deltaTime;
            }

            // going backwards since 'Execute' will remove items from the list
            m_EventIterator--;
            m_EventBatch++;
        }

    }


    /// <summary>
    /// the "In" method is used for scheduling events. it always takes a
    /// delay along with a callback (method or delegate) to be triggered
    /// at the end of the delay. the overloads support arguments, iterations
    /// and intervals. an optional "Event.Handle" object can also be passed
    /// as the last parameter. this will be associated to the scheduled
    /// event and will enable interaction with the event post-initiation,
    /// along with extraction of a number of useful parameters
    /// </summary>

    // time + callback + [timer handle]
    public static void In(float delay, Callback callback, Handle timerHandle = null)
    { Schedule(delay, callback, null, null, timerHandle, 1, -1.0f); }

    // time + callback + iterations + [timer handle]
    public static void In(float delay, Callback callback, int iterations, Handle timerHandle = null)
    { Schedule(delay, callback, null, null, timerHandle, iterations, -1.0f); }

    // time + callback + iterations + interval + [timer handle]
    public static void In(float delay, Callback callback, int iterations, float interval, Handle timerHandle = null)
    { Schedule(delay, callback, null, null, timerHandle, iterations, interval); }

    // time + callback + arguments + [timer handle]
    public static void In(float delay, ArgCallback callback, object arguments, Handle timerHandle = null)
    { Schedule(delay, null, callback, arguments, timerHandle, 1, -1.0f); }

    // time + callback + arguments + iterations + [timer handle]
    public static void In(float delay, ArgCallback callback, object arguments, int iterations, Handle timerHandle = null)
    { Schedule(delay, null, callback, arguments, timerHandle, iterations, -1.0f); }

    // time + callback + arguments + iterations + interval + [timer handle]
    public static void In(float delay, ArgCallback callback, object arguments, int iterations, float interval, Handle timerHandle = null)
    { Schedule(delay, null, callback, arguments, timerHandle, iterations, interval); }


    /// <summary>
    /// the "Start" method is used to run a timer for the sole
    /// purpose of measuring time (useful for e.g. stopwatches).
    /// it takes a mandatory timer handle as only input argument,
    /// has no callback method and will run practically forever.
    /// the timer handle can then be used to pause, resume and
    /// poll all sorts of info from the timer event
    /// </summary>
    public static void Start(Handle timerHandle)
    {
        Schedule(315360000.0f, /* ten years, yo ;) */ delegate() { }, null, null, timerHandle, 1, -1.0f);
    }


    /// <summary>
    /// the 'Schedule' method sets everything in order for the
    /// timer event to be fired. it also creates a hidden
    /// gameobject upon the first time called (for purposes of
    /// running the Update loop and drawing editor debug info)
    /// </summary>
    private static void Schedule(float time, Callback func, ArgCallback argFunc, object args, Handle timerHandle, int iterations, float interval)
    {

        if (func == null && argFunc == null)
        {
            UnityEngine.Debug.LogError("Error: (vp_Timer) Aborted event because function is null.");
            return;
        }

        // setup main gameobject
        if (m_MainObject == null)
        {
            m_MainObject = new GameObject("Timers");
            m_MainObject.AddComponent<vp_Timer>();
            UnityEngine.Object.DontDestroyOnLoad(m_MainObject);

#if (UNITY_EDITOR && !DEBUG)
				m_MainObject.gameObject.hideFlags = HideFlags.HideInHierarchy;
#endif
        }

        // force healthy time values
        time = Mathf.Max(0.0f, time);
        iterations = Mathf.Max(0, iterations);
        interval = (interval == -1.0f) ? time : Mathf.Max(0.0f, interval);

        // recycle an event - or create a new one if the pool is empty
        m_NewEvent = null;
        if (m_Pool.Count > 0)
        {
            m_NewEvent = m_Pool[0];
            m_Pool.Remove(m_NewEvent);
        }
        else
            m_NewEvent = new Event();

        // iterate the event counter and store the id for this event
        vp_Timer.m_EventCount++;
        m_NewEvent.Id = vp_Timer.m_EventCount;

        // set up the event with its function, arguments and times
        if (func != null)
            m_NewEvent.Function = func;
        else if (argFunc != null)
        {
            m_NewEvent.ArgFunction = argFunc;
            m_NewEvent.Arguments = args;
        }
        m_NewEvent.StartTime = Time.time;
        m_NewEvent.DueTime = Time.time + time;
        m_NewEvent.Iterations = iterations;
        m_NewEvent.Interval = interval;
        m_NewEvent.LifeTime = 0.0f;
        m_NewEvent.Paused = false;

        // add event to the Active list
        vp_Timer.m_Active.Add(m_NewEvent);

        // if a timer handle was provided, associate it to this event,
        // but first cancel any already active event using the same
        // handle: there can be only one ...
        if (timerHandle != null)
        {
            if (timerHandle.Active)
                timerHandle.Cancel();
            // setting the 'Id' property associates this handle with
            // the currently active event with the corresponding id
            timerHandle.Id = m_NewEvent.Id;
        }

#if (UNITY_EDITOR && DEBUG)
		m_NewEvent.StoreCallingMethod();
		EditorRefresh();
#endif

    }


    /// <summary>
    /// cancels a timer if the passed timer handle is still active
    /// </summary>
    private static void Cancel(vp_Timer.Handle handle)
    {

        if (handle == null)
            return;

        // NOTE: the below Active check is super-important for verifying timer
        // handle integrity. recycling 'handle.Event' if 'handle.Active' is false
        // will cancel the wrong event and lead to some other timer never firing
        // (this is because timer events are recycled but not their timer handles).
        if (handle.Active)
        {
            // setting the 'Id' property to zero will result in DueTime also
            // becoming zero, sending the event to 'Execute' in the next frame
            // where it will be recycled instead of executed
            handle.Id = 0;
            return;
        }

    }


    /// <summary>
    /// cancels every currently active timer
    /// </summary>
    public static void CancelAll()
    {

        for (int t = vp_Timer.m_Active.Count - 1; t > -1; t--)
        {
            vp_Timer.m_Active[t].Id = 0;
        }

    }


    /// <summary>
    /// cancels every currently active timer that points to a
    /// certain method
    /// </summary>
    public static void CancelAll(string methodName)
    {

        for (int t = vp_Timer.m_Active.Count - 1; t > -1; t--)
        {
            if (vp_Timer.m_Active[t].MethodName == methodName)
                vp_Timer.m_Active[t].Id = 0;
        }

    }


    /// <summary>
    /// clears all currently active timers along with the object
    /// pool, that is: disposes of every timer event, releasing
    /// memory to the garbage collector
    /// </summary>
    public static void DestroyAll()
    {
        vp_Timer.m_Active.Clear();
        vp_Timer.m_Pool.Clear();

#if (UNITY_EDITOR && DEBUG)
		EditorRefresh();
#endif

    }


    /// <summary>
    /// provides the custom vp_Timer editor class with debug info
    /// </summary>
    public static Stats EditorGetStats()
    {

        Stats stats;
        stats.Created = m_Active.Count + m_Pool.Count;
        stats.Inactive = m_Pool.Count;
        stats.Active = m_Active.Count;
        return stats;

    }


    /// <summary>
    /// provides the custom vp_Timer editor class with the name of a
    /// specific method in the Active list along with any arguments,
    /// plus a stack trace (if compiling with the DEBUG define)
    /// </summary>
    public static string EditorGetMethodInfo(int eventIndex)
    {

        if (eventIndex < 0 || eventIndex > m_Active.Count - 1)
            return "Argument out of range.";

        return m_Active[eventIndex].MethodInfo;

    }


    /// <summary>
    /// provides the custom vp_Timer editor class with the id of a
    /// specific event in the Active list
    /// </summary>
    public static int EditorGetMethodId(int eventIndex)
    {

        if (eventIndex < 0 || eventIndex > m_Active.Count - 1)
            return 0;

        return m_Active[eventIndex].Id;

    }


#if (DEBUG && UNITY_EDITOR)
	/// <summary>
	/// updates the name of the main gamobject, visible in the
	/// hierarchy view during runtime (if compiling with the
	/// DEBUG define)
	/// </summary>
	private static void EditorRefresh()
	{
		m_MainObject.name = "Timers (" + m_Active.Count + " / " + (m_Pool.Count + m_Active.Count).ToString() + ")";
	}
#endif


    /////////////////////////////////////////////////////////////////////////////////
    //
    //	vp_Timer.Event
    //
    //	description:	this class is the internal representation of an event object.
    //					it is not to be manipulated directly since events are recycled
    //					and references to them would be unreliable. to interact with
    //					an event post initiation, schedule it with a 'vp_Timer.Handle'
    //
    /////////////////////////////////////////////////////////////////////////////////
    private class Event
    {

        public int Id;

        public Callback Function = null;
        public ArgCallback ArgFunction = null;
        public object Arguments = null;

        public int Iterations = 1;
        public float Interval = -1.0f;
        public float DueTime = 0.0f;
        public float StartTime = 0.0f;
        public float LifeTime = 0.0f;
        public bool Paused = false;

#if (DEBUG && UNITY_EDITOR)
		private string m_CallingMethod = "";
#endif


        /// <summary>
        /// runs an event function, taking care of iterations, intervals
        /// canceling and recycling
        /// </summary>
        public void Execute()
        {

            // if either 'Id' or 'DueTime' is zero, this timer has been
            // canceled: recycle it!
            if (Id == 0 || DueTime == 0.0f)
            {
                Recycle();
                return;
            }

            // attempt to execute the callback function
            if (Function != null)
                Function();
            else if (ArgFunction != null)
                ArgFunction(Arguments);
            else
            {
                // function was null: nothing to do so abort
                Error("Aborted event because function is null.");
                Recycle();
                return;
            }

            // count down to recycling
            if (Iterations > 0)
            {
                Iterations--;
                // usually a timer has one default iteration and will
                // enter the below scope to get recycled right away ...
                if (Iterations < 1)
                {
                    Recycle();
                    return;
                }
            }

            // ... but if we end up here the timer either has atleast one
            // iteration left, or user set iterations to zero for infinite
            // repetition. either way: update due time with the interval
            DueTime = Time.time + Interval;

        }


        /// <summary>
        /// performs internal recycling of the vp_Timer
        /// </summary>
        private void Recycle()
        {

            Id = 0;
            DueTime = 0.0f;
            StartTime = 0.0f;

            Function = null;
            ArgFunction = null;
            Arguments = null;

            if (vp_Timer.m_Active.Remove(this))
                m_Pool.Add(this);

#if (UNITY_EDITOR && DEBUG)
			EditorRefresh();
#endif

        }


        /// <summary>
        /// destroys this timer event forever, releasing its memory
        /// to the garbage collector
        /// </summary>
        private void Destroy()
        {

            vp_Timer.m_Active.Remove(this);
            vp_Timer.m_Pool.Remove(this);

        }


#if (UNITY_EDITOR && DEBUG)
		/// <summary>
		/// used by the debug mode to fetch the callstack
		/// </summary>
		public void StoreCallingMethod()
		{
			StackTrace stackTrace = new StackTrace();

			string result = "";
			string declaringType = "";
			for (int v = 3; v < stackTrace.FrameCount; v++)
			{
				StackFrame stackFrame = stackTrace.GetFrame(v);
				declaringType = stackFrame.GetMethod().DeclaringType.ToString();
				result += " <- " + declaringType + ":" + stackFrame.GetMethod().Name.ToString();
			}

			m_CallingMethod = result;

		}
#endif

        /// <summary>
        /// standard event error method
        /// </summary>
        private void Error(string message)
        {

            string msg = "Error: (vp_Timer.Event) " + message;
#if (UNITY_EDITOR && DEBUG)
			msg += MethodInfo;
#endif
            UnityEngine.Debug.LogError(msg);

        }


        /// <summary>
        /// returns the name of the scheduled method, or 'delegate'
        /// </summary>
        public string MethodName
        {
            get
            {
                if (Function != null)
                {
                    if (Function.Method != null)
                    {
                        if (Function.Method.Name[0] == '<')
                            return "delegate";
                        else return Function.Method.Name;
                    }
                }
                else if (ArgFunction != null)
                {
                    if (ArgFunction.Method != null)
                    {
                        if (ArgFunction.Method.Name[0] == '<')
                            return "delegate";
                        else return ArgFunction.Method.Name;
                    }
                }
                return null;
            }
        }


        /// <summary>
        /// returns the name of the scheduled method along with any arguments,
        /// plus a stack trace (if compiling with the DEBUG define)
        /// </summary>
        public string MethodInfo
        {
            get
            {
                string s = MethodName;
                if (!string.IsNullOrEmpty(s))
                {
                    s += "(";
                    if (Arguments != null)
                    {
                        if (Arguments.GetType().IsArray)
                        {
                            object[] array = (object[])Arguments;
                            foreach (object o in array)
                            {
                                s += o.ToString();
                                if (Array.IndexOf(array, o) < array.Length - 1)
                                    s += ", ";
                            }
                        }
                        else
                            s += Arguments;
                    }
                    s += ")";
                }
                else
                    s = "(function = null)";

#if (DEBUG && UNITY_EDITOR)
				s += m_CallingMethod;
#endif
                return s;
            }
        }

    }


    /////////////////////////////////////////////////////////////////////////////////
    //
    //	vp_Timer.Handle
    //
    //	description:	this class is used to keep track of a currently running event.
    //					it is most commonly used to cancel an event or to see if it's
    //					still active, but it also has many properties to analyze the
    //					state of an event. the editor uses it to display debug info
    //
    /////////////////////////////////////////////////////////////////////////////////
    public class Handle
    {

        private vp_Timer.Event m_Event = null;	// timer we're pointing at
        private int m_Id = 0;					// id associated with timer upon creation of this handle
        private int m_StartIterations = 1;		// the amount of iterations of the event when started
        private float m_FirstDueTime = 0.0f;	// the initial execution delay of the event

        /// <summary>
        /// pauses or unpauses this timer event
        /// </summary>
        public bool Paused
        {
            get
            {
                return Active && m_Event.Paused;
            }
            set
            {
                if (Active)
                    m_Event.Paused = value;
            }
        }

        //////// fixed times ////////

        /// <summary>
        /// returns the time of initial scheduling
        /// </summary>
        public float TimeOfInitiation
        {
            get
            {
                if (Active)
                    return m_Event.StartTime;
                else return 0.0f;
            }
        }

        /// <summary>
        /// returns the time of first execution
        /// </summary>
        public float TimeOfFirstIteration
        {
            get
            {
                if (Active)
                    return m_FirstDueTime;
                return 0.0f;
            }
        }

        /// <summary>
        /// returns the expected due time of the next iteration of an event
        /// </summary>
        public float TimeOfNextIteration
        {
            get
            {
                if (Active)
                    return m_Event.DueTime;
                return 0.0f;
            }
        }

        /// <summary>
        /// returns the expected due time of the last iteration of an event
        /// </summary>
        public float TimeOfLastIteration
        {
            get
            {
                if (Active)
                    return Time.time + DurationLeft;
                return 0.0f;
            }
        }

        //////// timespans ////////

        /// <summary>
        /// returns the delay before first execution
        /// </summary>
        public float Delay
        {
            get
            {
                return (Mathf.Round((m_FirstDueTime - TimeOfInitiation) * 1000.0f) / 1000.0f);
            }
        }

        /// <summary>
        /// returns the repeat interval of an event
        /// </summary>
        public float Interval
        {
            get
            {
                if (Active)
                    return m_Event.Interval;
                return 0.0f;
            }
        }

        /// <summary>
        /// returns the time left until the next iteration of an event
        /// </summary>
        public float TimeUntilNextIteration
        {
            get
            {
                if (Active)
                    return m_Event.DueTime - Time.time;
                return 0.0f;
            }
        }

        /// <summary>
        /// returns the current total time left (all iterations) of an event
        /// </summary>
        public float DurationLeft
        {
            get
            {
                if (Active)
                    return TimeUntilNextIteration + ((m_Event.Iterations - 1) * m_Event.Interval);
                return 0.0f;
            }
        }

        /// <summary>
        /// returns the total expected duration (due time + all iterations) of an event.
        /// NOTE: this does not take pausing into account.
        /// </summary>
        public float DurationTotal
        {
            get
            {
                if (Active)
                {
                    return Delay +
                        ((m_StartIterations) * ((m_StartIterations > 1) ? Interval : 0.0f));
                }
                return 0.0f;
            }
        }


        /// <summary>
        /// returns the current age of an event (lifetime since initiation)
        /// </summary>
        public float Duration
        {
            get
            {
                if (Active)
                    return m_Event.LifeTime;
                return 0.0f;
            }
        }

        //////// iterations ////////

        /// <summary>
        /// returns the total expected amount of iterations of an event upon initiation
        /// </summary>
        public int IterationsTotal
        {
            get
            {
                return m_StartIterations;
            }
        }

        /// <summary>
        /// returns the number of iterations left of an event
        /// </summary>
        public int IterationsLeft
        {
            get
            {
                if (Active)
                    return m_Event.Iterations;
                return 0;
            }
        }

        //////// main data ////////

        /// <summary>
        /// returns the id of this event handle, which is also the id of
        /// the associated event (if not, the handle is considered inactive).
        /// Setting this property directly is not recommended
        /// </summary>
        public int Id
        {
            get
            {
                return m_Id;
            }
            set
            {
                // setting the property associates this handle with a
                // currently active event with the same id, or null.
                m_Id = value;

                // if 'Id' is being set to zero, cancel the event immediately
                // by setting due time to zero and return
                if (m_Id == 0)
                {
                    m_Event.DueTime = 0.0f;
                    return;
                }

                // find the associated event. most likely it is the last created one in
                // the active list, which is instantly found by looping backwards.
                // if not, it might be some other event that a brave user wishes to
                // find and hook up manually (this is OK, though not recommended)
                m_Event = null;
                for (int t = vp_Timer.m_Active.Count - 1; t > -1; t--)
                {
                    if (vp_Timer.m_Active[t].Id == m_Id)
                    {
                        m_Event = vp_Timer.m_Active[t];
                        break;
                    }
                }
                if (m_Event == null)
                    UnityEngine.Debug.LogError("Error: (vp_Timer.Handle) Failed to assign event with Id '" + m_Id + "'.");

                // store some initial event info
                m_StartIterations = m_Event.Iterations;
                m_FirstDueTime = m_Event.DueTime;

            }

        }

        /// <summary>
        /// returns true if the timer event of this handle is active
        /// (running or paused). rReturns false if not, or if this
        /// timer handle is no longer valid
        /// </summary>
        public bool Active
        {
            // NOTE: this property verifies timer handle integrity.
            // if 'Event.Id' and 'Id' differ, the timer handle is no
            // longer valid and using its event may cause bugs
            get
            {
                if (m_Event == null || Id == 0 || m_Event.Id == 0)
                    return false;
                return m_Event.Id == Id;
            }
        }

        /// <summary>
        /// returns the name of the scheduled method, or 'delegate'
        /// </summary>
        public string MethodName { get { return m_Event.MethodName; } }

        /// <summary>
        /// returns the name of the scheduled method along with any arguments,
        /// plus a stack trace (if compiling with the DEBUG define)
        /// </summary>
        public string MethodInfo { get { return m_Event.MethodInfo; } }


        /// <summary>
        /// cancels the event associated with this handle, if active
        /// </summary>
        public void Cancel()
        {
            vp_Timer.Cancel(this);
        }


        /// <summary>
        /// executes the event associated with this handle early, if active
        /// </summary>
        public void Execute()
        {
            m_Event.DueTime = Time.time;
        }

    }


}
