using System;
using HouseAccess.Speech;
using HouseAccess.Util;
using Il2CppInterop.Runtime.InteropTypes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HouseAccess.UI;

public static class TextFields
{
	private static int _lastSeen;

	private static float _nextCheck;

	private static bool _announced;

	private static Component _cached;

	private static int _cachedFrame = -1;

	public static Component Current
	{
		get
		{
			int frameCount = Time.frameCount;
			if (frameCount == _cachedFrame && ((UnityEngine.Object)(object)_cached == (UnityEngine.Object)null || Cpp.Alive((UnityEngine.Object)(object)_cached)))
			{
				return _cached;
			}
			_cachedFrame = frameCount;
			_cached = FindField();
			return _cached;
		}
	}

	public static bool HasField => (UnityEngine.Object)(object)Current != (UnityEngine.Object)null;

	public static bool Typing
	{
		get
		{
			Component current = Current;
			if ((UnityEngine.Object)(object)current == (UnityEngine.Object)null)
			{
				return false;
			}
			try
			{
				InputField val = ((Il2CppObjectBase)current).TryCast<InputField>();
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					return val.isFocused;
				}
				TMP_InputField val2 = ((Il2CppObjectBase)current).TryCast<TMP_InputField>();
				if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
				{
					return val2.isFocused;
				}
			}
			catch
			{
			}
			return false;
		}
	}

	public static void Reset()
	{
		_lastSeen = 0;
		_announced = false;
	}

	private static Component FindField()
	{
		try
		{
			foreach (InputField item in Cpp.FindAll<InputField>(activeOnly: true))
			{
				if ((UnityEngine.Object)(object)item != (UnityEngine.Object)null && ((UIBehaviour)item).IsActive() && ((Selectable)item).IsInteractable())
				{
					return (Component)(object)item;
				}
			}
			foreach (TMP_InputField item2 in Cpp.FindAll<TMP_InputField>(activeOnly: true))
			{
				if ((UnityEngine.Object)(object)item2 != (UnityEngine.Object)null && ((UIBehaviour)item2).IsActive() && ((Selectable)item2).IsInteractable())
				{
					return (Component)(object)item2;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	public static void Focus()
	{
		Component current = Current;
		if ((UnityEngine.Object)(object)current == (UnityEngine.Object)null)
		{
			Speaker.SayNow("No text box here.");
			return;
		}
		try
		{
			InputField plain = ((Il2CppObjectBase)current).TryCast<InputField>();
			if ((UnityEngine.Object)(object)plain != (UnityEngine.Object)null)
			{
				((Selectable)plain).Select();
				plain.ActivateInputField();
				string existing = Cpp.Read(() => plain.text);
				Announce(existing);
				return;
			}
			TMP_InputField tmp = ((Il2CppObjectBase)current).TryCast<TMP_InputField>();
			if ((UnityEngine.Object)(object)tmp != (UnityEngine.Object)null)
			{
				((Selectable)tmp).Select();
				tmp.ActivateInputField();
				string existing2 = Cpp.Read(() => tmp.text);
				Announce(existing2);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Could not focus the text box: " + ex.Message);
			Speaker.SayNow("Could not use the text box.");
		}
	}

	private static void Announce(string existing)
	{
		Speaker.SayNow(string.IsNullOrWhiteSpace(existing) ? "Text box ready. Type a name, then press enter." : ("Text box ready, containing " + existing + ". Type to replace it, then press enter."));
	}

	public static string Contents()
	{
		Component current = Current;
		if ((UnityEngine.Object)(object)current == (UnityEngine.Object)null)
		{
			return null;
		}
		try
		{
			InputField plain = ((Il2CppObjectBase)current).TryCast<InputField>();
			if ((UnityEngine.Object)(object)plain != (UnityEngine.Object)null)
			{
				return Cpp.Read(() => plain.text);
			}
			TMP_InputField tmp = ((Il2CppObjectBase)current).TryCast<TMP_InputField>();
			if ((UnityEngine.Object)(object)tmp != (UnityEngine.Object)null)
			{
				return Cpp.Read(() => tmp.text);
			}
		}
		catch
		{
		}
		return null;
	}

	public static void Tick()
	{
		if (Time.unscaledTime < _nextCheck)
		{
			return;
		}
		_nextCheck = Time.unscaledTime + 0.4f;
		Component current = Current;
		if ((UnityEngine.Object)(object)current == (UnityEngine.Object)null)
		{
			_lastSeen = 0;
			_announced = false;
			return;
		}
		int instanceID;
		try
		{
			instanceID = ((UnityEngine.Object)current.gameObject).GetInstanceID();
		}
		catch
		{
			return;
		}
		if (instanceID != _lastSeen)
		{
			_lastSeen = instanceID;
			_announced = true;
			string text = Prefs.KeyTypeText?.Value;
			Speaker.Say(string.IsNullOrWhiteSpace(text) ? "There is a text box on this screen." : ("There is a text box. Press " + text + " to type in it."), Pri.High);
		}
	}
}
