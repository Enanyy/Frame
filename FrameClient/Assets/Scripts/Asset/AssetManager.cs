using System;
using UnityEngine;

public class AssetManager : Singleton<AssetManager>
{
    public T Load<T>(string path) where T:UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }
}

