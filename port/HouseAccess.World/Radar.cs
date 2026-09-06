using System;
using System.Collections.Generic;
using HouseAccess.Game;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Interaction;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace HouseAccess.World;

public static class Radar
{
	private static readonly List<Entry> Entries = new List<Entry>();

	private static float _lastSweep = -999f;

	private static int _index = -1;

	private static string _roomName;

	private static InteractiveItem _retryItem;

	private static string _retryLabel;

	private static float _retryAt;

	public static Filter Mode { get; private set; } = Filter.All;

	public static IReadOnlyList<Entry> All => Entries;

	public static Entry Current
	{
		get
		{
			if (_index < 0 || _index >= Entries.Count)
			{
				return null;
			}
			Entry entry = Entries[_index];
			return (entry != null && entry.Alive) ? entry : null;
		}
	}

	public static void Reset()
	{
		Placement.Clear();
		Entries.Clear();
		_index = -1;
		_lastSweep = -999f;
	}

	public static void Tick()
	{
		if (Cpp.Alive((UnityEngine.Object)(object)_retryItem) && Time.unscaledTime >= _retryAt)
		{
			InteractiveItem retryItem = _retryItem;
			string retryLabel = _retryLabel;
			_retryAt = float.MaxValue;
			if (GameRefs.CanActOn(retryItem))
			{
				_retryItem = null;
				ActionPicker.Open(retryItem, retryLabel);
				return;
			}
			_retryItem = null;
			if (Cpp.Alive((UnityEngine.Object)(object)retryItem))
			{
				Speaker.Say(retryLabel + " is out of reach, but you may be able to use something on it.", Pri.High);
				ActionPicker.Open(retryItem, retryLabel);
				return;
			}
			Speaker.Say(retryLabel + " is out of reach. Opening its options in case something you carry will reach it.", Pri.High);
			ActionPicker.Open(retryItem, retryLabel);
		}
		if (Time.unscaledTime - _lastSweep >= Mathf.Max(0.15f, Prefs.ScanIntervalSec.Value))
		{
			_lastSweep = Time.unscaledTime;
			Sweep();
		}
		else
		{
			Refresh();
		}
	}

