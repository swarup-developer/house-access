using System.Collections.Generic;
using HouseAccess.Game;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Interaction;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace HouseAccess.World;

public static class Placement
{
	private static readonly Dictionary<int, string> Cache = new Dictionary<int, string>();

	public static void Clear()
	{
		Cache.Clear();
	}

	public static string Describe(InteractiveItem item, Vector3 point)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		string text = Of(item, point);
		string text2 = Height(point);
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text + ", " + text2;
	}

	public static string Of(InteractiveItem item, Vector3 point)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if (!Cpp.Alive((UnityEngine.Object)(object)item))
		{
			return null;
		}
		GameObject val = Cpp.Read(() => ((Component)item).gameObject);
		if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
		{
			return null;
		}
		int instanceID = ((UnityEngine.Object)val).GetInstanceID();
		if (Cache.TryGetValue(instanceID, out var value))
		{
			return value;
		}
		string text = Compute(val, point);
		if (Cache.Count > 400)
		{
			Cache.Clear();
		}
		Cache[instanceID] = text;
		return text;
	}

	private static string Compute(GameObject go, Vector3 point)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		List<string> list = new List<string>();
		string text = FromParent(go) ?? FromBelow(go, point);
		string text2 = FromAbove(go, point);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add("under the " + text2);
		}
		else if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add("on the " + text);
		}
		return (list.Count == 0) ? null : string.Join(", ", list);
	}

	private static string FromParent(GameObject go)
	{
		try
		{
			Transform parent = go.transform.parent;
			int num = 0;
			while ((UnityEngine.Object)(object)parent != (UnityEngine.Object)null && num++ < 3)
			{
				string text = Clean(((UnityEngine.Object)parent).name);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
				parent = parent.parent;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string FromBelow(GameObject go, Vector3 point)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return Probe(go, point, Vector3.down, 1.6f);
	}

	private static string FromAbove(GameObject go, Vector3 point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (point.y > GameRefs.FeetPos.y + 0.7f)
		{
			return null;
		}
		return Probe(go, point, Vector3.up, 1.4f);
	}

	private static string Probe(GameObject go, Vector3 origin, Vector3 dir, float distance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Il2CppStructArray<RaycastHit> val = Physics.RaycastAll(origin, dir, distance, -1, (QueryTriggerInteraction)1);
			if (val == null)
			{
				return null;
			}
			float num = float.MaxValue;
			string result = null;
			foreach (RaycastHit item in (Il2CppArrayBase<RaycastHit>)(object)val)
			{
				RaycastHit current = item;
				if ((UnityEngine.Object)(object)current.collider == (UnityEngine.Object)null || current.distance >= num)
				{
					continue;
				}
				GameObject gameObject = ((Component)current.collider).gameObject;
				if (!((UnityEngine.Object)(object)gameObject == (UnityEngine.Object)(object)go) && !IsPartOf(gameObject.transform, go.transform) && !GameRefs.IsPlayerPart(current.transform))
				{
					string text = NameOf(gameObject);
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

	private static bool IsPartOf(Transform t, Transform root)
	{
		int num = 0;
		while ((UnityEngine.Object)(object)t != (UnityEngine.Object)null && num++ < 8)
		{
			if ((UnityEngine.Object)(object)t == (UnityEngine.Object)(object)root)
			{
				return true;
			}
			t = t.parent;
		}
		return false;
	}

	private static string NameOf(GameObject go)
	{
		try
		{
			InteractiveItem componentInParent = go.GetComponentInParent<InteractiveItem>();
			if (Cpp.Alive((UnityEngine.Object)(object)componentInParent))
			{
				string text = GameRefs.NameOf(componentInParent);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return Clean(text);
				}
			}
		}
		catch
		{
		}
		return Clean(((UnityEngine.Object)go).name);
	}

	private static string Clean(string raw)
	{
		string text = TextUtil.Humanize(raw);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (text.Length < 3 || text.Length > 28)
		{
			return null;
		}
		string text2 = text.ToLowerInvariant();
		string[] array = new string[25]
		{
			"collider", "collision", "mesh", "group", "container", "holder", "parent", "root", "pivot", "anchor",
			"spawn", "point", "empty", "object", "prefab", "clone", "geo", "lod", "static", "default",
			"position", "transform", "node", "item", "trigger"
		};
		string[] array2 = array;
		foreach (string value in array2)
		{
			if (text2.Contains(value))
			{
				return null;
			}
		}
		if (text2.Contains("floor") || text2.Contains("ground"))
		{
			return "floor";
		}
		return text;
	}

	private static string Height(Vector3 point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		float y = GameRefs.FeetPos.y;
		float num = point.y - y;
		if (num < -0.25f)
		{
			return IsCrouching() ? "low down" : "low down, crouch to reach it";
		}
		if (num > 1.9f)
		{
			return "up high";
		}
		return null;
	}

	public static bool IsCrouching()
	{
		try
		{
			PlayerCharacter p = GameRefs.Player;
			return (UnityEngine.Object)(object)p != (UnityEngine.Object)null && Cpp.Read(() => ((Character)p).IsCrouching, fallback: false);
		}
		catch
		{
			return false;
		}
	}

	public static bool NeedsCrouch(Vector3 point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return point.y - GameRefs.FeetPos.y < -0.25f && !IsCrouching();
	}
}
