using System;
using System.Collections.Generic;
using HouseAccess.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HouseAccess.InputLayer;

public static class Keys
{
	private static bool _legacyBroken;

	private static bool _newChecked;

	private static bool _newAvailable;

	private static readonly Dictionary<KeyCode, Key> Map = BuildMap();

	private static bool _typingCached;

	private static int _typingFrame = -1;

	public static string ActiveBackend => (!_legacyBroken) ? "Legacy Input" : (_newAvailable ? "Input System" : "none");

	public static bool TypingInField
	{
		get
		{
			int frameCount = Time.frameCount;
			if (frameCount == _typingFrame)
			{
				return _typingCached;
			}
			_typingFrame = frameCount;
			_typingCached = LookForCaret();
			return _typingCached;
		}
	}

	public static bool Shift => Held((KeyCode)304) || Held((KeyCode)303);

	public static bool Ctrl => Held((KeyCode)306) || Held((KeyCode)305);

	public static bool Alt => Held((KeyCode)308) || Held((KeyCode)307);

	public static bool MovementKeyHeld => Held((KeyCode)119) || Held((KeyCode)97) || Held((KeyCode)115) || Held((KeyCode)100) || Held((KeyCode)273) || Held((KeyCode)274);

	private static Keyboard Kb
	{
		get
		{
			if (!_newChecked)
			{
				_newChecked = true;
				try
				{
					_newAvailable = Keyboard.current != null;
				}
				catch (Exception ex)
				{
					Log.Warn("Input System unavailable: " + ex.Message);
					_newAvailable = false;
				}
			}
			if (!_newAvailable)
			{
				return null;
			}
			try
			{
				return Keyboard.current;
			}
			catch
			{
				return null;
			}
		}
	}