	private static void RefreshOne(Entry e)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		if (e == null || !GameRefsReady())
		{
			return;
		}
		try
		{
			Vector3 eyePos = GameRefs.EyePos;
			Vector3 forward = GameRefs.Forward;
			e.Point = PointOf(e);
			Vector3 toTarget = e.Point - eyePos;
			e.Distance = ((e.Kind == EntryKind.Room && e.RoomRef != null) ? e.RoomRef.DistanceFrom(eyePos) : toTarget.magnitude);
			e.Clock = TextUtil.ClockBearing(forward, toTarget);
			e.Elevation = TextUtil.Elevation(eyePos, e.Point);
			if (e.Kind == EntryKind.Item || e.Kind == EntryKind.Door)
			{
				e.InReach = Cpp.Read(() => e.Item.InPlayerInteractionRange(), fallback: false);
			}
			else if (e.Kind == EntryKind.Person)
			{
				e.InReach = (Cpp.Alive((UnityEngine.Object)(object)e.Item) ? Cpp.Read(() => e.Item.InPlayerInteractionRange(), fallback: false) : (e.Distance <= 1.6f));
			}
		}
		catch
		{
		}
	}

	private static void Refresh()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		if (!GameRefsReady())
		{
			return;
		}
		Vector3 eyePos = GameRefs.EyePos;
		Vector3 forward = GameRefs.Forward;
		for (int num = Entries.Count - 1; num >= 0; num--)
		{
			Entry e = Entries[num];
			if (e == null || !e.Alive)
			{
				Entries.RemoveAt(num);
				if (num <= _index)
				{
					_index--;
				}
			}
			else
			{
				e.Point = PointOf(e);
				Vector3 toTarget = e.Point - eyePos;
				e.Distance = ((e.Kind == EntryKind.Room && e.RoomRef != null) ? e.RoomRef.DistanceFrom(eyePos) : toTarget.magnitude);
				e.Clock = TextUtil.ClockBearing(forward, toTarget);
				e.Elevation = TextUtil.Elevation(eyePos, e.Point);
				if (e.Kind == EntryKind.Item)
				{
					e.InReach = Cpp.Read(() => e.Item.InPlayerInteractionRange(), fallback: false);
				}
				else if (e.Kind == EntryKind.Person)
				{
					e.InReach = (Cpp.Alive((UnityEngine.Object)(object)e.Item) ? Cpp.Read(() => e.Item.InPlayerInteractionRange(), fallback: false) : (e.Distance <= 1.6f));
					e.Room = GameRefs.RoomOf(e.Person);
				}
			}
		}
		if (_index >= Entries.Count)
		{
			_index = Entries.Count - 1;
		}
	}

	private static void Sweep()
	{
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Unknown result type (might be due to invalid IL or missing references)
		//IL_046b: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_0508: Unknown result type (might be due to invalid IL or missing references)
		//IL_0576: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0580: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05df: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		if (!GameRefsReady())
		{
			return;
		}
		int num = ((Current != null && (UnityEngine.Object)(object)Current.Go != (UnityEngine.Object)null) ? ((UnityEngine.Object)Current.Go).GetInstanceID() : 0);
		Entries.Clear();
		Vector3 eyePos = GameRefs.EyePos;
		Vector3 forward = GameRefs.Forward;
		float num2 = Mathf.Max(3f, Prefs.ScanRadius.Value);
		int num3 = Mathf.Max(8, Prefs.MaxTargets.Value);
		GameRefs.Room here = null;
		if (Prefs.RoomOnly.Value)
		{
			string text = GameRefs.CurrentZoneName();
			if (!string.IsNullOrWhiteSpace(text))
			{
				foreach (GameRefs.Room item3 in GameRefs.Rooms())
				{
					if (string.Equals(item3.Name, text.Trim(), StringComparison.OrdinalIgnoreCase))
					{
						here = item3;
						break;
					}
				}
			}
			if (here == null)
			{
				here = GameRefs.RoomOwning(GameRefs.FeetPos);
			}
		}
		_roomName = here?.Name;
		foreach (Character item4 in GameRefs.Npcs())
		{
			string text2 = GameRefs.NameOf(item4);
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			Vector3 val = GameRefs.AimPointOf(item4);
			float num4 = Vector3.Distance(eyePos, val);
			if (!(num4 > num2))
			{
				InteractiveItem item = null;
				try
				{
					item = ((Component)item4).GetComponentInChildren<InteractiveItem>();
				}
				catch
				{
				}
				Entries.Add(new Entry
				{
					Kind = EntryKind.Person,
					Item = item,
					Person = item4,
					Label = text2,
					Room = GameRefs.RoomOf(item4),
					Point = val,
					Distance = num4,
					Clock = TextUtil.ClockBearing(forward, val - eyePos),
					Elevation = TextUtil.Elevation(eyePos, val),
					InReach = (num4 <= 2.5f)
				});
			}
		}
		foreach (InteractiveItem item2 in GameRefs.Items())
		{
			string text3 = GameRefs.NameOf(item2);
			if (string.IsNullOrEmpty(text3))
			{
				continue;
			}
			Transform val2 = Cpp.Read(() => ((Component)item2).transform);
			if (!Cpp.Alive((UnityEngine.Object)(object)val2))
			{
				continue;
			}
			Vector3 val3 = PointOfTransform(item2, val2);
			float num5 = Vector3.Distance(eyePos, val3);
			if (num5 > num2)
			{
				continue;
			}
			Door val4 = ((Il2CppObjectBase)item2).TryCast<Door>();
			if ((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null && !GameRefs.IsWalkThrough(val4))
			{
				val4 = null;
			}
			bool num6;
			if (!((UnityEngine.Object)(object)val4 == (UnityEngine.Object)null))
			{
				if (SameRoom(val3))
				{
					goto IL_0358;
				}
				num6 = num5 > 6f;
			}
			else
			{
				num6 = !SameRoom(val3);
			}
			if (num6)
			{
				continue;
			}
			goto IL_0358;
			IL_0358:
			Entries.Add(new Entry
			{
				Kind = ((!((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)) ? EntryKind.Item : EntryKind.Door),
				Door = val4,
				Item = item2,
				Label = (GameRefs.DisplayLabel(item2) ?? text3),
				Point = val3,
				Distance = num5,
				Clock = TextUtil.ClockBearing(forward, val3 - eyePos),
				Elevation = TextUtil.Elevation(eyePos, val3),
				InReach = GameRefs.CanActOn(item2),
				Where = null
			});
		}
		foreach (DistractableRigidItem prop in GameRefs.Props())
		{
			Transform val5 = Cpp.Read(() => ((Component)prop).transform);
			if (!Cpp.Alive((UnityEngine.Object)(object)val5))
			{
				continue;
			}
			Vector3 position = val5.position;
			float num7 = Vector3.Distance(eyePos, position);
			if (!(num7 > num2) && SameRoom(position))
			{
				string text4 = GameRefs.NameOf(prop);
				if (!string.IsNullOrWhiteSpace(text4))
				{
					Entries.Add(new Entry
					{
						Kind = EntryKind.Prop,
						Prop = prop,
						Label = text4,
						Point = position,
						Distance = num7,
						Clock = TextUtil.ClockBearing(forward, position - eyePos),
						Elevation = TextUtil.Elevation(eyePos, position),
						InReach = (num7 <= 2.5f)
					});
				}
			}
		}
		float num8 = Mathf.Max(num2, 60f);
		foreach (GameRefs.Room item5 in GameRefs.Rooms())
		{
			Vector3 val6 = item5.FloorNearest(eyePos);
			float num9 = item5.DistanceFrom(eyePos);
			if (!(num9 > num8))
			{
				Entries.Add(new Entry
				{
					Kind = EntryKind.Room,
					Zone = item5.Zone,
					RoomRef = item5,
					Label = TextUtil.Humanize(item5.Name),
					Point = val6,
					Distance = num9,
					Clock = TextUtil.ClockBearing(forward, val6 - eyePos),
					Elevation = TextUtil.Elevation(eyePos, val6),
					InReach = false
				});
			}
		}
		Entries.RemoveAll((Entry e) => !Passes(e));
		Entries.Sort(delegate(Entry a, Entry b)
		{
			int num12 = Rank(a.Kind);
			int num13 = Rank(b.Kind);
			return (num12 != num13) ? (num12 - num13) : a.Distance.CompareTo(b.Distance);
		});
		if (Entries.Count > num3)
		{
			List<Entry> list = new List<Entry>();
			int num10 = 0;
			foreach (Entry entry in Entries)
			{
				if (entry.Kind == EntryKind.Room)
				{
					list.Add(entry);
				}
				else if (num10++ < num3)
				{
					list.Add(entry);
				}
			}
			if (list.Count < Entries.Count)
			{
				Log.Debug($"Radar: {Entries.Count} in range, nearest {list.Count} kept.");
			}
			Entries.Clear();
			Entries.AddRange(list);
		}
		_index = -1;
		if (num != 0)
		{
			for (int num11 = 0; num11 < Entries.Count; num11++)
			{
				GameObject go = Entries[num11].Go;
				if ((UnityEngine.Object)(object)go != (UnityEngine.Object)null && ((UnityEngine.Object)go).GetInstanceID() == num)
				{
					_index = num11;
					break;
				}
			}
		}
		if (_index < 0 && Entries.Count > 0)
		{
			_index = 0;
		}
		bool SameRoom(Vector3 point)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			if (here == null)
			{
				return true;
			}
			Vector3 feetPos = GameRefs.FeetPos;
			if (Vector3.Distance(feetPos, point) <= 3.5f && Mathf.Abs(point.y - feetPos.y) < 2.2f && ClearLineTo(point))
			{
				return true;
			}
			GameRefs.Room room = GameRefs.RoomOwning(point);
			if (room != null)
			{
				return string.Equals(room.Name, here.Name, StringComparison.OrdinalIgnoreCase);
			}
			return Vector3.Distance(GameRefs.FeetPos, point) <= 6f;
		}
	}

	private static bool GameRefsReady()
	{
		return GameRefs.Ready;
	}

	private static int Rank(EntryKind k)
	{
		return k switch
		{
			EntryKind.Person => 0, 
			EntryKind.Door => 1, 
			EntryKind.Item => 2, 
			EntryKind.Prop => 3, 
			_ => 3, 
		};
	}

	private static bool ClearLineTo(Vector3 point)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Vector3 eyePos = GameRefs.EyePos;
			Vector3 val = point - eyePos;
			float magnitude = val.magnitude;
			if (magnitude < 0.4f)
			{
				return true;
			}
			Il2CppStructArray<RaycastHit> val2 = Physics.RaycastAll(eyePos, val.normalized, magnitude - 0.25f, -1, (QueryTriggerInteraction)1);
			if (val2 == null)
			{
				return true;
			}
			foreach (RaycastHit item in (Il2CppArrayBase<RaycastHit>)(object)val2)
			{
				RaycastHit current = item;
				if ((UnityEngine.Object)(object)current.collider == (UnityEngine.Object)null || GameRefs.IsPlayerPart(current.transform))
				{
					continue;
				}
				try
				{
					if (Cpp.Alive((UnityEngine.Object)(object)((Component)current.collider).GetComponentInParent<InteractiveItem>()))
					{
						continue;
					}
				}
				catch
				{
				}
				Bounds bounds = current.collider.bounds;
				if (bounds.size.x < 1.5f && bounds.size.z < 1.5f)
				{
					continue;
				}
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static bool Passes(Entry e)
	{
		return Mode switch
		{
			Filter.People => e.Kind == EntryKind.Person, 
			Filter.Items => e.Kind == EntryKind.Item || e.Kind == EntryKind.Prop, 
			Filter.Doors => e.Kind == EntryKind.Door, 
			Filter.Rooms => e.Kind == EntryKind.Room, 
			Filter.InReach => e.InReach, 
			_ => true, 
		};
	}

	private static Vector3 PointOf(Entry e)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (e.Kind == EntryKind.Room)
		{
			return e.Point;
		}
		if (e.Kind == EntryKind.Person)
		{
			return GameRefs.AimPointOf(e.Person);
		}
		Transform val = Cpp.Read(() => ((Component)e.Item).transform);
		return Cpp.Alive((UnityEngine.Object)(object)val) ? PointOfTransform(e.Item, val) : e.Point;
	}

	private static Vector3 PointOfTransform(InteractiveItem item, Transform t)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Collider componentInChildren = ((Component)item).GetComponentInChildren<Collider>();
			if (Cpp.Alive((UnityEngine.Object)(object)componentInChildren))
			{
				Bounds bounds = componentInChildren.bounds;
				return bounds.center;
			}
		}
		catch
		{
		}
		return t.position;
	}

	public static void Next()
	{
		Step(1);
	}

	public static void Prev()
	{
		Step(-1);
	}

	private static void Step(int dir)
	{
		if (Entries.Count == 0)
		{
			Sweep();
			if (Entries.Count == 0)
			{
				string text = ((Mode == Filter.All) ? ("Nothing within " + TextUtil.Distance(Prefs.ScanRadius.Value) + ".") : ("Nothing in this category. Press " + Prefs.KeyCycleFilter.Value + " for everything."));
				Speaker.Say(text, Pri.High);
				return;
			}
		}
		_index = ((_index >= 0) ? ((_index + dir + Entries.Count) % Entries.Count) : ((dir <= 0) ? (Entries.Count - 1) : 0));
		AnnounceCurrent();
	}

	public static void CycleFilter()
	{
		CycleFilter(1);
	}

	public static void CycleFilter(int direction)
	{
		Mode = (Filter)(((int)(Mode + direction) % 6 + 6) % 6);
		Sweep();
		string value = Mode switch
		{
			Filter.People => "People", 
			Filter.Items => "Items", 
			Filter.Doors => "Doors", 
			Filter.Rooms => "Rooms", 
			Filter.InReach => "Within reach", 
			_ => "Everything", 
		};
		string value2 = ((Prefs.RoomOnly.Value && (Mode == Filter.Items || Mode == Filter.InReach) && !string.IsNullOrWhiteSpace(_roomName)) ? (" in the " + TextUtil.Humanize(_roomName)) : string.Empty);
		Speaker.SayNow($"{value}{value2}. {TextUtil.Pluralise(Entries.Count, "target", "targets")}.");
		if (Entries.Count > 0)
		{
			_index = 0;
			AnnounceCurrent();
		}
	}

	public static bool SelectEntry(Entry wanted)
	{
		if (wanted == null)
		{
			return false;
		}
		GameObject go = wanted.Go;
		for (int i = 0; i < Entries.Count; i++)
		{
			Entry entry = Entries[i];
			if (entry != null)
			{
				bool flag = entry == wanted;
				if (!flag && (UnityEngine.Object)(object)go != (UnityEngine.Object)null)
				{
					GameObject go2 = entry.Go;
					flag = (UnityEngine.Object)(object)go2 != (UnityEngine.Object)null && ((UnityEngine.Object)go2).GetInstanceID() == ((UnityEngine.Object)go).GetInstanceID();
				}
				if (flag)
				{
					_index = i;
					AnnounceCurrent();
					return true;
				}
			}
		}
		Sweep();
		for (int j = 0; j < Entries.Count; j++)
		{
			Entry entry2 = Entries[j];
			if (entry2 != null && !((UnityEngine.Object)(object)go == (UnityEngine.Object)null))
			{
				GameObject go3 = entry2.Go;
				if (!((UnityEngine.Object)(object)go3 == (UnityEngine.Object)null) && ((UnityEngine.Object)go3).GetInstanceID() == ((UnityEngine.Object)go).GetInstanceID())
				{
					_index = j;
					AnnounceCurrent();
					return true;
				}
			}
		}
		return false;
	}

	public static void AnnounceCurrent()
	{
		Entry current = Current;
		if (current == null)
		{
			Speaker.Say("No target selected.", Pri.High);
			return;
		}
		RefreshOne(current);
		string text = ((Entries.Count > 1) ? $", {_index + 1} of {Entries.Count}" : string.Empty);
		Speaker.Say(current.Describe() + text, Pri.High);
	}

	public static void AnnounceInteractions()
	{
		Entry current = Current;
		if (current == null)
		{
			Speaker.Say("No target selected.", Pri.High);
			return;
		}
		if (current.Kind == EntryKind.Room)
		{
			Speaker.Say(current.Label + ". A room. Press walk to go there.", Pri.High);
			return;
		}
		if (current.Kind == EntryKind.Door)
		{
			string value = GameRefs.StateOf(current.Door);
			string text = GameRefs.KeyFor(current.Door);
			string value2 = (string.IsNullOrEmpty(text) ? string.Empty : (" Needs the " + text + "."));
			Speaker.Say($"{current.Label}, {value}. Interact to {(GameRefs.IsShut(current.Door) ? "open" : "close")}.{value2}", Pri.High);
			return;
		}
		if (current.Kind == EntryKind.Person)
		{
			List<string> list = new List<string> { current.Label };
			if (!string.IsNullOrWhiteSpace(current.Room))
			{
				list.Add("in the " + current.Room);
			}
			string text2 = Activity.Describe(current.Person);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(text2);
			}
			string text3 = GameRefs.TalkStatus(current.Person);
			list.Add(string.IsNullOrWhiteSpace(text3) ? "ready to talk" : text3);
			Speaker.SayParts(Pri.High, list.ToArray());
			return;
		}
		List<GameRefs.Verb> list2 = GameRefs.VerbsOf(current.Item);
		if (list2.Count == 0)
		{
			Speaker.Say(current.Label + ". No options available right now.", Pri.High);
			return;
		}
		List<string> list3 = new List<string>();
		foreach (GameRefs.Verb item in list2)
		{
			string text4 = TextUtil.Humanize(item.Name);
			list3.Add(item.Available ? text4 : (text4 + " unavailable"));
		}
		Speaker.Say(current.Label + ". " + string.Join(", ", list3) + ".", Pri.High);
	}

	public static void InteractWithCurrent()
	{
		//IL_0501: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_054f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		Entry current = Current;
		if (current == null)
		{
			Speaker.SayNow("No target selected.");
			return;
		}
		if (current.Kind == EntryKind.Room)
		{
			Speaker.SayNow(current.Label + " is a room. Press walk to go there.");
			return;
		}
		if (current.Kind == EntryKind.Door)
		{
			if (current.Distance > 3.5f)
			{
				Speaker.SayNow(current.Label + " is " + TextUtil.Distance(current.Distance) + " away. Walk to it first.");
				return;
			}
			List<GameRefs.Verb> list = GameRefs.VerbsOf(current.Item);
			bool flag = false;
			foreach (GameRefs.Verb item in list)
			{
				string text = (item.Name ?? string.Empty).ToLowerInvariant();
				if (text.Contains("open") || text.Contains("close") || text.Contains("inspect") || text.Contains("interact"))
				{
					continue;
				}
				flag = true;
				break;
			}
			if (flag)
			{
				ActionPicker.Open(current.Item, current.Label);
			}
			else if (GameRefs.IsLocked(current.Door))
			{
				string text2 = GameRefs.KeyFor(current.Door);
				Speaker.SayNow(string.IsNullOrWhiteSpace(text2) ? (current.Label + " is locked. Looking again for a way to unlock it.") : (current.Label + " is locked and needs the " + text2 + "."));
				_retryItem = current.Item;
				_retryLabel = current.Label;
				_retryAt = Time.unscaledTime + 0.4f;
			}
			else
			{
				string text3 = GameRefs.ToggleDoor(current.Door);
				Speaker.SayNow(current.Label + ". " + text3);
			}
			return;
		}
		if (current.Kind == EntryKind.Prop)
		{
			Speaker.SayNow(current.InReach ? (current.Label + " can only be picked up. Use the pick up key.") : (current.Label + " is " + TextUtil.Distance(current.Distance) + " away. Walk closer to pick it up."));
			return;
		}
		if (current.Kind == EntryKind.Item)
		{
			if (!current.InReach && current.Distance <= 3.5f && _retryItem != current.Item)
			{
				_retryItem = current.Item;
				_retryLabel = current.Label;
				_retryAt = Time.unscaledTime + 0.5f;
				Navigator.FacePoint(current.Point);
				Speaker.SayNow("Turning to " + current.Label + ".");
				return;
			}
			_retryItem = null;
			if (!current.InReach)
			{
				if (current.Distance < 1.8f)
				{
					Speaker.SayNow(current.Label + " is right here but the game will not let you take it. Opening its options.");
					ActionPicker.Open(current.Item, current.Label);
					return;
				}
				float num = current.Point.y - GameRefs.FeetPos.y;
				if (num > 2.4f && current.Distance < 5f)
				{
					Speaker.SayNow(current.Label + " is above you, out of reach. Something long may reach it.");
					ActionPicker.Open(current.Item, current.Label);
					return;
				}
				if (num > 1.2f && current.Distance < 3f)
				{
					ActionPicker.Open(current.Item, current.Label);
					return;
				}
				string text4 = current.BlockedBy();
				if (!string.IsNullOrWhiteSpace(text4))
				{
					Speaker.SayNow(current.Label + " is behind the " + text4 + ". Move it out of the way first.");
				}
				else if (current.Distance < 2.5f && Placement.NeedsCrouch(current.Point))
				{
					Speaker.SayNow(current.Label + " is below you. Crouch to reach it.");
				}
				else
				{
					Speaker.SayNow(current.Label + " is " + TextUtil.Distance(current.Distance) + " away. Too far to reach.");
				}
			}
			else
			{
				ActionPicker.Open(current.Item, current.Label);
			}
			return;
		}
		InteractiveItem val = null;
		try
		{
			val = ((Component)current.Person).GetComponentInChildren<InteractiveItem>();
		}
		catch (Exception ex)
		{
			Log.Warn("Person proxy lookup failed: " + ex.Message);
		}
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			Speaker.SayNow("Facing " + current.Label + ". Press your interact key to talk.");
			return;
		}
		float num2 = Vector3.Distance(GameRefs.FeetPos, GameRefs.AimPointOf(current.Person));
		if (num2 > 3.5f)
		{
			Speaker.SayNow(current.Label + " is " + TextUtil.Distance(num2) + " away. Walk closer first.");
			return;
		}
		Navigator.FacePoint(GameRefs.AimPointOf(current.Person));
		ActionPicker.Open(val, current.Label);
	}

	public static List<Entry> PeopleNearby()
	{
		List<Entry> list = new List<Entry>();
		foreach (Entry entry in Entries)
		{
			if (entry != null && entry.Alive && entry.Kind == EntryKind.Person)
			{
				list.Add(entry);
			}
		}
		return list;
	}

	public static void ForceSweep()
	{
		Sweep();
	}
}
