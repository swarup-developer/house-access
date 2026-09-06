using System;
using System.Collections.Generic;
using System.Text;
using HouseAccess.Game;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Interaction;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace HouseAccess.World;

public static class ActionPicker
{
	private static InteractiveItem _item;

	private static string _label;

	private static readonly List<GameRefs.Verb> Verbs = new List<GameRefs.Verb>();

	private static int _index = -1;

	private static float _expiresAt;

	private static float _watchUntil;

	private static float _chosenAt;

	private static string _chosenVerb;

	private static List<string> _bagBefore;

	private static string _stateBefore;

	private static string _confirmVerb;

	private static float _confirmUntil;

	public static bool Active => Verbs.Count > 0 && Cpp.Alive((UnityEngine.Object)(object)_item);

	public static void Reset()
	{
		_item = null;
		_label = null;
		Verbs.Clear();
		_index = -1;
	}

	public static void Open(InteractiveItem item, string label)
	{
		if (Prefs.UseGameWheel.Value && GameRefs.OpenWheelFor(item))
		{
			return;
		}
		if (!Cpp.Alive((UnityEngine.Object)(object)item))
		{
			Speaker.SayNow("Nothing to interact with there.");
			return;
		}
		List<GameRefs.Verb> list = GameRefs.VerbsOf(item);
		list.RemoveAll((GameRefs.Verb v) => string.Equals(v.Name, "Interact", StringComparison.OrdinalIgnoreCase));
		if (list.Count == 0)
		{
			Speaker.SayNow("Interacting with " + label + ".");
			try
			{
				item.Interact();
				return;
			}
			catch (Exception ex)
			{
				Log.Warn("Interact failed: " + ex.Message);
				Speaker.SayNow("That did not work.");
				return;
			}
		}
		_item = item;
		_label = label;
		Verbs.Clear();
		Verbs.AddRange(list);
		_index = 0;
		_confirmVerb = null;
		_expiresAt = Time.unscaledTime + 30f;
		if (Cpp.Alive((UnityEngine.Object)(object)item))
		{
			try
			{
				Door componentInParent = ((Component)item).GetComponentInParent<Door>();
				if (Cpp.Alive((UnityEngine.Object)(object)componentInParent))
				{
					string text = GameRefs.SideOf(componentInParent);
					if (!string.IsNullOrWhiteSpace(text))
					{
						Speaker.Say("Locking works from where you stand: " + text + ".");
					}
				}
			}
			catch
			{
			}
		}
		Announce();
	}

	public static void Cancel(bool announce)
	{
		_confirmVerb = null;
		if (!Active)
		{
			Reset();
			return;
		}
		Reset();
		if (announce)
		{
			Speaker.SayNow("Cancelled.");
		}
	}

	private static void WatchForResponse()
	{
		if (_watchUntil <= 0f)
		{
			return;
		}
		if (Speaker.LastSpokeAt > _chosenAt + 0.15f)
		{
			ClearWatch();
			return;
		}
		if (_bagBefore != null)
		{
			string text = InventoryBridge.DiffAgainst(_bagBefore);
			if (!string.IsNullOrWhiteSpace(text))
			{
				Speaker.Say(text, Pri.High);
				Log.Info("[action] " + _chosenVerb + " changed the inventory: " + text);
				ClearWatch();
				return;
			}
		}
		PlayerCharacter player = GameRefs.Player;
		string text2 = Activity.Describe((player != null) ? ((Il2CppObjectBase)player).TryCast<Character>() : null);
		if (!string.Equals(text2, _stateBefore, StringComparison.OrdinalIgnoreCase))
		{
			Speaker.Say(string.IsNullOrWhiteSpace(text2) ? (_chosenVerb + ". Stopped.") : ("You are now " + text2 + "."));
			Log.Info($"[action] {_chosenVerb}: state was '{_stateBefore}', now '{text2}'.");
			ClearWatch();
		}
		else if (!(Time.unscaledTime < _watchUntil))
		{
			string chosenVerb = _chosenVerb;
			ClearWatch();
			Speaker.Say(chosenVerb + " did not seem to do anything.");
			Log.Info("[action] " + chosenVerb + " produced no response of any kind.");
		}
	}

	private static void ClearWatch()
	{
		_watchUntil = 0f;
		_chosenVerb = null;
		_bagBefore = null;
		_stateBefore = null;
	}

