using System;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine.Canvas;
using HouseParty;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.UI;

using Speaker = HouseAccess.Speech.Speaker;

namespace HouseAccess.Game;

public static class LoadingBridge
{
	private static LoadingScreenManager _mgr;

	private static bool _lastContinueVisible;

	private static int _lastProgressStep = -1;

	private static float _nextPoll;

	private static string _lastHint;

	private static string _lastTip;

	public static void Reset()
	{
		_mgr = null;
		_lastContinueVisible = false;
		_lastProgressStep = -1;
		_lastHint = null;
		_lastTip = null;
	}

	public static void OnHintChanged(HintLoader loader)
	{
		if ((UnityEngine.Object)(object)loader == (UnityEngine.Object)null || !Prefs.SpeakLoading.Value)
		{
			return;
		}
		Text label = Cpp.Read(() => loader.MJFGGLCHPDF);
		if (Cpp.Alive((UnityEngine.Object)(object)label))
		{
			string s = Cpp.Read(() => label.text);
			string text = TextUtil.Clean(s);
			if (!string.IsNullOrWhiteSpace(text) && !(text == _lastHint))
			{
				_lastHint = text;
				Speaker.Say("Hint. " + TextUtil.Cap(text, 400));
			}
		}
	}

	public static void OnToolTipChanged(ToolTipAssociate associate)
	{
		if (!Prefs.SpeakUi.Value)
		{
			return;
		}
		if ((UnityEngine.Object)(object)associate == (UnityEngine.Object)null)
		{
			_lastTip = null;
			return;
		}
		string s = Cpp.Read(() => associate._ToolTipText);
		string text = TextUtil.Clean(s);
		if (!string.IsNullOrWhiteSpace(text) && !(text == _lastTip))
		{
			_lastTip = text;
			Speaker.Say(TextUtil.Cap(text, 300));
		}
	}

	public static void Tick()
	{
		if (Time.unscaledTime < _nextPoll)
		{
			return;
		}
		_nextPoll = Time.unscaledTime + 0.25f;
		if (!Cpp.Alive((UnityEngine.Object)(object)_mgr))
		{
			_mgr = Cpp.FindOne<LoadingScreenManager>(activeOnly: false);
		}
		if (!Cpp.Alive((UnityEngine.Object)(object)_mgr))
		{
			if (_lastProgressStep >= 0 || _lastContinueVisible)
			{
				Reset();
			}
		}
		else
		{
			if (!((Component)_mgr).gameObject.activeInHierarchy)
			{
				if (_lastProgressStep >= 0 || _lastContinueVisible || _lastHint != null || _lastTip != null)
				{
					Reset();
				}
			}
			else
			{
				WatchContinuePrompt();
				WatchProgress();
			}
		}
	}

	private static void WatchContinuePrompt()
	{
		// This build exposes no _ClickToContinueObject on LoadingScreenManager; find an
		// active child of the loading screen that is labelled as the continue prompt.
		GameObject go = null;
		try
		{
			Il2CppArrayBase<Transform> children = ((Component)_mgr).GetComponentsInChildren<Transform>(true);
			if (children != null)
			{
				foreach (Transform t in children)
				{
					if (!Cpp.Alive((UnityEngine.Object)(object)t))
					{
						continue;
					}
					GameObject g = ((Component)t).gameObject;
					if (!Cpp.Alive((UnityEngine.Object)(object)g) || !g.activeInHierarchy)
					{
						continue;
					}
					string n = ((UnityEngine.Object)g).name;
					if (n != null && n.IndexOf("Continue", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						go = g;
						break;
					}
				}
			}
		}
		catch
		{
		}
		bool flag = Cpp.Alive((UnityEngine.Object)(object)go);
		if (flag != _lastContinueVisible)
		{
			_lastContinueVisible = flag;
			if (flag)
			{
				Speaker.SayNow("Loading finished. Press a key to continue.");
			}
		}
	}

	private static void WatchProgress()
	{
		if (!Prefs.SpeakLoading.Value)
		{
			return;
		}
		AsyncOperation val = Cpp.Read(() => _mgr.MBIIDOACBAL);
		if (val == null)
		{
			return;
		}
		float progress;
		try
		{
			progress = val.progress;
		}
		catch
		{
			return;
		}
		int num = Mathf.Clamp(Mathf.FloorToInt(progress / 0.9f * 4f), 0, 4);
		if (num != _lastProgressStep)
		{
			if (_lastProgressStep < 0 && num == 0)
			{
				_lastProgressStep = 0;
				return;
			}
			_lastProgressStep = num;
			Speaker.Say($"Loading {num * 25} percent.", Pri.Low);
		}
	}
}
