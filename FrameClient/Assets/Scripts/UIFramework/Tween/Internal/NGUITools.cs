//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2016 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// Helper class containing generic functions used throughout the UI library.
/// </summary>

static public class NGUITools
{
	static AudioListener mListener;

	static bool mLoaded = false;
	static float mGlobalVolume = 1f;

	/// <summary>
	/// Globally accessible volume affecting all sounds played via NGUITools.PlaySound().
	/// </summary>

	static public float soundVolume
	{
		get
		{
			if (!mLoaded)
			{
				mLoaded = true;
				mGlobalVolume = PlayerPrefs.GetFloat("Sound", 1f);
			}
			return mGlobalVolume;
		}
		set
		{
			if (mGlobalVolume != value)
			{
				mLoaded = true;
				mGlobalVolume = value;
				PlayerPrefs.SetFloat("Sound", value);
			}
		}
	}

	/// <summary>
	/// Helper function -- whether the disk access is allowed.
	/// </summary>

	static public bool fileAccess
	{
		get
		{
#if !UNITY_4_7
			if (Application.platform == RuntimePlatform.WebGLPlayer) return false;
#endif
#if UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3
			return Application.platform != RuntimePlatform.WindowsWebPlayer &&
				Application.platform != RuntimePlatform.OSXWebPlayer;
#else
			return true;
#endif
		}
	}

	/// <summary>
	/// Play the specified audio clip.
	/// </summary>

	static public AudioSource PlaySound (AudioClip clip) { return PlaySound(clip, 1f, 1f); }

	/// <summary>
	/// Play the specified audio clip with the specified volume.
	/// </summary>

	static public AudioSource PlaySound (AudioClip clip, float volume) { return PlaySound(clip, volume, 1f); }

	static float mLastTimestamp = 0f;
	static AudioClip mLastClip;

	/// <summary>
	/// Play the specified audio clip with the specified volume and pitch.
	/// </summary>

	static public AudioSource PlaySound (AudioClip clip, float volume, float pitch)
	{
		float time = RealTime.time;
		if (mLastClip == clip && mLastTimestamp + 0.1f > time) return null;

		mLastClip = clip;
		mLastTimestamp = time;
		volume *= soundVolume;

		if (clip != null && volume > 0.01f)
		{
			if (mListener == null || !NGUITools.GetActive(mListener))
			{
				AudioListener[] listeners = GameObject.FindObjectsOfType(typeof(AudioListener)) as AudioListener[];

				if (listeners != null)
				{
					for (int i = 0; i < listeners.Length; ++i)
					{
						if (NGUITools.GetActive(listeners[i]))
						{
							mListener = listeners[i];
							break;
						}
					}
				}

				if (mListener == null)
				{
					Camera cam = Camera.main;
					if (cam == null) cam = GameObject.FindObjectOfType(typeof(Camera)) as Camera;
					if (cam != null) mListener = cam.gameObject.AddComponent<AudioListener>();
				}
			}

			if (mListener != null && mListener.enabled && NGUITools.GetActive(mListener.gameObject))
			{
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
				AudioSource source = mListener.audio;
#else
				AudioSource source = mListener.GetComponent<AudioSource>();
#endif
				if (source == null) source = mListener.gameObject.AddComponent<AudioSource>();
#if !UNITY_FLASH
				source.priority = 50;
				source.pitch = pitch;
#endif
				source.PlayOneShot(clip, volume);
				return source;
			}
		}
		return null;
	}

	/// <summary>
	/// New WWW call can fail if the crossdomain policy doesn't check out. Exceptions suck. It's much more elegant to check for null instead.
	/// </summary>

//    static public WWW OpenURL (string url)
//    {
//#if UNITY_FLASH
//        Debug.LogError("WWW is not yet implemented in Flash");
//        return null;
//#else
//        WWW www = null;
//        try { www = new WWW(url); }
//        catch (System.Exception ex) { Debug.LogError(ex.Message); }
//        return www;
//#endif
//    }

//    /// <summary>
//    /// New WWW call can fail if the crossdomain policy doesn't check out. Exceptions suck. It's much more elegant to check for null instead.
//    /// </summary>

//    static public WWW OpenURL (string url, WWWForm form)
//    {
//        if (form == null) return OpenURL(url);
//#if UNITY_FLASH
//        Debug.LogError("WWW is not yet implemented in Flash");
//        return null;
//#else
//        WWW www = null;
//        try { www = new WWW(url, form); }
//        catch (System.Exception ex) { Debug.LogError(ex != null ? ex.Message : "<null>"); }
//        return www;
//#endif
//    }

	/// <summary>
	/// Same as Random.Range, but the returned value is between min and max, inclusive.
	/// Unity's Random.Range is less than max instead, unless min == max.
	/// This means Range(0,1) produces 0 instead of 0 or 1. That's unacceptable.
	/// </summary>

	static public int RandomRange (int min, int max)
	{
		if (min == max) return min;
		return UnityEngine.Random.Range(min, max + 1);
	}

	/// <summary>
	/// Returns the hierarchy of the object in a human-readable format.
	/// </summary>

	static public string GetHierarchy (GameObject obj)
	{
		if (obj == null) return "";
		string path = obj.name;

		while (obj.transform.parent != null)
		{
			obj = obj.transform.parent.gameObject;
			path = obj.name + "\\" + path;
		}
		return path;
	}

	/// <summary>
	/// Find all active objects of specified type.
	/// </summary>

	static public T[] FindActive<T> () where T : Component
	{
		return GameObject.FindObjectsOfType(typeof(T)) as T[];
	}




	/// <summary>
	/// Helper function that returns the string name of the type.
	/// </summary>

