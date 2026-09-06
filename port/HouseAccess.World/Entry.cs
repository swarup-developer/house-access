using HouseAccess.Game;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Interaction;
using EekCharacterEngine.Motion;
using HouseParty;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace HouseAccess.World;

public sealed class Entry
{
	public EntryKind Kind;

	public string Label;

	public Character Person;

	public InteractiveItem Item;

	public Zone Zone;

	public Door Door;

	public DistractableRigidItem Prop;

	public string Room;

	public string Where;

	public GameRefs.Room RoomRef;

	public Vector3 Point;

	public float Distance;

	public int Clock;

	public string Elevation;

	public bool InReach;

	public Vector3 GroundPoint
	{
		get
		{
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_0138: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (Kind == EntryKind.Person && Cpp.Alive((UnityEngine.Object)(object)Person))
				{
					return ((Component)Person).transform.position;
				}
				if (Kind == EntryKind.Room && RoomRef != null)
				{
					return RoomRef.FloorNearest(GameRefs.FeetPos);
				}
				if (Kind == EntryKind.Item && Cpp.Alive((UnityEngine.Object)(object)Item))
				{
					Collider componentInChildren = ((Component)Item).GetComponentInChildren<Collider>();
					Vector3 val;
					if (!Cpp.Alive((UnityEngine.Object)(object)componentInChildren))
					{
						val = ((Component)Item).transform.position;
					}
					else
					{
						Bounds bounds = componentInChildren.bounds;
						float x = bounds.center.x;
						bounds = componentInChildren.bounds;
						float y = bounds.min.y;
						bounds = componentInChildren.bounds;
						val = new Vector3(x, y, bounds.center.z);
					}
					return StandableNear(val);
				}
				if (Kind == EntryKind.Prop && Cpp.Alive((UnityEngine.Object)(object)Prop))
				{
					return StandableNear(((Component)Prop).transform.position);
				}
			}
			catch
			{
			}
			return Point;
		}
	}

	public bool Alive => Kind switch
	{
		EntryKind.Person => Cpp.Alive((UnityEngine.Object)(object)Person), 
		EntryKind.Prop => Cpp.Alive((UnityEngine.Object)(object)Prop), 
		EntryKind.Room => (UnityEngine.Object)(object)Zone == (UnityEngine.Object)null || Cpp.Alive((UnityEngine.Object)(object)Zone), 
		_ => Cpp.Alive((UnityEngine.Object)(object)Item), 
	};

	public GameObject Go => (GameObject)(Kind switch
	{
		EntryKind.Person => Cpp.Read(() => ((Component)Person).gameObject), 
		EntryKind.Prop => Cpp.Read(() => ((Component)Prop).gameObject), 
		EntryKind.Room => ((UnityEngine.Object)(object)Zone == (UnityEngine.Object)null) ? null : Cpp.Read(() => ((Component)Zone).gameObject), 
		_ => Cpp.Read(() => ((Component)Item).gameObject), 
	});

	public string BlockedBy()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		if (Kind != EntryKind.Item || !Cpp.Alive((UnityEngine.Object)(object)Item))
		{
			return null;
		}
		try
		{
			Vector3 eyePos = GameRefs.EyePos;
			Vector3 val = Point - eyePos;
			float magnitude = val.magnitude;
			if (magnitude < 0.4f)
			{
				return null;
			}
			Il2CppStructArray<RaycastHit> val2 = Physics.RaycastAll(eyePos, val.normalized, magnitude - 0.25f, -1, (QueryTriggerInteraction)1);
			if (val2 == null)
			{
				return null;
			}
			GameObject val3 = Cpp.Read(() => ((Component)Item).gameObject);
			float num = float.MaxValue;
			string result = null;
			foreach (RaycastHit item in (Il2CppArrayBase<RaycastHit>)(object)val2)
			{
				RaycastHit current = item;
				if ((UnityEngine.Object)(object)current.collider == (UnityEngine.Object)null || current.distance >= num || GameRefs.IsPlayerPart(current.transform))
				{
					continue;
				}
				GameObject gameObject = ((Component)current.collider).gameObject;
				if ((UnityEngine.Object)(object)val3 != (UnityEngine.Object)null && ((UnityEngine.Object)(object)gameObject == (UnityEngine.Object)(object)val3 || gameObject.transform.IsChildOf(val3.transform)))
				{
					continue;
				}
				InteractiveItem componentInParent = gameObject.GetComponentInParent<InteractiveItem>();
				if (Cpp.Alive((UnityEngine.Object)(object)componentInParent))
				{
					string text = GameRefs.NameOf(componentInParent);
					if (!string.IsNullOrWhiteSpace(text))
					{
						num = current.distance;
						result = text;
					}
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static Vector3 StandableNear(Vector3 point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		float y = GameRefs.FeetPos.y;
		float num = point.y - y;
		if (num < 2.4f && num > -1.5f)
		{
			return point;
		}
		try
		{
			Il2CppStructArray<RaycastHit> val = Physics.RaycastAll(point + Vector3.up * 0.2f, Vector3.down, 12f, -1, (QueryTriggerInteraction)1);
			if (val != null)
			{
				float num2 = float.MaxValue;
				Vector3 val2 = Vector3.zero;
				bool flag = false;
				foreach (RaycastHit item in (Il2CppArrayBase<RaycastHit>)(object)val)
				{
					RaycastHit current = item;
					if (!((UnityEngine.Object)(object)current.collider == (UnityEngine.Object)null) && !(Mathf.Abs(current.point.y - y) > 2.2f) && !(current.distance >= num2))
					{
						num2 = current.distance;
						val2 = current.point;
						flag = true;
					}
				}
				if (flag)
				{
					return val2 + Vector3.up * 0.05f;
				}
			}
		}
		catch
		{
		}
		return new Vector3(point.x, y, point.z);
	}

	public string Describe()
	{
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		string text = null;
		if (Kind == EntryKind.Person)
		{
			text = "person";
		}
		else if (Kind == EntryKind.Room)
		{
			text = "room";
		}
		else if (Kind == EntryKind.Door)
		{
			text = "door, " + (GameRefs.StateOf(Door) ?? "door");
			string text2 = GameRefs.SideOf(Door);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text + ", " + text2;
			}
		}
		else if (Kind == EntryKind.Prop)
		{
			text = "movable";
		}
		string text3 = TextUtil.DescribeTarget(Label, text, Distance, Clock, Elevation);
		if (Kind == EntryKind.Person && !string.IsNullOrWhiteSpace(Room))
		{
			text3 = text3 + ", in the " + Room;
		}
		if (Kind == EntryKind.Person)
		{
			string text4 = Activity.Describe(Person);
			if (!string.IsNullOrWhiteSpace(text4))
			{
				text3 = text3 + ", " + text4;
			}
			if (InReach)
			{
				string text5 = GameRefs.TalkStatus(Person);
				text3 += (string.IsNullOrWhiteSpace(text5) ? ", ready to talk" : (", " + text5));
			}
		}
		if (Kind == EntryKind.Item && Cpp.Alive((UnityEngine.Object)(object)Item))
		{
			string text6 = Placement.Describe(Item, Point);
			if (!string.IsNullOrWhiteSpace(text6))
			{
				text3 = text3 + ", " + text6;
			}
			try
			{
				Thermostat componentInParent = ((Component)Item).GetComponentInParent<Thermostat>();
				if (Cpp.Alive((UnityEngine.Object)(object)componentInParent))
				{
					string text7 = Thermostats.Reading(componentInParent);
					if (!string.IsNullOrWhiteSpace(text7))
					{
						text3 = text3 + ", set to " + text7;
					}
				}
			}
			catch
			{
			}
		}
		if (Kind == EntryKind.Item)
		{
			string text8 = BlockedBy();
			if (!string.IsNullOrWhiteSpace(text8))
			{
				text3 = text3 + ", behind the " + text8;
			}
		}
		if (InReach && Kind != EntryKind.Room)
		{
			text3 += ", in reach";
		}
		return text3;
	}
}
