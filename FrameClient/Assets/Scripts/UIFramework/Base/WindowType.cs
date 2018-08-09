using System;

public enum WindowType
{
    /// <summary>
    /// 一般的
    /// </summary>
    Normal,//
    /// <summary>
    /// 一直处于栈底的 只能有唯一的
    /// </summary>
    Root,  //
    /// <summary>
    /// 弹出框，不隐藏上一个窗口
    /// </summary>
    Pop,   //
}

