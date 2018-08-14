using PBMessage;
using System;
using System.Collections.Generic;

public static class CommandDispatch 
{
    static Dispatcher mDispatcher = new Dispatcher();

    public static bool RegisterReceiver<T>(CommandID varCommandID, Action<T> action) where T : class
    {
        return mDispatcher.RegisterReceiver((int)varCommandID, action);
    }
    public static void UnRegisterReceiver<T>(CommandID varCommandID, Action<T> action) where T : class
    {
        mDispatcher.UnRegisterReceiver((int)varCommandID, action);
    }

    public static void Dispatch(Command cmd)
    {
        if(cmd==null)
        {
            return;
        }
        CommandID id =(CommandID)cmd.type;

        Type type = GetType(id);

        object data = cmd.Get(type);

        if (data ==null)
        {
            return;
        }
        mDispatcher.Dispatch(cmd.type, data, type);
    }

    public static Type GetType(CommandID varCommandID)
    {
        return mDispatcher.GetType((int)varCommandID);
    }

    public static void Clear()
    {
        mDispatcher.Clear();
    }
}

