using System;
using System.Collections.Generic;

public class WindowPath
{
    readonly static Dictionary<string, string> windowPath = new Dictionary<string, string>
    {
        {typeof(UI_Main).ToString(),"UI/UI_Main/UI_Main" },
        {typeof(UI_ScaleWindow).ToString(),"UI/UI_ScaleWindow/UI_ScaleWindow" },
        {typeof(UI_FadeWindow).ToString(),"UI/UI_FadeWindow/UI_FadeWindow" },
        {typeof(UI_MoveWindow).ToString(),"UI/UI_MoveWindow/UI_MoveWindow" },
        {typeof(UI_PopWindow).ToString(),"UI/UI_PopWindow/UI_PopWindow" },
        {typeof(UI_Dialog).ToString(),"UI/UI_Dialog/UI_Dialog" },      
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


