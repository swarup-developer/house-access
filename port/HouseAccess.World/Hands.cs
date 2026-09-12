using System;
using System.Text;
using HouseAccess.Game;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine.Interaction;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace HouseAccess.World;

public static class Hands
{
	private static readonly string[] Options = new string[4] { "Put it down", "Throw gently", "Throw firmly", "Throw hard" };

	private static readonly float[] Strength = new float[4] { 0f, 0.25f, 0.6f, 1f };

	private static int _index = -1;

	private static float _expiresAt;

	private static DragRigidbody Drag
	{
		get
		{
			try
			{
				return Cpp.FindOne<DragRigidbody>(activeOnly: false);
			}
			catch
			{
				return null;
			}
		}
	}

	public static bool Dragging
	{
		get
		{
			DragRigidbody d = Drag;
			if (!Cpp.Alive((UnityEngine.Object)(object)d))
			{
				return false;
			}
			try
			{
				SpringJoint joint = Cpp.Read(() => d.m_SpringJoint);
				return Cpp.Alive(joint) && Cpp.Alive(Cpp.Read(() => joint.connectedBody));
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool Active => _index >= 0 && Time.unscaledTime <= _expiresAt;

	private static IKInteraction Hand => Cpp.FindOne<IKInteraction>(activeOnly: false);

	public static InteractiveItem Held
	{
		get
		{
			IKInteraction h = Hand;
			if (!Cpp.Alive((UnityEngine.Object)(object)h))
			{
				return null;
			}
			return Cpp.Read(() => h.CurrentlyHolding);
		}
	}

	public static void Reach(float delta)
	{
		DragRigidbody d = Drag;
		if (!Cpp.Alive((UnityEngine.Object)(object)d))
		{
			Speaker.SayNow("Cannot move things out here.");
			return;
		}
		if (!Dragging)
		{
			InteractiveItem held = Held;
			Speaker.SayNow(((UnityEngine.Object)(object)held != (UnityEngine.Object)null) ? ((GameRefs.NameOf(held) ?? "That") + " is in your hand, not held out. Pick something up with the pick up key to move it about.") : "Nothing held out to move.");
			return;
		}
		try
		{
			float num = Cpp.Read(() => d.Distance, 2f);
			float num2 = Mathf.Clamp(num + delta, 0.6f, 5f);
			d.Distance = num2;
			Speaker.Say(TextUtil.Distance(num2) + " out.");
		}
		catch (Exception ex)
		{
			Log.Warn("Could not change the reach: " + ex.Message);
		}
	}

	public static void Reset()
	{
		_index = -1;
	}

	public static void RaiseOrLower()
	{
		InteractiveItem held = Held;
		if ((UnityEngine.Object)(object)held == (UnityEngine.Object)null)
		{
			if (PhoneBridge.IsUp)
			{
				InteractivePhone val = Cpp.FindOne<InteractivePhone>(activeOnly: false);
				if (Cpp.Alive((UnityEngine.Object)(object)val))
				{
					try
					{
						val.LowerPhone();
						Speaker.SayNow("Phone down.");
						return;
					}
					catch
					{
					}
				}
			}
			Speaker.SayNow("Nothing in your hand. Take something first.");
			return;
		}
		string text = GameRefs.NameOf(held) ?? "it";
		InteractivePhone val2 = ((Il2CppObjectBase)held).TryCast<InteractivePhone>();
		if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
		{
			try
			{
				val2 = ((Component)held).GetComponentInParent<InteractivePhone>();
			}
			catch
			{
			}
		}
		if (Cpp.Alive((UnityEngine.Object)(object)val2))
		{
			bool isUp = PhoneBridge.IsUp;
			try
			{
				if (isUp)
				{
					val2.LowerPhone();
					Speaker.SayNow(text + " down.");
				}
				else
				{
					val2.RaisePhone();
					Speaker.SayNow(text + " up.");
				}
				return;
			}
			catch (Exception ex)
			{
				Log.Warn("Could not move the phone: " + ex.Message);
			}
		}
		IKInteraction hand = Hand;
		try
		{
			if (Cpp.Alive((UnityEngine.Object)(object)hand))
			{
				hand.DropHeldItem();
				Speaker.SayNow("Put " + text + " away.");
			}
			else
			{
				Speaker.SayNow("Cannot put it away right now.");
			}
		}
		catch (Exception ex2)
		{
			Log.Warn("Could not put it away: " + ex2.Message);
		}
	}

	public static void Use()
	{
		if (!GameRefs.InPlayScene)
		{
			Speaker.SayNow("Only in the house.");
			return;
		}
		InteractiveItem held = Held;
		if ((UnityEngine.Object)(object)held != (UnityEngine.Object)null)
		{
			OpenOptions(held);
			return;
		}
		Entry current = Radar.Current;
		if (current == null || (current.Kind != EntryKind.Item && current.Kind != EntryKind.Prop))
		{
			string text = Activity.Holding();
			Speaker.SayNow(string.IsNullOrWhiteSpace(text) ? "Your hands are empty. Select something to pick up." : ("Holding up " + text + ". Nothing selected to pick up."));
		}
		else
		{
			Grab();
		}
	}

	private static void Grab()
	{
		Entry current = Radar.Current;
		if (current == null || (current.Kind != EntryKind.Item && current.Kind != EntryKind.Prop))
		{
			Speaker.SayNow("Select an item or a movable object first.");
			return;
		}
		if (!current.InReach)
		{
			Speaker.SayNow(current.Label + " is " + TextUtil.Distance(current.Distance) + " away. Too far to reach.");
			return;
		}
		DistractableRigidItem val = null;
		if (current.Kind == EntryKind.Prop)
		{
			val = current.Prop;
		}
		else if (Cpp.Alive((UnityEngine.Object)(object)current.Item))
		{
			if (!GameRefs.CanGrab(current.Item))
			{
				Speaker.SayNow(current.Label + " cannot be picked up.");
				return;
			}
			val = ((Il2CppObjectBase)current.Item).TryCast<DistractableRigidItem>();
		}
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			Speaker.SayNow(current.Label + " cannot be lifted. Try interact instead.");
			return;
		}
		IKInteraction hand = Hand;
		if (!Cpp.Alive((UnityEngine.Object)(object)hand))
		{
			Speaker.SayNow("Cannot pick things up right now.");
			return;
		}
		Speaker.SayNow("Picking up " + current.Label + ".");
		// This build no longer takes DistractableRigidItem directly; the carry routine
		// is GrabFromInventory(InteractiveItem), so route through the item's InteractiveItem
		// when it has one and say so clearly when it does not.
		InteractiveItem proxy = ((Il2CppObjectBase)val).TryCast<InteractiveItem>();
		if ((UnityEngine.Object)(object)proxy == (UnityEngine.Object)null)
		{
			try
			{
				proxy = ((Component)val).GetComponentInParent<InteractiveItem>();
			}
			catch
			{
			}
		}
		if ((UnityEngine.Object)(object)proxy == (UnityEngine.Object)null)
		{
			Speaker.Say(current.Label + " cannot be carried on this game version, but you can still interact with it.", Pri.High);
			return;
		}
		try
		{
			hand.GrabFromInventory(proxy);
		}
		catch (Exception ex)
		{
			Log.Warn("Grab failed: " + ex.Message);
			Speaker.Say("Could not pick up " + current.Label + ".", Pri.High);
		}
	}

	private static void OpenOptions(InteractiveItem held)
	{
		string value = GameRefs.NameOf(held) ?? "it";
		_index = 0;
		_expiresAt = Time.unscaledTime + 20f;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Holding ");
		stringBuilder.Append(value);
		stringBuilder.Append(". ");
		for (int i = 0; i < Options.Length; i++)
		{
			stringBuilder.Append(i + 1);
			stringBuilder.Append(". ");
			stringBuilder.Append(Options[i]);
			stringBuilder.Append(". ");
		}
		string value2 = Prefs.KeyCancel?.Value;
		if (!string.IsNullOrWhiteSpace(value2))
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
			handler.AppendFormatted(value2);
			handler.AppendLiteral(" to close. ");
			stringBuilder2.Append(ref handler);
		}
		Speaker.Say(stringBuilder.ToString(), Pri.High);
		SpeakCurrent();
	}

	/// <summary>
	/// Reads the option under the cursor. <paramref name="force" /> speaks at Critical so a
	/// repeated number key is heard again rather than suppressed as a duplicate.
	/// </summary>
	private static void SpeakCurrent(bool force = false)
	{
		if (_index >= 0 && _index < Options.Length)
		{
			Speaker.Say($"{Options[_index]}, {_index + 1} of {Options.Length}.", force ? Pri.Critical : Pri.High);
		}
	}

	private static void Choose(int i)
	{
		if (i < 0 || i >= Options.Length)
		{
			return;
		}
		IKInteraction hand = Hand;
		InteractiveItem held = Held;
		_index = -1;
		if (!Cpp.Alive((UnityEngine.Object)(object)hand) || (UnityEngine.Object)(object)held == (UnityEngine.Object)null)
		{
			Speaker.SayNow("Not holding anything.");
			return;
		}
		string text = GameRefs.NameOf(held) ?? "it";
		if (i == 0)
		{
			Speaker.SayNow("Put down " + text + ".");
			try
			{
				hand.DropHeldItem();
				return;
			}
			catch (Exception ex)
			{
				Log.Warn("Drop failed: " + ex.Message);
				return;
			}
		}
		float num = Cpp.Read(() => IKInteraction.MinRHActionStrength, 0f);
		float num2 = Cpp.Read(() => IKInteraction.MaxRHActionStrength, 1f);
		if (num2 <= num)
		{
			num = 0f;
			num2 = 1f;
		}
		float num3 = Mathf.Lerp(num, num2, Strength[i]);
		Speaker.SayNow(Options[i] + ". " + text + ".");
		try
		{
			// This build has no Release(float): drop the item, then push it gently along
			// your view direction so the gentle/firm/hard options still mean something.
			hand.DropHeldItem();
			Camera cam = Camera.main;
			Rigidbody rb = ((Component)held).GetComponentInChildren<Rigidbody>(true);
			if (Cpp.Alive((UnityEngine.Object)(object)rb) && (UnityEngine.Object)(object)cam != (UnityEngine.Object)null)
			{
				float speed = Mathf.Lerp(1.2f, 3.2f, num3);
				rb.AddForce(((Component)cam).transform.forward * (speed * rb.mass), ForceMode.Impulse);
			}
		}
		catch (Exception ex2)
		{
			Log.Warn("Throw failed: " + ex2.Message);
			Speaker.SayNow("That did not work.");
		}
	}

	public static void Tick()
	{
		if (_index >= 0 && Time.unscaledTime > _expiresAt)
		{
			_index = -1;
		}
		else
		{
			if (!Active)
			{
				return;
			}
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
			if (Keys.Hit(Prefs.KeyCancel))
			{
				_index = -1;
				Speaker.SayNow("Cancelled.");
				return;
			}
			// This list used to act on a bare number while every other list only read the
			// item, so the same key meant two different things depending on which menu was
			// open - and here it threw whatever you were holding. A number now reads,
			// control and a number acts, and reading refreshes the timeout the same way
			// arrowing through the options does.
			if (ListNumbers.Pressed(Options.Length, out var index, out var pick))
			{
				if (pick)
				{
					Choose(index);
					return;
				}
				_index = index;
				_expiresAt = Time.unscaledTime + 20f;
				SpeakCurrent(force: true);
			}
		}
	}

	private static void Move(int dir)
	{
		_index = (_index + dir + Options.Length) % Options.Length;
		_expiresAt = Time.unscaledTime + 20f;
		SpeakCurrent();
	}
}
