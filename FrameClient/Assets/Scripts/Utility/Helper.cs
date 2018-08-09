using System;
using UnityEngine;

#if UNITY_EDITOR
using System.Diagnostics;  
#endif


public class Helper
{

	public static void PrintStackTrace()
	{
		#if UNITY_EDITOR
		string tmpStackTraceInfo = null;  
		//设置为true，这样才能捕获到文件路径名和当前行数，当前行数为GetFrames代码的函数，也可以设置其他参数  
		StackTrace st = new StackTrace(true);  
		//得到当前的所以堆栈  
		StackFrame[] sf = st.GetFrames();  
		for (int i = 0; i < sf.Length; ++i)  
		{  
			tmpStackTraceInfo = tmpStackTraceInfo + "\r\n" + " FileName=" + sf[i].GetFileName() + " fullname=" + sf[i].GetMethod().DeclaringType.FullName + " function:" + sf[i].GetMethod().Name + " line:" + sf[i].GetFileLineNumber();           
		}  

		UnityEngine.Debug.Log(tmpStackTraceInfo);

		#endif
	}


	public static void SetLayer(GameObject go,string layerName)
	{
		int layer = LayerMask.NameToLayer (layerName);
		if (go != null) {
			go.layer = layer;
		}
	}
	public static bool IsEquals(Vector3 pos1,Vector3 pos2)
	{
		if (!FLOAT_IS_ZERO(pos2.x - pos1.x))
			return false;
		if (!FLOAT_IS_ZERO(pos2.y - pos1.y))
			return false;
		if (!FLOAT_IS_ZERO(pos2.z - pos1.z))
			return false;
		return true;
	}
	public static float FLOAT_CRITICAL_VALUE = 0.1f;
	public static bool FLOAT_IS_ZERO(float value)
	{
		return Mathf.Abs (value) < FLOAT_CRITICAL_VALUE;
	}

	public static uint StringToUint(string varString)
	{
		uint tmpValue = 0;

		uint.TryParse (varString, out tmpValue);

		return tmpValue;
	}

	public static int StringToInt32(string varString)
	{
		int tmpValue = 0;

		int.TryParse (varString, out tmpValue);

		return tmpValue; 
	}

	public static float StringToFloat(string varString)
	{
		float tmpValue = 0f;

		float.TryParse (varString, out tmpValue);

		return tmpValue; 
	}

	public static Vector3 StringToVector3(string varString)
	{
		Vector3 tmpValue = Vector3.zero;

		if (string.IsNullOrEmpty (varString) || varString.Length < 5) {
			return tmpValue;
		}
		varString = varString.Substring (1, varString.Length - 2);

		string[] tmpStringArray = varString.Split (',');

		if (tmpStringArray.Length == 3) {
			tmpValue.x = StringToFloat (tmpStringArray [0]);
			tmpValue.y = StringToFloat (tmpStringArray [1]);
			tmpValue.z = StringToFloat (tmpStringArray [2]);
		}

		return tmpValue;
	}

	public static bool StringToBool(string varString)
	{
		bool varValue = false;
		bool.TryParse (varString, out varValue);
		return  varValue;
	}

	public static Color StringToColor(string varString)
	{
		Color tmpValue = new Color (0, 0, 0, 0);

		if (string.IsNullOrEmpty (varString) ) {
			return tmpValue;
		}
		varString = varString.Substring (1, varString.Length - 2);

		string[] tmpStringArray = varString.Split (',');

		if (tmpStringArray.Length == 4) {
			tmpValue.r = StringToFloat (tmpStringArray [0]);
			tmpValue.g = StringToFloat (tmpStringArray [1]);
			tmpValue.b = StringToFloat (tmpStringArray [2]);
			tmpValue.a = StringToFloat (tmpStringArray [3]);
		}
		return tmpValue;
	}

	public static Vector4 StringToVector4(string varString)
	{
		Vector4 tmpValue = new Vector4 (0, 0, 0, 0);

		if (string.IsNullOrEmpty (varString) ) {
			return tmpValue;
		}
		varString = varString.Substring (1, varString.Length - 2);

		string[] tmpStringArray = varString.Split (',');

		if (tmpStringArray.Length == 4) {
			tmpValue.x = StringToFloat (tmpStringArray [0]);
			tmpValue.y = StringToFloat (tmpStringArray [1]);
			tmpValue.z = StringToFloat (tmpStringArray [2]);
			tmpValue.w = StringToFloat (tmpStringArray [3]);
		}
		return tmpValue;
	}

	public static AnimationCurve StringToAnimationCurve(System.Security.SecurityElement varElement)
	{
		AnimationCurve tmpAnimationCurve = new AnimationCurve ();

		if (varElement != null && varElement.Tag=="AnimationCurve") {
			
			foreach (System.Security.SecurityElement child in varElement.Children) {
				Keyframe keyframe = new Keyframe ();

				if (string.IsNullOrEmpty (child.Attribute ("key")) == false) {
					float tmpTime = 0;
					if (float.TryParse (child.Attribute ("key"), out tmpTime )) {
						keyframe.time = tmpTime;
					}
				}
				if (string.IsNullOrEmpty (child.Attribute ("value")) == false) {
					float tmpValue = 0;
					if (float.TryParse (child.Attribute ("value"), out tmpValue)) {
						keyframe.value = tmpValue;
					}
				}
				tmpAnimationCurve.AddKey (keyframe);
				tmpAnimationCurve.SmoothTangents (tmpAnimationCurve.keys.Length - 1, 0);
			}
		}

		return tmpAnimationCurve;
	}

    public static void SetMeshRendererColor(Transform transform, Color c)
    {
        if(transform == null)
        {
            return;
        }
        MeshRenderer mr = transform.GetComponent<MeshRenderer>();
        if(mr && mr.material.HasProperty("_Color"))
        {
            mr.material.SetColor("_Color", c);
        }

        for(int i = 0; i <transform.childCount; ++i)
        {
            SetMeshRendererColor(transform.GetChild(i), c);
        }
    }
}


