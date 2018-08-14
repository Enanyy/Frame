using System;
using System.Collections;
using System.Collections.Generic;

public class GUID
{
    public static long Int64()
    {
        byte[] buffer = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt64(buffer, 0);
    }

    public static int Int32()
    {
        byte[] buffer = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt32(buffer, 0);
    }
}


