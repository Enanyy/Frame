using System;
using System.Collections;
using System.Collections.Generic;

public class GUID
{
	public static long GenerateID()
	{
		byte[] buffer = Guid.NewGuid().ToByteArray();  
		return BitConverter.ToInt64(buffer, 0);  
	}
}