	static public string GetTypeName<T> ()
	{
		string s = typeof(T).ToString();
		if (s.StartsWith("UI")) s = s.Substring(2);
		else if (s.StartsWith("UnityEngine.")) s = s.Substring(12);
		return s;
	}

	/// <summary>
	/// Helper function that returns the string name of the type.
	/// </summary>

	static public string GetTypeName (UnityEngine.Object obj)
	{
		if (obj == null) return "Null";
		string s = obj.GetType().ToString();
		if (s.StartsWith("UI")) s = s.Substring(2);
		else if (s.StartsWith("UnityEngine.")) s = s.Substring(12);
		return s;
	}

	/// <summary>
	/// Convenience method that works without warnings in both Unity 3 and 4.
	/// </summary>

	static public void RegisterUndo (UnityEngine.Object obj, string name)
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(obj, name);
		NGUITools.SetDirty(obj);
#endif
	}

	/// <summary>
	/// Convenience function that marks the specified object as dirty in the Unity Editor.
	/// </summary>

	static public void SetDirty (UnityEngine.Object obj)
	{
#if UNITY_EDITOR
		if (obj)
		{
			//if (obj is Component) Debug.Log(NGUITools.GetHierarchy((obj as Component).gameObject), obj);
			//else if (obj is GameObject) Debug.Log(NGUITools.GetHierarchy(obj as GameObject), obj);
			//else Debug.Log("Hmm... " + obj.GetType(), obj);
			UnityEditor.EditorUtility.SetDirty(obj);
		}
#endif
	}

	/// <summary>
	/// Add a new child game object.
	/// </summary>

	static public GameObject AddChild (this GameObject parent) { return AddChild(parent, true); }

	/// <summary>
	/// Add a new child game object.
	/// </summary>

	static public GameObject AddChild (this GameObject parent, bool undo)
	{
		GameObject go = new GameObject();
#if UNITY_EDITOR
		if (undo && !Application.isPlaying)
			UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Object");
#endif
		if (parent != null)
		{
			Transform t = go.transform;
			t.parent = parent.transform;
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;
			go.layer = parent.layer;
		}
		return go;
	}

	/// <summary>
	/// Instantiate an object and add it to the specified parent.
	/// </summary>

	static public GameObject AddChild (this GameObject parent, GameObject prefab)
	{
		GameObject go = GameObject.Instantiate(prefab) as GameObject;
#if UNITY_EDITOR
		if (!Application.isPlaying)
			UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Object");
#endif
		if (go != null && parent != null)
		{
			Transform t = go.transform;
			t.parent = parent.transform;
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;
			go.layer = parent.layer;
		}
		return go;
	}

	/// <summary>
	/// Calculate the game object's depth based on the widgets within, and also taking panel depth into consideration.
	/// </summary>




	
	
	/// <summary>
	/// Helper function that recursively sets all children with widgets' game objects layers to the specified value.
	/// </summary>

	static public void SetChildLayer (this Transform t, int layer)
	{
		for (int i = 0; i < t.childCount; ++i)
		{
			Transform child = t.GetChild(i);
			child.gameObject.layer = layer;
			SetChildLayer(child, layer);
		}
	}

	static Dictionary<System.Type, string> mTypeNames = new Dictionary<Type, string>();

	/// <summary>
	/// Add a child object to the specified parent and attaches the specified script to it.
	/// </summary>

	static public T AddChild<T> (this GameObject parent) where T : Component
	{
		GameObject go = AddChild(parent);
		string name;

		if (!mTypeNames.TryGetValue(typeof(T), out name) || name == null)
		{
			name = GetTypeName<T>();
			mTypeNames[typeof(T)] = name;
		}
		go.name = name;
		return go.AddComponent<T>();
	}

	/// <summary>
	/// Add a child object to the specified parent and attaches the specified script to it.
	/// </summary>

	static public T AddChild<T> (this GameObject parent, bool undo) where T : Component
	{
		GameObject go = AddChild(parent, undo);
		string name;

		if (!mTypeNames.TryGetValue(typeof(T), out name) || name == null)
		{
			name = GetTypeName<T>();
			mTypeNames[typeof(T)] = name;
		}
		go.name = name;
		return go.AddComponent<T>();
	}

	/// <summary>
	/// Add a new widget of specified type.
	/// </summary>

	

	/// <summary>
	/// Add a sprite appropriate for the specified atlas sprite.
	/// It will be sliced if the sprite has an inner rect, and a regular sprite otherwise.
	/// </summary>

	

	/// <summary>
	/// Get the rootmost object of the specified game object.
	/// </summary>

	static public GameObject GetRoot (GameObject go)
	{
		Transform t = go.transform;

		for (; ; )
		{
			Transform parent = t.parent;
			if (parent == null) break;
			t = parent;
		}
		return t.gameObject;
	}

	/// <summary>
	/// Finds the specified component on the game object or one of its parents.
	/// </summary>

	static public T FindInParents<T> (GameObject go) where T : Component
	{
		if (go == null) return null;
		// Commented out because apparently it causes Unity 4.5.3 to lag horribly:
		// http://www.tasharen.com/forum/index.php?topic=10882.0
//#if UNITY_4_3
 #if UNITY_FLASH
		object comp = go.GetComponent<T>();
 #else
		T comp = go.GetComponent<T>();
 #endif
		if (comp == null)
		{
			Transform t = go.transform.parent;

			while (t != null && comp == null)
			{
				comp = t.gameObject.GetComponent<T>();
				t = t.parent;
			}
		}
 #if UNITY_FLASH
		return (T)comp;
 #else
		return comp;
 #endif
//#else
//		return go.GetComponentInParent<T>();
//#endif
	}

	/// <summary>
	/// Finds the specified component on the game object or one of its parents.
	/// </summary>

	static public T FindInParents<T> (Transform trans) where T : Component
	{
		if (trans == null) return null;
#if UNITY_4_3
 #if UNITY_FLASH
		object comp = trans.GetComponent<T>();
 #else
		T comp = trans.GetComponent<T>();
 #endif
		if (comp == null)
		{
			Transform t = trans.transform.parent;

			while (t != null && comp == null)
			{
				comp = t.gameObject.GetComponent<T>();
				t = t.parent;
			}
		}
 #if UNITY_FLASH
		return (T)comp;
 #else
		return comp;
 #endif
#else
		return trans.GetComponentInParent<T>();
#endif
	}

	/// <summary>
	/// Destroy the specified object, immediately if in edit mode.
	/// </summary>

	static public void Destroy (UnityEngine.Object obj)
	{
		if (obj)
		{
			if (obj is Transform)
			{
				Transform t = (obj as Transform);
				GameObject go = t.gameObject;

				if (Application.isPlaying)
				{
					t.parent = null;
					UnityEngine.Object.Destroy(go);
				}
				else UnityEngine.Object.DestroyImmediate(go);
			}
			else if (obj is GameObject)
			{
				GameObject go = obj as GameObject;
				Transform t = go.transform;

				if (Application.isPlaying)
				{
					t.parent = null;
					UnityEngine.Object.Destroy(go);
				}
				else UnityEngine.Object.DestroyImmediate(go);
			}
			else if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
			else UnityEngine.Object.DestroyImmediate(obj);
		}
	}

	/// <summary>
	/// Convenience extension that destroys all children of the transform.
	/// </summary>

	static public void DestroyChildren (this Transform t)
	{
		bool isPlaying = Application.isPlaying;

		while (t.childCount != 0)
		{
			Transform child = t.GetChild(0);

			if (isPlaying)
			{
				child.parent = null;
				UnityEngine.Object.Destroy(child.gameObject);
			}
			else UnityEngine.Object.DestroyImmediate(child.gameObject);
		}
	}

	/// <summary>
	/// Destroy the specified object immediately, unless not in the editor, in which case the regular Destroy is used instead.
	/// </summary>

	static public void DestroyImmediate (UnityEngine.Object obj)
	{
		if (obj != null)
		{
			if (Application.isEditor) UnityEngine.Object.DestroyImmediate(obj);
			else UnityEngine.Object.Destroy(obj);
		}
	}

	/// <summary>
	/// Call the specified function on all objects in the scene.
	/// </summary>

	static public void Broadcast (string funcName)
	{
		GameObject[] gos = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		for (int i = 0, imax = gos.Length; i < imax; ++i) gos[i].SendMessage(funcName, SendMessageOptions.DontRequireReceiver);
	}

	/// <summary>
	/// Call the specified function on all objects in the scene.
	/// </summary>

	static public void Broadcast (string funcName, object param)
	{
		GameObject[] gos = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		for (int i = 0, imax = gos.Length; i < imax; ++i) gos[i].SendMessage(funcName, param, SendMessageOptions.DontRequireReceiver);
	}

	/// <summary>
	/// Determines whether the 'parent' contains a 'child' in its hierarchy.
	/// </summary>

	static public bool IsChild (Transform parent, Transform child)
	{
		if (parent == null || child == null) return false;

		while (child != null)
		{
			if (child == parent) return true;
			child = child.parent;
		}
		return false;
	}

	/// <summary>
	/// Activate the specified object and all of its children.
	/// </summary>

	static void Activate (Transform t) { Activate(t, false); }

	/// <summary>
	/// Activate the specified object and all of its children.
	/// </summary>

	static void Activate (Transform t, bool compatibilityMode)
	{
		SetActiveSelf(t.gameObject, true);

		if (compatibilityMode)
		{
			// If there is even a single enabled child, then we're using a Unity 4.0-based nested active state scheme.
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				if (child.gameObject.activeSelf) return;
			}

			// If this point is reached, then all the children are disabled, so we must be using a Unity 3.5-based active state scheme.
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				Activate(child, true);
			}
		}
	}

	/// <summary>
	/// Deactivate the specified object and all of its children.
	/// </summary>

	static void Deactivate (Transform t) { SetActiveSelf(t.gameObject, false); }

	/// <summary>
	/// SetActiveRecursively enables children before parents. This is a problem when a widget gets re-enabled
	/// and it tries to find a panel on its parent.
	/// </summary>

	static public void SetActive (GameObject go, bool state) { SetActive(go, state, true); }

	/// <summary>
	/// SetActiveRecursively enables children before parents. This is a problem when a widget gets re-enabled
	/// and it tries to find a panel on its parent.
	/// </summary>

	static public void SetActive (GameObject go, bool state, bool compatibilityMode)
	{
		if (go)
		{
			if (state)
			{
				Activate(go.transform, compatibilityMode);

			}
			else Deactivate(go.transform);
		}
	}

	/// <summary>
	/// Activate or deactivate children of the specified game object without changing the active state of the object itself.
	/// </summary>

	static public void SetActiveChildren (GameObject go, bool state)
	{
		Transform t = go.transform;

		if (state)
		{
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				Activate(child);
			}
		}
		else
		{
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				Deactivate(child);
			}
		}
	}

	/// <summary>
	/// Helper function that returns whether the specified MonoBehaviour is active.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public bool GetActive (Behaviour mb)
	{
		return mb && mb.enabled && mb.gameObject.activeInHierarchy;
	}

	/// <summary>
	/// Unity4 has changed GameObject.active to GameObject.activeself.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public bool GetActive (GameObject go)
	{
		return go && go.activeInHierarchy;
	}

	/// <summary>
	/// Unity4 has changed GameObject.active to GameObject.SetActive.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public void SetActiveSelf (GameObject go, bool state)
	{
		go.SetActive(state);
	}

	/// <summary>
	/// Recursively set the game object's layer.
	/// </summary>

	static public void SetLayer (GameObject go, int layer)
	{
		go.layer = layer;

		Transform t = go.transform;
		
		for (int i = 0, imax = t.childCount; i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			SetLayer(child.gameObject, layer);
		}
	}

	/// <summary>
	/// Helper function used to make the vector use integer numbers.
	/// </summary>

	static public Vector3 Round (Vector3 v)
	{
		v.x = Mathf.Round(v.x);
		v.y = Mathf.Round(v.y);
		v.z = Mathf.Round(v.z);
		return v;
	}






	
	/// <summary>
	/// Save the specified binary data into the specified file.
	/// </summary>

	static public bool Save (string fileName, byte[] bytes)
	{
#if UNITY_WEBPLAYER || UNITY_FLASH || UNITY_METRO || UNITY_WP8 || UNITY_WP_8_1
		return false;
#else
		if (!NGUITools.fileAccess) return false;

		string path = Application.persistentDataPath + "/" + fileName;

		if (bytes == null)
		{
			if (File.Exists(path)) File.Delete(path);
			return true;
		}

		FileStream file = null;

		try
		{
			file = File.Create(path);
		}
		catch (System.Exception ex)
		{
			Debug.LogError(ex.Message);
			return false;
		}

		file.Write(bytes, 0, bytes.Length);
		file.Close();
		return true;
#endif
	}

	/// <summary>
	/// Load all binary data from the specified file.
	/// </summary>

	static public byte[] Load (string fileName)
	{
#if UNITY_WEBPLAYER || UNITY_FLASH || UNITY_METRO || UNITY_WP8 || UNITY_WP_8_1
		return null;
#else
		if (!NGUITools.fileAccess) return null;

		string path = Application.persistentDataPath + "/" + fileName;

		if (File.Exists(path))
		{
			return File.ReadAllBytes(path);
		}
		return null;
#endif
	}

	/// <summary>
	/// Pre-multiply shaders result in a black outline if this operation is done in the shader. It's better to do it outside.
	/// </summary>

	static public Color ApplyPMA (Color c)
	{
		if (c.a != 1f)
		{
			c.r *= c.a;
			c.g *= c.a;
			c.b *= c.a;
		}
		return c;
	}

	/// <summary>
	/// Inform all widgets underneath the specified object that the parent has changed.
	/// </summary>

	static public void MarkParentAsChanged (GameObject go)
	{
		//UIRect[] rects = go.GetComponentsInChildren<UIRect>();
		//for (int i = 0, imax = rects.Length; i < imax; ++i)
	    //		rects[i].ParentHasChanged();
	}

	/// <summary>
	/// Access to the clipboard via undocumented APIs.
	/// </summary>

	static public string clipboard
	{
		get
		{
			TextEditor te = new TextEditor();
			te.Paste();
#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
			return te.content.text;
#else
			return te.text;
#endif
		}
		set
		{
			TextEditor te = new TextEditor();
#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
			te.content = new GUIContent(value);
#else
			te.text = value;
#endif
			te.OnFocus();
			te.Copy();
		}
	}

	

	/// <summary>
	/// Extension for the game object that checks to see if the component already exists before adding a new one.
	/// If the component is already present it will be returned instead.
	/// </summary>

	static public T AddMissingComponent<T> (this GameObject go) where T : Component
	{
#if UNITY_FLASH
		object comp = go.GetComponent<T>();
#else
		T comp = go.GetComponent<T>();
#endif
		if (comp == null)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				RegisterUndo(go, "Add " + typeof(T));
#endif
			comp = go.AddComponent<T>();
		}
