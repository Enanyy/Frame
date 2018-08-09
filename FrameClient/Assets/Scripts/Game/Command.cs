using Network;
using System;
using PBMessage;


public class Command
{
    private long mID;
    private long mFrame;
    private int mType;
    private byte[] mData;
    private long mTime;

    public long id { get { return mID; } }

    public long frame { get { return mFrame; } }
    public int type { get { return mType; } }
    public byte[] data { get { return mData; } }
    public long time { get { return mTime; } }

    public Command() { }


    public Command(long id, long frame, int type, byte[] data, long time)
    {
        mID = id;
        mFrame = frame;
        mType = type;
        mData = data;
        mTime = time;
    }

    public void Set<T>(CommandID type, T t) where T : class, ProtoBuf.IExtensible
    {
        mType = (int)type;
        mData = ProtoTransfer.SerializeProtoBuf<T>(t);
    }

    public void SetFrame(long frame, long time)
    {
        mFrame = frame;
        mTime = time;
    }

    public T Get<T>() where T : class, ProtoBuf.IExtensible
    {
        T t = ProtoTransfer.DeserializeProtoBuf<T>(mData);
        return t;
    }

    public object Get(Type type)
    {
        return ProtoTransfer.DeserializeProtoBuf(mData, type);
    }
}

