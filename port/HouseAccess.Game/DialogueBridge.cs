using System;
using System.Collections.Generic;
using System.Text;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using HouseAccess.World;
using EekUI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class DialogueBridge
{
	private static bool _pending;

	private static bool _listReady;

	private static float _pendingSince;

	private static int _index = -1;

	private static readonly List<Button> Buttons = new List<Button>();

	private static readonly List<string> Labels = new List<string>();

	private static string _pendingLine;

	private static float _pendingAt;

	private static bool _repliesHeld;

	private static float _repliesHeldSince;

	private static float _lastActiveAt;

	private static int _lastFocusId;

	private static bool _listAnnounced;

	public static bool Active { get; private set; }

	public static string LastLine { get; private set; }

	public static string LastSpeaker { get; private set; }

	public static bool RepliesHeld => _repliesHeld;

	public static bool SpeakerTalking => false;

	public static bool HasChoices => _listReady && Buttons.Count > 0;

	public static void Reset()
	{
		_pending = false;
		_listReady = false;
		_index = -1;
		Active = false;
		_pendingLine = null;
		_repliesHeld = false;
		_listAnnounced = false;
		_lastFocusId = 0;
		Buttons.Clear();
		Labels.Clear();
	}

	public static void NotifyStarted(string speaker)
	{
		Hold.ReleaseQuiet();
		LastSpeaker = speaker;
		Active = true;
		_listReady = false;
		_repliesHeld = false;
		_listAnnounced = false;
		_lastFocusId = 0;
		Buttons.Clear();
		Labels.Clear();
		_index = -1;
		Log.Info("Dialogue started with " + (string.IsNullOrWhiteSpace(speaker) ? "unknown speaker" : speaker) + ".");
	}

	public static void NotifyLine(string cleanText)
	{
		Active = true;
		if (!string.IsNullOrWhiteSpace(cleanText))
		{
			LastLine = cleanText;
			string text = (Prefs.DialogueSpeech?.Value ?? "voiced").Trim().ToLowerInvariant();
			if (text == "never")
			{
				_pendingLine = null;
				return;
			}
			if (text == "always")
			{
				Speaker.Say(TextUtil.Cap(cleanText, 600), Pri.High);
				return;
			}
			_pendingLine = cleanText;
			_pendingAt = Time.unscaledTime + 1.2f;
		}
	}

	private static bool VoicePlaying()
	{
		// This game build does not expose the current dialogue speaker, so the mod cannot
		// tell whether the voiced line is still playing; lines are announced after a short
		// pause instead.
		return false;
	}

	private static void ResolvePending()
	{
		if (_pendingLine != null)
		{
			if (VoicePlaying())
			{
				_pendingLine = null;
			}
			else if (!(Time.unscaledTime < _pendingAt))
			{
				string pendingLine = _pendingLine;
				_pendingLine = null;
				Speaker.Say(TextUtil.Cap(pendingLine, 600), Pri.High);
			}
		}
	}

	public static void RepeatLast()
	{
		if (!string.IsNullOrWhiteSpace(LastLine))
		{
			string text = (string.IsNullOrWhiteSpace(LastSpeaker) ? string.Empty : (LastSpeaker + " said. "));
			Speaker.SayNow(text + TextUtil.Cap(LastLine, 700));
		}
		if (!GameRefs.DialogueActive)
		{
			return;
		}
		if (!_listReady || Labels.Count == 0)
		{
			_pending = true;
			_pendingSince = Time.unscaledTime;
			TryCollect(GameRefs.Dialogue);
			_repliesHeld = false;
		}
		if (Labels.Count > 0)
		{
			Announce();
		}
		else
		{
			Speaker.Say("No replies on screen.");
		}
	}

	public static void NotifyResponsesPending()
	{
		_pending = true;
		_listReady = false;
		_pendingSince = Time.unscaledTime;
	}

	public static void NotifyResponseChosen()
	{
		_listReady = false;
		_pending = false;
		_repliesHeld = false;
		_listAnnounced = false;
		_lastFocusId = 0;
		Buttons.Clear();
		Labels.Clear();
		_index = -1;
	}

	public static void NotifyEnded()
	{
		Log.Info("Dialogue ended.");
		Reset();
		Speaker.Say("Conversation ended.");
	}

	public static void Tick()
	{
		if (!GameRefs.DialogueActive && !_pending && !_listReady && !_repliesHeld)
		{
			// This build hides the dialogue UI's open state; the engine's static
			// reference is the only signal, and it is not trustworthy while replies
			// are on screen. Once replies are pending, held or collected, keep
			// treating the conversation as active so the list is still read out.
			if (Active && Time.unscaledTime - _lastActiveAt > 1.5f)
			{
				NotifyEnded();
			}
			return;
		}
		Active = true;
		_lastActiveAt = Time.unscaledTime;
		ResolvePending();
		ReleaseReplies();
		if (!_listReady)
		{
			TryCollect(GameRefs.Dialogue);
		}
		if (_listReady)
		{
			SyncGameFocus();
			HandleKeys();
		}
	}

	private static void TryCollect(DialogueUI ui)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)ui))
		{
			return;
		}
		// The response button container is not named on this game build (its members were
		// stripped to interop names); the response buttons list is the first List<Button>
		// the dialogue UI keeps.
		List<Button> list = Cpp.Read(() => Cpp.ToManaged(ui.FAIFDGOFNIA));
		if (list == null || list.Count == 0)
		{
			// No buttons exist at all. Only an empty container after replies were queued
			// is a lost batch worth warning about.
			if (_pending && Time.unscaledTime - _pendingSince > 4f)
			{
				_pending = false;
				Log.Warn("Dialogue responses never appeared; giving up on this batch.");
			}
			return;
		}
		Buttons.Clear();
		Labels.Clear();
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			Button b = list[num2];
			if (!Cpp.Alive((UnityEngine.Object)(object)b))
			{
				continue;
			}
			GameObject val = Cpp.Read(() => ((Component)b).gameObject);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null || !val.activeInHierarchy || !Cpp.Read(() => ((Selectable)b).interactable, fallback: true))
			{
				continue;
			}
			string text = LabelUnder(val);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			Buttons.Add(b);
			Labels.Add(TextUtil.Clean(text));
		}
		if (Buttons.Count == 0)
		{
			// The reply buttons exist but none are enabled or labelled yet - the game
			// fills them at the start of an exchange and keeps them disabled while the
			// voiced line plays. That is waiting, not failure: keep the batch alive.
			if (_pending)
			{
				_pendingSince = Time.unscaledTime;
			}
			return;
		}
		_pending = false;
		_listReady = true;
		_index = 0;
		_repliesHeld = true;
		_repliesHeldSince = Time.unscaledTime;
		_listAnnounced = false;
		// Point the game's real focus at the first reply, so its own arrow handling
		// (if any) starts from the same place the mod announces.
		SelectReply(0);
		Log.Info("Dialogue: " + Buttons.Count + " repl" + (Buttons.Count == 1 ? "y" : "ies") + " collected.");
	}

	private static void ReleaseReplies()
	{
		// The old build waited 20 seconds before reading the replies, which on this
		// game build (where the voiced line cannot be detected) meant twenty seconds
		// of silence after the line - indistinguishable from a broken mod. The list
		// is read once after a short pause instead. The list stays "held" until a
		// reply is chosen or the conversation ends, which keeps MenuReader quiet
		// about the dialogue screen so the game's focus changes and the mod's arrows
		// cannot announce the same reply twice.
		if (_repliesHeld && !_listAnnounced && Time.unscaledTime - _repliesHeldSince > 1.2f)
		{
			_listAnnounced = true;
			Announce();
		}
	}

	private static string LabelUnder(GameObject go)
	{
		try
		{
			Il2CppArrayBase<Text> componentsInChildren = go.GetComponentsInChildren<Text>(true);
			if (componentsInChildren != null)
			{
				foreach (Text t in componentsInChildren)
				{
					string text = Cpp.Read(() => t.text);
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text;
					}
				}
			}
			Il2CppArrayBase<TMP_Text> componentsInChildren2 = go.GetComponentsInChildren<TMP_Text>(true);
			if (componentsInChildren2 != null)
			{
				foreach (TMP_Text t2 in componentsInChildren2)
				{
					string text2 = Cpp.Read(() => t2.text);
					if (!string.IsNullOrWhiteSpace(text2))
					{
						return text2;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static void Announce()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(TextUtil.Pluralise(Buttons.Count, "reply", "replies"));
		stringBuilder.Append(". ");
		for (int i = 0; i < Labels.Count; i++)
		{
			stringBuilder.Append(i + 1);
			stringBuilder.Append(". ");
			stringBuilder.Append(Labels[i]);
			stringBuilder.Append(". ");
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 900), Pri.High);
	}

	private static void HandleKeys()
	{
		if (Keys.Hit(Prefs.KeyRepeatTarget))
		{
			Announce();
			return;
		}
		if (Keys.Hit(Prefs.KeyUiNext))
		{
			MoveReply(1);
			return;
		}
		if (Keys.Hit(Prefs.KeyUiPrev))
		{
			MoveReply(-1);
			return;
		}
		if (Keys.Hit(Prefs.KeyUiActivate))
		{
			Choose(_index);
			return;
		}
		// Numbers go through ListNumbers so replies count the same way as every other
		// list in the mod: keypad included, 0 for the tenth reply, and a spoken answer
		// when the number is past the end instead of a silently dropped key.
		if (!ListNumbers.Pressed(Buttons.Count, out var index, out var pick))
		{
			return;
		}
		if (!pick)
		{
			Speaker.SayNow((index < Labels.Count) ? $"{index + 1}. {Labels[index]}" : "reply");
			SelectReply(index);
			return;
		}
		Choose(index);
	}

	/// <summary>
	/// Moves the reply cursor one step (Down = +1, Up = -1), wrapping at the ends, and
	/// reads the reply now focused: "&lt;reply&gt;, N of M." The same arrows that browse
	/// every other list in the mod browse the replies too.
	/// </summary>
	private static void MoveReply(int dir)
	{
		if (Labels.Count == 0)
		{
			return;
		}
		_index = (_index + dir + Labels.Count) % Labels.Count;
		SpeakReply(_index);
		SelectReply(_index);
	}

	private static void SpeakReply(int i)
	{
		if (i >= 0 && i < Labels.Count)
		{
			Speaker.Say($"{Labels[i]}, {i + 1} of {Labels.Count}.", Pri.High);
		}
	}

	/// <summary>
	/// Points the game's own EventSystem selection at the reply, the same call the mod
	/// uses to move focus in its menus, so the game's highlight (and any arrow handling
	/// of its own) follows the reply the mod is announcing.
	/// </summary>
	private static void SelectReply(int i)
	{
		try
		{
			if (i < 0 || i >= Buttons.Count)
			{
				return;
			}
			Button b = Buttons[i];
			if (Cpp.Alive((UnityEngine.Object)(object)b))
			{
				((Selectable)b).Select();
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// Follows the game's real focus. If the game selects a reply button on its own
	/// (its arrow handling, a click, or the mouse), the mod's cursor moves to that
	/// reply and reads it, so the two can never drift apart. The reverse direction -
	/// the mod moving the game's focus - happens in <see cref="MoveReply" /> and at
	/// collection. A game-led move is logged so the two can be told apart.
	/// </summary>
	private static void SyncGameFocus()
	{
		try
		{
			EventSystem es = EventSystem.current;
			if (!Cpp.Alive((UnityEngine.Object)(object)es))
			{
				return;
			}
			GameObject go = Cpp.Read(() => es.currentSelectedGameObject);
			if ((UnityEngine.Object)(object)go == (UnityEngine.Object)null)
			{
				return;
			}
			int num = go.GetInstanceID();
			for (int i = 0; i < Buttons.Count; i++)
			{
				Button b = Buttons[i];
				if (!Cpp.Alive((UnityEngine.Object)(object)b))
				{
					continue;
				}
				GameObject val = Cpp.Read(() => ((Component)b).gameObject);
				if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null || val.GetInstanceID() != num)
				{
					continue;
				}
				if (num != _lastFocusId)
				{
					_lastFocusId = num;
					if (i != _index)
					{
						_index = i;
						Log.Info("Dialogue: game focus moved to reply " + (i + 1) + ".");
						SpeakReply(i);
					}
				}
				return;
			}
			_lastFocusId = 0;
		}
		catch
		{
		}
	}

	/// <summary>
	/// Picks the reply at <paramref name="i" />: says it, presses its button and clears
	/// the list. Shared by Enter (KeyUiActivate) and Control+number so the two paths
	/// cannot drift apart.
	/// </summary>
	private static void Choose(int i)
	{
		if (i < 0 || i >= Buttons.Count)
		{
			Speaker.SayNow("No replies on screen.");
			return;
		}
		Button val = Buttons[i];
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			Speaker.SayNow("That reply is no longer there.");
			return;
		}
		Speaker.SayNow((i < Labels.Count) ? Labels[i] : "reply");
		try
		{
			((UnityEvent)val.onClick).Invoke();
		}
		catch (Exception ex)
		{
			Log.Warn("Reply invoke failed: " + ex.Message);
		}
		NotifyResponseChosen();
	}
}