#if UNITY_FLASH
		return (T)comp;
#else
		return comp;
#endif
	}

	// Temporary variable to avoid GC allocation
	static Vector3[] mSides = new Vector3[4];

	/// <summary>
	/// Get sides relative to the specified camera. The order is left, top, right, bottom.
	/// </summary>

	static public Vector3[] GetSides (this Camera cam)
	{
		return cam.GetSides(Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.5f), null);
	}

	/// <summary>
	/// Get sides relative to the specified camera. The order is left, top, right, bottom.
	/// </summary>

	static public Vector3[] GetSides (this Camera cam, float depth)
	{
		return cam.GetSides(depth, null);
	}

	/// <summary>
	/// Get sides relative to the specified camera. The order is left, top, right, bottom.
	/// </summary>

	static public Vector3[] GetSides (this Camera cam, Transform relativeTo)
	{
		return cam.GetSides(Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.5f), relativeTo);
	}

	/// <summary>
	/// Get sides relative to the specified camera. The order is left, top, right, bottom.
	/// </summary>

	static public Vector3[] GetSides (this Camera cam, float depth, Transform relativeTo)
	{
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
		if (cam.isOrthoGraphic)
#else
		if (cam.orthographic)
#endif
		{
			float os = cam.orthographicSize;
			float x0 = -os;
			float x1 = os;
			float y0 = -os;
			float y1 = os;

			Rect rect = cam.rect;
			Vector2 size = screenSize;

			float aspect = size.x / size.y;
			aspect *= rect.width / rect.height;
			x0 *= aspect;
			x1 *= aspect;

			// We want to ignore the scale, as scale doesn't affect the camera's view region in Unity
			Transform t = cam.transform;
			Quaternion rot = t.rotation;
			Vector3 pos = t.position;

			int w = Mathf.RoundToInt(size.x);
			int h = Mathf.RoundToInt(size.y);

			if ((w & 1) == 1) pos.x -= 1f / size.x;
			if ((h & 1) == 1) pos.y += 1f / size.y;

			mSides[0] = rot * (new Vector3(x0, 0f, depth)) + pos;
			mSides[1] = rot * (new Vector3(0f, y1, depth)) + pos;
			mSides[2] = rot * (new Vector3(x1, 0f, depth)) + pos;
			mSides[3] = rot * (new Vector3(0f, y0, depth)) + pos;
		}
		else
		{
			mSides[0] = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth));
			mSides[1] = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, depth));
			mSides[2] = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth));
			mSides[3] = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth));
		}
		
		if (relativeTo != null)
		{
			for (int i = 0; i < 4; ++i)
				mSides[i] = relativeTo.InverseTransformPoint(mSides[i]);
		}
		return mSides;
	}

	/// <summary>
	/// Get the camera's world-space corners. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	static public Vector3[] GetWorldCorners (this Camera cam)
	{
		float depth = Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.5f);
		return cam.GetWorldCorners(depth, null);
	}

	/// <summary>
	/// Get the camera's world-space corners. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	static public Vector3[] GetWorldCorners (this Camera cam, float depth)
	{
		return cam.GetWorldCorners(depth, null);
	}

	/// <summary>
	/// Get the camera's world-space corners. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	static public Vector3[] GetWorldCorners (this Camera cam, Transform relativeTo)
	{
		return cam.GetWorldCorners(Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.5f), relativeTo);
	}

	/// <summary>
	/// Get the camera's world-space corners. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	static public Vector3[] GetWorldCorners (this Camera cam, float depth, Transform relativeTo)
	{
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
		if (cam.isOrthoGraphic)
#else
		if (cam.orthographic)
#endif
		{
			float os = cam.orthographicSize;
			float x0 = -os;
			float x1 = os;
			float y0 = -os;
			float y1 = os;

			Rect rect = cam.rect;
			Vector2 size = screenSize;
			float aspect = size.x / size.y;
			aspect *= rect.width / rect.height;
			x0 *= aspect;
			x1 *= aspect;

			// We want to ignore the scale, as scale doesn't affect the camera's view region in Unity
			Transform t = cam.transform;
			Quaternion rot = t.rotation;
			Vector3 pos = t.position;

			mSides[0] = rot * (new Vector3(x0, y0, depth)) + pos;
			mSides[1] = rot * (new Vector3(x0, y1, depth)) + pos;
			mSides[2] = rot * (new Vector3(x1, y1, depth)) + pos;
			mSides[3] = rot * (new Vector3(x1, y0, depth)) + pos;
		}
		else
		{
			mSides[0] = cam.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
			mSides[1] = cam.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
			mSides[2] = cam.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
			mSides[3] = cam.ViewportToWorldPoint(new Vector3(1f, 0f, depth));
		}

		if (relativeTo != null)
		{
			for (int i = 0; i < 4; ++i)
				mSides[i] = relativeTo.InverseTransformPoint(mSides[i]);
		}
		return mSides;
	}

	/// <summary>
	/// Convenience function that converts Class + Function combo into Class.Function representation.
	/// </summary>

	static public string GetFuncName (object obj, string method)
	{
		if (obj == null) return "<null>";
		string type = obj.GetType().ToString();
		int period = type.LastIndexOf('/');
		if (period > 0) type = type.Substring(period + 1);
		return string.IsNullOrEmpty(method) ? type : type + "/" + method;
	}

