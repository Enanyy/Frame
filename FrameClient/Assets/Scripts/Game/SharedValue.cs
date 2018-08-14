using System;

public abstract class SharedValue<T> where T:new()
{
    private static T t;
    /// <summary>
    /// 使用静态变量
    /// </summary>
    public static T sData
    {
        get
        {
            if (t == null)
            {
                t = new T();
            }
            return t;
        }
    }
}

