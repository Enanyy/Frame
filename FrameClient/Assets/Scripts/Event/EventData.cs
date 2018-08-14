using System;
using UnityEngine;
/*
where T : struct 限制类型参数T必须继承自System.ValueType。
where T : class 限制类型参数T必须是引用类型，也就是不能继承自System.ValueType。
where T : new() 限制类型参数T必须有一个缺省的构造函数
where T : NameOfClass 限制类型参数T必须继承自某个类或实现某个接口。
以上这些限定可以组合使用，比如： public class Point where T : class, IComparable, new()
*/
public abstract class IEventData<T>:SharedValue<T> where T:class,new()
{
    public virtual void Clear()
    {

    }
}



public class EventConnect:IEventData<EventConnect>
{
    public string ip;
    public int tcpPort;
    public int udpPort;

    public override void Clear()
    {
        ip = "";
        tcpPort = 0;
        udpPort = 0;
    }
}

public  class EventPlayerCreate:IEventData<EventPlayerCreate>
{
    public int roleid;
    public string name;
    public int maxBlood;
    public int nowBlood;
    public int type;

    public override void Clear()
    {
        roleid = 0;
        name = "";
        maxBlood = 0;
        nowBlood = 0;
        type = 0;
    }
}



public class EventPlayerMove:IEventData<EventPlayerMove>
{
    public int roleid;
    public Vector3 position;


    public override void Clear()
    {
        roleid = 0;
        position = Vector3.zero;
    }
}

public class EventPlayerRemove:IEventData<EventPlayerRemove>
{
    public int roleid;

    public override void Clear()
    {
        roleid = 0;
    }
}

public class EventPlayerBloodChange:IEventData<EventPlayerBloodChange>
{ 
    public int roleid;
    public int maxBlood;
    public int nowBlood;

    public int interval; //>0加血 <0掉血

    public override void Clear()
    {
        roleid = 0;
        maxBlood = 0;
        nowBlood = 0;
        interval = 0;
    }
}