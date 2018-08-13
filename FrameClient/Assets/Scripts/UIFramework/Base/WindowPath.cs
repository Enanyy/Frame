using System;
using System.Collections.Generic;

public class WindowPath
{
    readonly static Dictionary<string, string> windowPath = new Dictionary<string, string>
    {
        {typeof(UI_Main).ToString(),"UI/UI_Main/UI_Main" },
          
    };

    public static string Get<T>() where T: BaseWindow
    {
        string type = typeof(T).ToString();

        if(windowPath.ContainsKey(type))
        {
            return windowPath[type];
        }
        return "";
    }
}