#if UNITY_EDITOR || !UNITY_FLASH
	/// <summary>
	/// Execute the specified function on the target game object.
	/// </summary>

	static public void Execute<T> (GameObject go, string funcName) where T : Component
	{
		T[] comps = go.GetComponents<T>();

		foreach (T comp in comps)
		{
#if !UNITY_EDITOR && (UNITY_WEBPLAYER || UNITY_FLASH || UNITY_METRO || UNITY_WP8 || UNITY_WP_8_1)
			comp.SendMessage(funcName, SendMessageOptions.DontRequireReceiver);
#else
			MethodInfo method = comp.GetType().GetMethod(funcName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null) method.Invoke(comp, null);
#endif
		}
	}

	/// <summary>
	/// Execute the specified function on the target game object and all of its children.
	/// </summary>

	static public void ExecuteAll<T> (GameObject root, string funcName) where T : Component
	{
		Execute<T>(root, funcName);
		Transform t = root.transform;
		for (int i = 0, imax = t.childCount; i < imax; ++i)
			ExecuteAll<T>(t.GetChild(i).gameObject, funcName);
	}

	/// <summary>
	/// Immediately start, update, and create all the draw calls from newly instantiated UI.
	/// This is useful if you plan on doing something like immediately taking a screenshot then destroying the UI.
	/// </summary>

	static public void ImmediatelyCreateDrawCalls (GameObject root)
	{
		
	}