	public static bool Down(KeyCode code)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if ((int)code == 0)
		{
			return false;
		}
		if (TypingInField)
		{
			return false;
		}
		if (!_legacyBroken)
		{
			try
			{
				return Input.GetKeyDown(code);
			}
			catch (Exception ex)
			{
				_legacyBroken = true;
				Log.Info("Legacy Input unavailable (" + ex.GetType().Name + "); switching to the Input System.");
			}
		}
		return NewDown(code);
	}

	public static bool Held(KeyCode code)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if ((int)code == 0)
		{
			return false;
		}
		if (!_legacyBroken)
		{
			try
			{
				return Input.GetKey(code);
			}
			catch
			{
				_legacyBroken = true;
			}
		}
		return NewHeld(code);
	}

	/// <summary>
	/// The list position a number key is asking for this frame: 1 to 9 from the number row
	/// or the keypad, and 0 for the tenth item, which is how the lists read themselves out.
	/// Returns 0 when no number key went down.
	///
	/// This is the only place the mod turns a key into a list position, so every list counts
	/// the same way. Alt is excluded because the mod's own Alt chords (reach, photos, camera)
	/// must keep working while a list is up; Ctrl and Shift are left to the caller, which is
	/// what makes "a number reads, control and a number picks" possible. <see cref="Down" />
	/// already refuses every key while a game text field holds the caret, so typing a digit
	/// into the game's own console or name box never reaches a list.
	/// </summary>
	public static int NumberDown()
	{
		if (Alt)
		{
			return 0;
		}
		for (int i = 1; i <= 9; i++)
		{
			// KeyCode.Alpha1 is 49 and KeyCode.Keypad1 is 257. Both rows are in the map
			// above, so this reads the same on the legacy backend and the Input System.
			if (Down((KeyCode)(48 + i)) || Down((KeyCode)(256 + i)))
			{
				return i;
			}
		}
		if (Down((KeyCode)48) || Down((KeyCode)256))
		{
			return 10;
		}
		return 0;
	}

	public static bool Hit(MelonPreferences_Entry<string> entry)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (TypingInField)
		{
			return false;
		}
		Prefs.Chord c = Prefs.Combo(entry);
		if (c.IsNone)
		{
			return false;
		}
		if (!ModifiersMatch(c))
		{
			return false;
		}
		return Down(c.Key);
	}

	public static bool Holding(MelonPreferences_Entry<string> entry)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (TypingInField)
		{
			return false;
		}
		Prefs.Chord c = Prefs.Combo(entry);
		if (c.IsNone)
		{
			return false;
		}
		if (!ModifiersMatch(c))
		{
			return false;
		}
		return Held(c.Key);
	}

	private static bool ModifiersMatch(Prefs.Chord c)
	{
		return Ctrl == c.Ctrl && Alt == c.Alt && Shift == c.Shift;
	}

	private static bool LookForCaret()
	{
		return TextFields.Typing;
	}

	private static bool NewDown(KeyCode code)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		Keyboard kb = Kb;
		if (kb == null)
		{
			return false;
		}
		if (!Map.TryGetValue(code, out var value))
		{
			return false;
		}
		try
		{
			KeyControl val = kb[value];
			return val != null && ((ButtonControl)val).wasPressedThisFrame;
		}
		catch
		{
			return false;
		}
	}

	private static bool NewHeld(KeyCode code)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		Keyboard kb = Kb;
		if (kb == null)
		{
			return false;
		}
		if (!Map.TryGetValue(code, out var value))
		{
			return false;
		}
		try
		{
			KeyControl val = kb[value];
			return val != null && ((ButtonControl)val).isPressed;
		}
		catch
		{
			return false;
		}
	}

	public static bool WarpMouse(Vector2 screenPos)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Mouse current = Mouse.current;
			if (current == null)
			{
				return false;
			}
			current.WarpCursorPosition(screenPos);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static Dictionary<KeyCode, Key> BuildMap()
	{
		return new Dictionary<KeyCode, Key>
		{
			{
				(KeyCode)97,
				(Key)15
			},
			{
				(KeyCode)98,
				(Key)16
			},
			{
				(KeyCode)99,
				(Key)17
			},
			{
				(KeyCode)100,
				(Key)18
			},
			{
				(KeyCode)101,
				(Key)19
			},
			{
				(KeyCode)102,
				(Key)20
			},
			{
				(KeyCode)103,
				(Key)21
			},
			{
				(KeyCode)104,
				(Key)22
			},
			{
				(KeyCode)105,
				(Key)23
			},
			{
				(KeyCode)106,
				(Key)24
			},
			{
				(KeyCode)107,
				(Key)25
			},
			{
				(KeyCode)108,
				(Key)26
			},
			{
				(KeyCode)109,
				(Key)27
			},
			{
				(KeyCode)110,
				(Key)28
			},
			{
				(KeyCode)111,
				(Key)29
			},
			{
				(KeyCode)112,
				(Key)30
			},
			{
				(KeyCode)113,
				(Key)31
			},
			{
				(KeyCode)114,
				(Key)32
			},
			{
				(KeyCode)115,
				(Key)33
			},
			{
				(KeyCode)116,
				(Key)34
			},
			{
				(KeyCode)117,
				(Key)35
			},
			{
				(KeyCode)118,
				(Key)36
			},
			{
				(KeyCode)119,
				(Key)37
			},
			{
				(KeyCode)120,
				(Key)38
			},
			{
				(KeyCode)121,
				(Key)39
			},
			{
				(KeyCode)122,
				(Key)40
			},
			{
				(KeyCode)48,
				(Key)50
			},
			{
				(KeyCode)49,
				(Key)41
			},
			{
				(KeyCode)50,
				(Key)42
			},
			{
				(KeyCode)51,
				(Key)43
			},
			{
				(KeyCode)52,
				(Key)44
			},
			{
				(KeyCode)53,
				(Key)45
			},
			{
				(KeyCode)54,
				(Key)46
			},
			{
				(KeyCode)55,
				(Key)47
			},
			{
				(KeyCode)56,
				(Key)48
			},
			{
				(KeyCode)57,
				(Key)49
			},
			{
				(KeyCode)282,
				(Key)94
			},
			{
				(KeyCode)283,
				(Key)95
			},
			{
				(KeyCode)284,
				(Key)96
			},
			{
				(KeyCode)285,
				(Key)97
			},
			{
				(KeyCode)286,
				(Key)98
			},
			{
				(KeyCode)287,
				(Key)99
			},
			{
				(KeyCode)288,
				(Key)100
			},
			{
				(KeyCode)289,
				(Key)101
			},
			{
				(KeyCode)290,
				(Key)102
			},
			{
				(KeyCode)291,
				(Key)103
			},
			{
				(KeyCode)292,
				(Key)104
			},
			{
				(KeyCode)293,
				(Key)105
			},
			{
				(KeyCode)273,
				(Key)63
			},
			{
				(KeyCode)274,
				(Key)64
			},
			{
				(KeyCode)276,
				(Key)61
			},
			{
				(KeyCode)275,
				(Key)62
			},
			{
				(KeyCode)13,
				(Key)2
			},
			{
				(KeyCode)271,
				(Key)77
			},
			{
				(KeyCode)27,
				(Key)60
			},
			{
				(KeyCode)32,
				(Key)1
			},
			{
				(KeyCode)9,
				(Key)3
			},
			{
				(KeyCode)8,
				(Key)65
			},
			{
				(KeyCode)127,
				(Key)71
			},
			{
				(KeyCode)277,
				(Key)70
			},
			{
				(KeyCode)278,
				(Key)68
			},
			{
				(KeyCode)279,
				(Key)69
			},
			{
				(KeyCode)280,
				(Key)67
			},
			{
				(KeyCode)281,
				(Key)66
			},
			{
				(KeyCode)91,
				(Key)11
			},
			{
				(KeyCode)93,
				(Key)12
			},
			{
				(KeyCode)92,
				(Key)10
			},
			{
				(KeyCode)59,
				(Key)6
			},
			{
				(KeyCode)39,
				(Key)5
			},
			{
				(KeyCode)44,
				(Key)7
			},
			{
				(KeyCode)46,
				(Key)8
			},
			{
				(KeyCode)47,
				(Key)9
			},
			{
				(KeyCode)45,
				(Key)13
			},
			{
				(KeyCode)61,
				(Key)14
			},
			{
				(KeyCode)96,
				(Key)4
			},
			{
				(KeyCode)304,
				(Key)51
			},
			{
				(KeyCode)303,
				(Key)52
			},
			{
				(KeyCode)306,
				(Key)55
			},
			{
				(KeyCode)305,
				(Key)56
			},
			{
				(KeyCode)308,
				(Key)53
			},
			{
				(KeyCode)307,
				(Key)54
			},
			{
				(KeyCode)256,
				(Key)84
			},
			{
				(KeyCode)257,
				(Key)85
			},
			{
				(KeyCode)258,
				(Key)86
			},
			{
				(KeyCode)259,
				(Key)87
			},
			{
				(KeyCode)260,
				(Key)88
			},
			{
				(KeyCode)261,
				(Key)89
			},
			{
				(KeyCode)262,
				(Key)90
			},
			{
				(KeyCode)263,
				(Key)91
			},
			{
				(KeyCode)264,
				(Key)92
			},
			{
				(KeyCode)265,
				(Key)93
			},
			{
				(KeyCode)270,
				(Key)80
			},
			{
				(KeyCode)269,
				(Key)81
			},
			{
				(KeyCode)268,
				(Key)79
			},
			{
				(KeyCode)267,
				(Key)78
			},
			{
				(KeyCode)266,
				(Key)82
			}
		};
	}
}
