using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventDispatch
{
    static Dispatcher mDispatcher = new Dispatcher();

    public static bool RegisterReceiver<T>(EventID varEventID, Action<T> action) 
    {
        return mDispatcher.RegisterReceiver((int)varEventID, action);
    }
    public static void UnRegisterReceiver<T>(EventID varEventID, Action<T> action) 
    {
        int id = (int)varEventID;
        mDispatcher.UnRegisterReceiver(id, action);
    }

    public static void Dispatch<T>(EventID varEventID, T data)
    {
        mDispatcher.Dispatch((int)varEventID, data);
    }

    public static Type GetType(EventID varEventID)
    {
        return mDispatcher.GetType((int)varEventID);
    }

    public static void Clear()
    {
        mDispatcher.Clear();
    }
}