#endif

#if UNITY_EDITOR
	static int mSizeFrame = -1;
	static Func<Vector2> s_GetSizeOfMainGameView;
	static Vector2 mGameSize = Vector2.one;
	[System.NonSerialized] static bool mCheckedMainViewFunc = false;

	/// <summary>
	/// Size of the game view cannot be retrieved from Screen.width and Screen.height when the game view is hidden.
	/// </summary>

	static public Vector2 screenSize
	{
		get
		{
			int frame = Time.frameCount;

			if (mSizeFrame != frame || !Application.isPlaying)
			{
				//Profiler.BeginSample("Editor-only GC allocation (NGUITools.screenSize)");
				mSizeFrame = frame;

				if (s_GetSizeOfMainGameView == null && !mCheckedMainViewFunc)
				{
					mCheckedMainViewFunc = true;
					System.Type type = System.Type.GetType("UnityEditor.GameView,UnityEditor");

					// Post-Unity 5.4
					var methodInfo = type.GetMethod("GetMainGameViewTargetSize",
						System.Reflection.BindingFlags.Public |
						System.Reflection.BindingFlags.NonPublic |
						System.Reflection.BindingFlags.Static);

					// Pre-Unity 5.4
					if (methodInfo == null)
						methodInfo = type.GetMethod("GetSizeOfMainGameView",
							System.Reflection.BindingFlags.Public |
							System.Reflection.BindingFlags.NonPublic |
							System.Reflection.BindingFlags.Static);

					// Create the delegate
					if (methodInfo != null)
					{
						s_GetSizeOfMainGameView = (Func<Vector2>)Delegate.CreateDelegate(typeof(Func<Vector2>), methodInfo);
					}
					else Debug.LogWarning("Unable to get the main game view size function");
				}

				if (s_GetSizeOfMainGameView != null)
				{
					mGameSize = s_GetSizeOfMainGameView();
				}
				else mGameSize = new Vector2(Screen.width, Screen.height);
				//Profiler.EndSample();
			}
			return mGameSize;
		}
	}
