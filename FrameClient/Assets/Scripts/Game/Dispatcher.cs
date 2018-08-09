using System;
using System.Collections.Generic;

public interface IReceiverHandler
{
    void RegisterReceiver();
    void UnRegisterReceiver();
}

public class Dispatcher
{
    private interface IReceiver
    {
        bool IsType(Type type);
        void Invoke(object data);
        bool Equals(IReceiver receiver);
    }

    private class Receiver<T> : IReceiver 
    {
        private Action<T> mReceiver;

        public Receiver(Action<T> receiver)
        {
            mReceiver = receiver;
        }

        public bool IsType(Type type)
        {
            return typeof(T) == type;
        }

        public void Invoke(T data)
        {
            if (mReceiver != null)
            {
                mReceiver(data);
            }
        }

        public void Invoke(object data)
        {
            Invoke((T)data);           
        }

        public bool Equals(IReceiver receiver)
        {
            if (receiver == null)
            {
                return false;
            }

            if (receiver.IsType(typeof(T)) == false)
            {
                return false;
            }

            Receiver<T> o = receiver as Receiver<T>;
            if (o == null)
            {
                return false;
            }
            return o.mReceiver == mReceiver;
        }
    }

    private Dictionary<int, List<IReceiver>> mReceiverDic = new Dictionary<int, List<IReceiver>>();
    private Dictionary<int, Type> mReceiverTypeDic = new Dictionary<int, Type>();

    public bool RegisterReceiver<T>(int id, Action<T> action) 
    {
        if (action == null) return false;

        if (mReceiverTypeDic.ContainsKey(id) == false)
        {
            mReceiverTypeDic.Add(id, typeof(T));
        }

        Receiver<T> receiver = new Receiver<T>(action);


        if (mReceiverDic.ContainsKey(id))
        {
            for (int i = 0; i < mReceiverDic[id].Count; ++i)
            {
                if (mReceiverDic[id][i].Equals(receiver))
                {
                    return false;
                }
            }
        }
        else
        {
            mReceiverDic.Add(id, new List<IReceiver>());
        }

        mReceiverDic[id].Add(receiver);

        return true;

    }
    public void UnRegisterReceiver<T>(int id, Action<T> action) 
    {
        if (mReceiverDic.ContainsKey(id))
        {
            Receiver<T> receiver = new Receiver<T>(action);

            for (int i = mReceiverDic[id].Count - 1; i >= 0; --i)
            {
                if (mReceiverDic[id][i].Equals(receiver))
                {
                    mReceiverDic[id].RemoveAt(i);
                }
            }

            if (mReceiverDic[id].Count == 0)
            {
                mReceiverTypeDic.Remove(id);
            }
        }
    }

    public void Dispatch<T>(int id, T data)
    {
        if (mReceiverDic.ContainsKey(id))
        {
            for (int i = 0; i < mReceiverDic[id].Count; ++i)
            {
                IReceiver receiver = mReceiverDic[id][i];
                if (receiver != null && receiver.IsType(typeof(T)))
                {
                    Receiver<T> o = receiver as Receiver<T>;
                    if (o != null)
                    {
                        o.Invoke(data);
                    }
                }
            }
        }
    }

    public void Dispatch(int id, object data, Type type)
    {
        if (mReceiverDic.ContainsKey(id))
        {
            for (int i = 0; i < mReceiverDic[id].Count; ++i)
            {
                IReceiver receiver = mReceiverDic[id][i];
                if (receiver != null && receiver.IsType(type))
                {
                    receiver.Invoke(data);
                }
            }
        }
    }

    public Type GetType(int id)
    {
        if (mReceiverTypeDic.ContainsKey(id))
        {
            return mReceiverTypeDic[id];
        }
        return null;
    }

    public void Clear()
    {
        mReceiverDic.Clear();
        mReceiverTypeDic.Clear();
    }
}

