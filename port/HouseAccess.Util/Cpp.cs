using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HouseAccess.Util;

public static class Cpp
{
	public static bool Alive(UnityEngine.Object o)
	{
		try
		{
			return o != (UnityEngine.Object)null;
		}
		catch
		{
			return false;
		}
	}

	public static List<T> FindAll<T>(bool activeOnly = false) where T : Component
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		List<T> list = new List<T>();
		try
		{
			Il2CppReferenceArray<UnityEngine.Object> val = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
			if (val == null)
			{
				return list;
			}
			foreach (UnityEngine.Object item in (Il2CppArrayBase<UnityEngine.Object>)(object)val)
			{
				if (item == (UnityEngine.Object)null)
				{
					continue;
				}
				T val2 = ((Il2CppObjectBase)item).TryCast<T>();
				if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
				{
					continue;
				}
				GameObject gameObject;
				try
				{
					gameObject = ((Component)val2).gameObject;
				}
				catch
				{
					continue;
				}
				if (!((UnityEngine.Object)(object)gameObject == (UnityEngine.Object)null))
				{
					Scene scene = gameObject.scene;
					if (scene.IsValid() && (!activeOnly || gameObject.activeInHierarchy))
					{
						list.Add(val2);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("FindAll<" + typeof(T).Name + "> failed: " + ex.Message);
		}
		return list;
	}

	public static List<T> FindAllObjects<T>() where T : UnityEngine.Object
	{
		List<T> list = new List<T>();
		try
		{
			Il2CppReferenceArray<UnityEngine.Object> val = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
			if (val == null)
			{
				return list;
			}
			foreach (UnityEngine.Object item in (Il2CppArrayBase<UnityEngine.Object>)(object)val)
			{
				if (!(item == (UnityEngine.Object)null))
				{
					T val2 = ((Il2CppObjectBase)item).TryCast<T>();
					if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
					{
						list.Add(val2);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("FindAllObjects<" + typeof(T).Name + "> failed: " + ex.Message);
		}
		return list;
	}

	public static T FindOne<T>(bool activeOnly = false) where T : Component
	{
		List<T> list = FindAll<T>(activeOnly);
		return (list.Count > 0) ? list[0] : default(T);
	}

	public static int Count<T>(List<T> list)
	{
		try
		{
			return list?.Count ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	public static T At<T>(List<T> list, int index)
	{
		try
		{
			if (list == null || index < 0 || index >= list.Count)
			{
				return default(T);
			}
			return list[index];
		}
		catch
		{
			return default(T);
		}
	}

	public static List<T> ToManaged<T>(List<T> list, int cap = 256)
	{
		List<T> list2 = new List<T>();
		int num = Math.Min(Count<T>(list), cap);
		for (int i = 0; i < num; i++)
		{
			T val = At<T>(list, i);
			if (val != null)
			{
				list2.Add(val);
			}
		}
		return list2;
	}

	public static List<string> ToManagedStrings(List<string> list, int cap = 64)
	{
		List<string> list2 = new List<string>();
		int num = Math.Min(Count<string>(list), cap);
		for (int i = 0; i < num; i++)
		{
			string text = At<string>(list, i);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list2.Add(text);
			}
		}
		return list2;
	}

	public static T AddComponent<T>(GameObject go) where T : Component
	{
		try
		{
			Component val = go.AddComponent(Il2CppType.Of<T>());
			return ((UnityEngine.Object)(object)val == (UnityEngine.Object)null) ? default(T) : ((Il2CppObjectBase)val).TryCast<T>();
		}
		catch (Exception ex)
		{
			Log.Warn("AddComponent<" + typeof(T).Name + "> failed: " + ex.Message);
			return default(T);
		}
	}

	public static string ClassName(Il2CppObjectBase obj)
	{
		if (obj == null)
		{
			return null;
		}
		try
		{
			IntPtr pointer = obj.Pointer;
			if (pointer == IntPtr.Zero)
			{
				return null;
			}
			IntPtr intPtr = IL2CPP.il2cpp_object_get_class(pointer);
			if (intPtr == IntPtr.Zero)
			{
				return null;
			}
			IntPtr intPtr2 = IL2CPP.il2cpp_class_get_name(intPtr);
			return (intPtr2 == IntPtr.Zero) ? null : Marshal.PtrToStringAnsi(intPtr2);
		}
		catch
		{
			return null;
		}
	}

	public static TVal Read<TVal>(Func<TVal> getter, TVal fallback = default(TVal))
	{
		try
		{
			return getter();
		}
		catch
		{
			return fallback;
		}
	}

	// --- Helpers for reading Il2CppSystem collections produced by the game. ---

	public static int CountOf<T>(Il2CppSystem.Collections.Generic.List<T> list)
	{
		try
		{
			return list == null ? 0 : list.Count;
		}
		catch
		{
			return 0;
		}
	}

	public static T AtOf<T>(Il2CppSystem.Collections.Generic.List<T> list, int index)
	{
		try
		{
			if (list == null || index < 0 || index >= list.Count)
			{
				return default(T);
			}
			return list[index];
		}
		catch
		{
			return default(T);
		}
	}

	public static List<T> ToManaged<T>(Il2CppSystem.Collections.Generic.List<T> list, int cap = 512)
	{
		List<T> list2 = new List<T>();
		int num = Math.Min(CountOf<T>(list), cap);
		for (int i = 0; i < num; i++)
		{
			T val = AtOf<T>(list, i);
			if (val != null)
			{
				list2.Add(val);
			}
		}
		return list2;
	}

	public static List<string> ToManagedStrings(Il2CppSystem.Collections.Generic.List<string> list, int cap = 64)
	{
		List<string> list2 = new List<string>();
		int num = Math.Min(CountOf<string>(list), cap);
		for (int i = 0; i < num; i++)
		{
			string text = AtOf<string>(list, i);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list2.Add(text);
			}
		}
		return list2;
	}
}