#else
	/// <summary>
	/// Size of the game view cannot be retrieved from Screen.width and Screen.height when the game view is hidden.
	/// </summary>

	static public Vector2 screenSize { get { return new Vector2(Screen.width, Screen.height); } }
#endif

	/// <summary>
	/// List of keys that can be checked.
	/// </summary>

	static public KeyCode[] keys = new KeyCode[]
	{
		KeyCode.Backspace, // 8,
		KeyCode.Tab, // 9,
		KeyCode.Clear, // 12,
		KeyCode.Return, // 13,
		KeyCode.Pause, // 19,
		KeyCode.Escape, // 27,
		KeyCode.Space, // 32,
		KeyCode.Exclaim, // 33,
		KeyCode.DoubleQuote, // 34,
		KeyCode.Hash, // 35,
		KeyCode.Dollar, // 36,
		KeyCode.Ampersand, // 38,
		KeyCode.Quote, // 39,
		KeyCode.LeftParen, // 40,
		KeyCode.RightParen, // 41,
		KeyCode.Asterisk, // 42,
		KeyCode.Plus, // 43,
		KeyCode.Comma, // 44,
		KeyCode.Minus, // 45,
		KeyCode.Period, // 46,
		KeyCode.Slash, // 47,
		KeyCode.Alpha0, // 48,
		KeyCode.Alpha1, // 49,
		KeyCode.Alpha2, // 50,
		KeyCode.Alpha3, // 51,
		KeyCode.Alpha4, // 52,
		KeyCode.Alpha5, // 53,
		KeyCode.Alpha6, // 54,
		KeyCode.Alpha7, // 55,
		KeyCode.Alpha8, // 56,
		KeyCode.Alpha9, // 57,
		KeyCode.Colon, // 58,
		KeyCode.Semicolon, // 59,
		KeyCode.Less, // 60,
		KeyCode.Equals, // 61,
		KeyCode.Greater, // 62,
		KeyCode.Question, // 63,
		KeyCode.At, // 64,
		KeyCode.LeftBracket, // 91,
		KeyCode.Backslash, // 92,
		KeyCode.RightBracket, // 93,
		KeyCode.Caret, // 94,
		KeyCode.Underscore, // 95,
		KeyCode.BackQuote, // 96,
		KeyCode.A, // 97,
		KeyCode.B, // 98,
		KeyCode.C, // 99,
		KeyCode.D, // 100,
		KeyCode.E, // 101,
		KeyCode.F, // 102,
		KeyCode.G, // 103,
		KeyCode.H, // 104,
		KeyCode.I, // 105,
		KeyCode.J, // 106,
		KeyCode.K, // 107,
		KeyCode.L, // 108,
		KeyCode.M, // 109,
		KeyCode.N, // 110,
		KeyCode.O, // 111,
		KeyCode.P, // 112,
		KeyCode.Q, // 113,
		KeyCode.R, // 114,
		KeyCode.S, // 115,
		KeyCode.T, // 116,
		KeyCode.U, // 117,
		KeyCode.V, // 118,
		KeyCode.W, // 119,
		KeyCode.X, // 120,
		KeyCode.Y, // 121,
		KeyCode.Z, // 122,
		KeyCode.Delete, // 127,
		KeyCode.Keypad0, // 256,
		KeyCode.Keypad1, // 257,
		KeyCode.Keypad2, // 258,
		KeyCode.Keypad3, // 259,
		KeyCode.Keypad4, // 260,
		KeyCode.Keypad5, // 261,
		KeyCode.Keypad6, // 262,
		KeyCode.Keypad7, // 263,
		KeyCode.Keypad8, // 264,
		KeyCode.Keypad9, // 265,
		KeyCode.KeypadPeriod, // 266,
		KeyCode.KeypadDivide, // 267,
		KeyCode.KeypadMultiply, // 268,
		KeyCode.KeypadMinus, // 269,
		KeyCode.KeypadPlus, // 270,
		KeyCode.KeypadEnter, // 271,
		KeyCode.KeypadEquals, // 272,
		KeyCode.UpArrow, // 273,
		KeyCode.DownArrow, // 274,
		KeyCode.RightArrow, // 275,
		KeyCode.LeftArrow, // 276,
		KeyCode.Insert, // 277,
		KeyCode.Home, // 278,
		KeyCode.End, // 279,
		KeyCode.PageUp, // 280,
		KeyCode.PageDown, // 281,
		KeyCode.F1, // 282,
		KeyCode.F2, // 283,
		KeyCode.F3, // 284,
		KeyCode.F4, // 285,
		KeyCode.F5, // 286,
		KeyCode.F6, // 287,
		KeyCode.F7, // 288,
		KeyCode.F8, // 289,
		KeyCode.F9, // 290,
		KeyCode.F10, // 291,
		KeyCode.F11, // 292,
		KeyCode.F12, // 293,
		KeyCode.F13, // 294,
		KeyCode.F14, // 295,
		KeyCode.F15, // 296,
		KeyCode.Numlock, // 300,
		KeyCode.CapsLock, // 301,
		KeyCode.ScrollLock, // 302,
		KeyCode.RightShift, // 303,
		KeyCode.LeftShift, // 304,
		KeyCode.RightControl, // 305,
		KeyCode.LeftControl, // 306,
		KeyCode.RightAlt, // 307,
		KeyCode.LeftAlt, // 308,
		//KeyCode.Mouse0, // 323,
		//KeyCode.Mouse1, // 324,
		//KeyCode.Mouse2, // 325,
		KeyCode.Mouse3, // 326,
		KeyCode.Mouse4, // 327,
		KeyCode.Mouse5, // 328,
		KeyCode.Mouse6, // 329,
		KeyCode.JoystickButton0, // 330,
		KeyCode.JoystickButton1, // 331,
		KeyCode.JoystickButton2, // 332,
		KeyCode.JoystickButton3, // 333,
		KeyCode.JoystickButton4, // 334,
		KeyCode.JoystickButton5, // 335,
		KeyCode.JoystickButton6, // 336,
		KeyCode.JoystickButton7, // 337,
		KeyCode.JoystickButton8, // 338,
		KeyCode.JoystickButton9, // 339,
		KeyCode.JoystickButton10, // 340,
		KeyCode.JoystickButton11, // 341,
		KeyCode.JoystickButton12, // 342,
		KeyCode.JoystickButton13, // 343,
		KeyCode.JoystickButton14, // 344,
		KeyCode.JoystickButton15, // 345,
		KeyCode.JoystickButton16, // 346,
		KeyCode.JoystickButton17, // 347,
		KeyCode.JoystickButton18, // 348,
		KeyCode.JoystickButton19, // 349,
	};

	/// <summary>
	/// Helper function that converts the specified key to a 3-character key identifier for captions.
	/// </summary>

	static public string KeyToCaption (KeyCode key)
	{
		switch (key)
		{
			case KeyCode.None: return null;
			case KeyCode.Backspace: return "BS";
			case KeyCode.Tab: return "Tab";
			case KeyCode.Clear: return "Clr";
			case KeyCode.Return: return "NT";
			case KeyCode.Pause: return "PS";
			case KeyCode.Escape: return "Esc";
			case KeyCode.Space: return "SP";
			case KeyCode.Exclaim: return "!";
			case KeyCode.DoubleQuote: return "\"";
			case KeyCode.Hash: return "#";
			case KeyCode.Dollar: return "$";
			case KeyCode.Ampersand: return "&";
			case KeyCode.Quote: return "'";
			case KeyCode.LeftParen: return "(";
			case KeyCode.RightParen: return ")";
			case KeyCode.Asterisk: return "*";
			case KeyCode.Plus: return "+";
			case KeyCode.Comma: return ",";
			case KeyCode.Minus: return "-";
			case KeyCode.Period: return ".";
			case KeyCode.Slash: return "/";
			case KeyCode.Alpha0: return "0";
			case KeyCode.Alpha1: return "1";
			case KeyCode.Alpha2: return "2";
			case KeyCode.Alpha3: return "3";
			case KeyCode.Alpha4: return "4";
			case KeyCode.Alpha5: return "5";
			case KeyCode.Alpha6: return "6";
			case KeyCode.Alpha7: return "7";
			case KeyCode.Alpha8: return "8";
			case KeyCode.Alpha9: return "9";
			case KeyCode.Colon: return ":";
			case KeyCode.Semicolon: return ";";
			case KeyCode.Less: return "<";
			case KeyCode.Equals: return "=";
			case KeyCode.Greater: return ">";
			case KeyCode.Question: return "?";
			case KeyCode.At: return "@";
			case KeyCode.LeftBracket: return "[";
			case KeyCode.Backslash: return "\\";
			case KeyCode.RightBracket: return "]";
			case KeyCode.Caret: return "^";
			case KeyCode.Underscore: return "_";
			case KeyCode.BackQuote: return "`";
			case KeyCode.A: return "A";
			case KeyCode.B: return "B";
			case KeyCode.C: return "C";
			case KeyCode.D: return "D";
			case KeyCode.E: return "E";
			case KeyCode.F: return "F";
			case KeyCode.G: return "G";
			case KeyCode.H: return "H";
			case KeyCode.I: return "I";
			case KeyCode.J: return "J";
			case KeyCode.K: return "K";
			case KeyCode.L: return "L";
			case KeyCode.M: return "M";
			case KeyCode.N: return "N0";
			case KeyCode.O: return "O";
			case KeyCode.P: return "P";
			case KeyCode.Q: return "Q";
			case KeyCode.R: return "R";
			case KeyCode.S: return "S";
			case KeyCode.T: return "T";
			case KeyCode.U: return "U";
			case KeyCode.V: return "V";
			case KeyCode.W: return "W";
			case KeyCode.X: return "X";
			case KeyCode.Y: return "Y";
			case KeyCode.Z: return "Z";
			case KeyCode.Delete: return "Del";
			case KeyCode.Keypad0: return "K0";
			case KeyCode.Keypad1: return "K1";
			case KeyCode.Keypad2: return "K2";
			case KeyCode.Keypad3: return "K3";
			case KeyCode.Keypad4: return "K4";
			case KeyCode.Keypad5: return "K5";
			case KeyCode.Keypad6: return "K6";
			case KeyCode.Keypad7: return "K7";
			case KeyCode.Keypad8: return "K8";
			case KeyCode.Keypad9: return "K9";
			case KeyCode.KeypadPeriod: return ".";
			case KeyCode.KeypadDivide: return "/";
			case KeyCode.KeypadMultiply: return "*";
			case KeyCode.KeypadMinus: return "-";
			case KeyCode.KeypadPlus: return "+";
			case KeyCode.KeypadEnter: return "NT";
			case KeyCode.KeypadEquals: return "=";
			case KeyCode.UpArrow: return "UP";
			case KeyCode.DownArrow: return "DN";
			case KeyCode.RightArrow: return "LT";
			case KeyCode.LeftArrow: return "RT";
			case KeyCode.Insert: return "Ins";
			case KeyCode.Home: return "Home";
			case KeyCode.End: return "End";
			case KeyCode.PageUp: return "PU";
			case KeyCode.PageDown: return "PD";
			case KeyCode.F1: return "F1";
			case KeyCode.F2: return "F2";
			case KeyCode.F3: return "F3";
			case KeyCode.F4: return "F4";
			case KeyCode.F5: return "F5";
			case KeyCode.F6: return "F6";
			case KeyCode.F7: return "F7";
			case KeyCode.F8: return "F8";
			case KeyCode.F9: return "F9";
			case KeyCode.F10: return "F10";
			case KeyCode.F11: return "F11";
			case KeyCode.F12: return "F12";
			case KeyCode.F13: return "F13";
			case KeyCode.F14: return "F14";
			case KeyCode.F15: return "F15";
			case KeyCode.Numlock: return "Num";
			case KeyCode.CapsLock: return "Cap";
			case KeyCode.ScrollLock: return "Scr";
			case KeyCode.RightShift: return "RS";
			case KeyCode.LeftShift: return "LS";
			case KeyCode.RightControl: return "RC";
			case KeyCode.LeftControl: return "LC";
			case KeyCode.RightAlt: return "RA";
			case KeyCode.LeftAlt: return "LA";
			case KeyCode.Mouse0: return "M0";
			case KeyCode.Mouse1: return "M1";
			case KeyCode.Mouse2: return "M2";
			case KeyCode.Mouse3: return "M3";
			case KeyCode.Mouse4: return "M4";
			case KeyCode.Mouse5: return "M5";
			case KeyCode.Mouse6: return "M6";
			case KeyCode.JoystickButton0: return "(A)";
			case KeyCode.JoystickButton1: return "(B)";
			case KeyCode.JoystickButton2: return "(X)";
			case KeyCode.JoystickButton3: return "(Y)";
			case KeyCode.JoystickButton4: return "(RB)";
			case KeyCode.JoystickButton5: return "(LB)";
			case KeyCode.JoystickButton6: return "(Back)";
			case KeyCode.JoystickButton7: return "(Start)";
			case KeyCode.JoystickButton8: return "(LS)";
			case KeyCode.JoystickButton9: return "(RS)";
			case KeyCode.JoystickButton10: return "J10";
			case KeyCode.JoystickButton11: return "J11";
			case KeyCode.JoystickButton12: return "J12";
			case KeyCode.JoystickButton13: return "J13";
			case KeyCode.JoystickButton14: return "J14";
			case KeyCode.JoystickButton15: return "J15";
			case KeyCode.JoystickButton16: return "J16";
			case KeyCode.JoystickButton17: return "J17";
			case KeyCode.JoystickButton18: return "J18";
			case KeyCode.JoystickButton19: return "J19";
		}
		return null;
	}

	
	
	static GameObject mGo;

	


	/// <summary>
	/// Transforms this color from gamma to linear space, but only if the active color space is actually set to linear.
	/// </summary>

	static public Color GammaToLinearSpace (this Color c)
	{
		if (mColorSpace == ColorSpace.Uninitialized)
			mColorSpace = QualitySettings.activeColorSpace;

		if (mColorSpace == ColorSpace.Linear)
		{
			c.r = Mathf.GammaToLinearSpace(c.r);
			c.g = Mathf.GammaToLinearSpace(c.g);
			c.b = Mathf.GammaToLinearSpace(c.b);
			c.a = Mathf.GammaToLinearSpace(c.a);
		}
		return c;
	}

	/// <summary>
	/// Transforms this color from gamma to linear space, but only if the active color space is actually set to linear.
	/// </summary>

	static public Color GammaToLinearSpace (this Vector4 v)
	{
		if (mColorSpace == ColorSpace.Uninitialized)
			mColorSpace = QualitySettings.activeColorSpace;

		if (mColorSpace == ColorSpace.Linear)
		{
			v.x = Mathf.GammaToLinearSpace(v.x);
			v.y = Mathf.GammaToLinearSpace(v.y);
			v.z = Mathf.GammaToLinearSpace(v.z);
			v.w = Mathf.GammaToLinearSpace(v.w);
		}
		return v;
	}
	static ColorSpace mColorSpace = ColorSpace.Uninitialized;
}
