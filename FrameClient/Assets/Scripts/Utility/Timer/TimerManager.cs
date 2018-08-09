using UnityEngine;
using System.Collections;
using System;

public class TimerManager
{
    public static TimerBehaviour GetTimer(GameObject target)
    {
        TimerBehaviour obj = target.GetComponent<TimerBehaviour>();
        if (obj == null)
        {
            obj = target.AddComponent<TimerBehaviour>();
        }
        return obj;
    }
}