	private static void Announce()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(_label);
		stringBuilder.Append(". ");
		stringBuilder.Append(TextUtil.Pluralise(Verbs.Count, "option", "options"));
		stringBuilder.Append(". ");
		for (int i = 0; i < Verbs.Count; i++)
		{
			stringBuilder.Append(i + 1);
			stringBuilder.Append(". ");
			stringBuilder.Append(Describe(Verbs[i]));
			stringBuilder.Append(". ");
		}
		string value = Prefs.KeyCancel?.Value;
		if (!string.IsNullOrWhiteSpace(value))
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendFormatted(value);
			handler.AppendLiteral(" to close. ");
			stringBuilder2.Append(ref handler);
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 800), Pri.High);
		SpeakCurrent();
	}

	/// <summary>
	/// Reads the verb under the cursor. <paramref name="force" /> speaks at Critical, which
	/// skips the duplicate-suppression window: pressing the same number twice is a deliberate
	/// request to hear that verb again, and swallowing it would sound like a dead key.
	/// </summary>
	private static void SpeakCurrent(bool force = false)
	{
		if (_index >= 0 && _index < Verbs.Count)
		{
			Speaker.Say($"{Describe(Verbs[_index])}, {_index + 1} of {Verbs.Count}.", force ? Pri.Critical : Pri.High);
		}
	}

	private static string Describe(GameRefs.Verb v)
	{
		string text = TextUtil.Humanize(v.Name);
		return v.Available ? text : (text + ", unavailable");
	}

	private static void Move(int dir)
	{
		if (Verbs.Count != 0)
		{
			_index = (_index + dir + Verbs.Count) % Verbs.Count;
			SpeakCurrent();
		}
	}

	private static void Choose(int i)
	{
		if (i < 0 || i >= Verbs.Count)
		{
			return;
		}
		if (!Cpp.Alive((UnityEngine.Object)(object)_item))
		{
			Cancel(announce: false);
			return;
		}
		GameRefs.Verb verb = Verbs[i];
		if (!verb.Available)
		{
			Speaker.SayNow(TextUtil.Humanize(verb.Name) + " is not available right now.");
			return;
		}
		string name = verb.Name;
		InteractiveItem item = _item;
		if (!string.Equals(_confirmVerb, name, StringComparison.OrdinalIgnoreCase) || Time.unscaledTime > _confirmUntil)
		{
			string text = Watchers.WhoWouldSee(item, name);
			if (!string.IsNullOrWhiteSpace(text))
			{
				_confirmVerb = name;
				_confirmUntil = Time.unscaledTime + 6f;
				Speaker.SayNow(text + " can see this. Press again to " + TextUtil.Humanize(name).ToLowerInvariant() + " anyway.");
				return;
			}
		}
		_confirmVerb = null;
		Reset();
		Speaker.SayNow(TextUtil.Humanize(name) + ".");
		Log.Info("[action] " + TextUtil.Humanize(name) + " on " + ((UnityEngine.Object)item).name);
		_chosenVerb = TextUtil.Humanize(name);
		_bagBefore = InventoryBridge.Snapshot();
		PlayerCharacter player = GameRefs.Player;
		_stateBefore = Activity.Describe((player != null) ? ((Il2CppObjectBase)player).TryCast<Character>() : null);
		// This game build has no InteractiveItem.BeforeInteraction gate; OnChooseInteraction
		// below still reports failure through WatchForResponse.
		try
		{
			item.OnChooseInteraction(name);
			_chosenAt = Time.unscaledTime;
			_watchUntil = _chosenAt + 4f;
		}
		catch (Exception ex)
		{
			Log.Warn("OnChooseInteraction failed: " + ex.Message);
			Speaker.SayNow("That did not work.");
		}
	}

	public static void Tick()
	{
		WatchForResponse();
		if (!Active)
		{
			if (Verbs.Count > 0)
			{
				Reset();
			}
			return;
		}
		if (Time.unscaledTime > _expiresAt)
		{
			Reset();
			return;
		}
		if (DialogueBridge.HasChoices)
		{
			Reset();
			return;
		}
		_expiresAt = Time.unscaledTime + 30f;
		if (Keys.Hit(Prefs.KeyUiNext))
		{
			Move(1);
			return;
		}
		if (Keys.Hit(Prefs.KeyUiPrev))
		{
			Move(-1);
			return;
		}
		if (Keys.Hit(Prefs.KeyUiActivate))
		{
			Choose(_index);
			return;
		}
		if (Keys.Hit(Prefs.KeyRepeatTarget))
		{
			SpeakCurrent();
			return;
		}
		if (Keys.Hit(Prefs.KeyCancel) || Keys.Hit(Prefs.KeyInteract))
		{
			Cancel(announce: true);
			return;
		}
		// One shared rule for number keys (see ListNumbers): a number reads the verb,
		// control and a number runs it. The refresh of _expiresAt above already covers
		// this branch, so reading down a long list does not let the picker time out.
		if (ListNumbers.Pressed(Verbs.Count, out var index, out var pick))
		{
			if (pick)
			{
				Choose(index);
				return;
			}
			_index = index;
			SpeakCurrent(force: true);
		}
	}
}
