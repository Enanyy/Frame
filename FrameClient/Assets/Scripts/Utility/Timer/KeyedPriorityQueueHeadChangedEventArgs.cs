
using System;
using System.Runtime;

public sealed class KeyedPriorityQueueHeadChangedEventArgs<T> : EventArgs where T : class
{
    private T newFirstElement;
    private T oldFirstElement;

    public KeyedPriorityQueueHeadChangedEventArgs(T oldFirstElement, T newFirstElement)
    {
        this.oldFirstElement = oldFirstElement;
        this.newFirstElement = newFirstElement;
    }

    public T NewFirstElement
    {
        get
        {
            return this.newFirstElement;
        }
    }

    public T OldFirstElement
    {
        get
        {
            return this.oldFirstElement;
        }
    }
}
