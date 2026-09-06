using System.Collections.Generic;
using System.Text;
using HouseAccess.Game;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Interaction;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace HouseAccess.World;

public static class RoomScan
{
	public static void ScanRoom()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		if (!GameRefs.Ready)
		{
			Speaker.SayNow("Nothing to scan here.");
			return;
		}
		Vector3 eyePos = GameRefs.EyePos;
		Vector3 forward = GameRefs.Forward;
		forward.y = 0f;
		if (forward.sqrMagnitude < 1E-05f)
		{
			forward = Vector3.forward;
		}
		forward.Normalize();
		StringBuilder stringBuilder = new StringBuilder();
		string text = GameRefs.CurrentZoneName();
		if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.Append(TextUtil.Humanize(text));
			stringBuilder.Append(". ");
		}
		float metres = ClearDistance(eyePos, forward);
		float metres2 = ClearDistance(eyePos, -forward);
		float metres3 = ClearDistance(eyePos, Quaternion.AngleAxis(90f, Vector3.up) * forward);
		float metres4 = ClearDistance(eyePos, Quaternion.AngleAxis(-90f, Vector3.up) * forward);
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 2, stringBuilder2);
		handler.AppendLiteral("Ahead ");
		handler.AppendFormatted(TextUtil.Distance(metres));
		handler.AppendLiteral(", behind ");
		handler.AppendFormatted(TextUtil.Distance(metres2));
		handler.AppendLiteral(", ");
		stringBuilder3.Append(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder2);
		handler.AppendLiteral("left ");
		handler.AppendFormatted(TextUtil.Distance(metres4));
		handler.AppendLiteral(", right ");
		handler.AppendFormatted(TextUtil.Distance(metres3));
		handler.AppendLiteral(". ");
		stringBuilder4.Append(ref handler);
		List<string> list = new List<string>();
		for (int i = 0; i < 12; i++)
		{
			float num = (float)i * 30f;
			Vector3 dir = Quaternion.AngleAxis(num, Vector3.up) * forward;
			if (Cast(eyePos, dir, 20f, out var best, out var go))
			{
				string value = NameFor(go);
				if (!string.IsNullOrWhiteSpace(value))
				{
					int value2 = ((i == 0) ? 12 : i);
					list.Add($"{value2}, {value}, {TextUtil.Distance(best.distance)}");
				}
			}
		}
		if (list.Count == 0)
		{
			stringBuilder.Append("Nothing detected around you.");
		}
		else
		{
			stringBuilder.Append(string.Join(". ", list));
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 1400), Pri.High);
	}

	private static bool Cast(Vector3 origin, Vector3 dir, float maxDist, out RaycastHit best, out GameObject go)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		best = default(RaycastHit);
		go = null;
		try
		{
			Il2CppStructArray<RaycastHit> val = Physics.RaycastAll(origin, dir, maxDist, -1, (QueryTriggerInteraction)1);
			if (val == null)
			{
				return false;
			}
			float num = float.MaxValue;
			bool result = false;
			foreach (RaycastHit item in (Il2CppArrayBase<RaycastHit>)(object)val)
			{
				RaycastHit current = item;
				if (!((UnityEngine.Object)(object)current.collider == (UnityEngine.Object)null) && !GameRefs.IsPlayerPart(current.transform) && !(current.distance >= num))
				{
					num = current.distance;
					best = current;
					go = ((Component)current.collider).gameObject;
					result = true;
				}
			}
			return result;
		}
		catch
		{
			return false;
		}
	}

	private static string NameFor(GameObject go)
	{
		if ((UnityEngine.Object)(object)go == (UnityEngine.Object)null)
		{
			return null;
		}
		try
		{
			Character componentInParent = go.GetComponentInParent<Character>();
			if (Cpp.Alive((UnityEngine.Object)(object)componentInParent))
			{
				string text = GameRefs.NameOf(componentInParent);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
			InteractiveItem componentInParent2 = go.GetComponentInParent<InteractiveItem>();
			if (Cpp.Alive((UnityEngine.Object)(object)componentInParent2))
			{
				string text2 = GameRefs.NameOf(componentInParent2);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2;
				}
			}
		}
		catch
		{
		}
		string text3 = TextUtil.Humanize(((UnityEngine.Object)go).name);
		return string.IsNullOrWhiteSpace(text3) ? null : text3;
	}

	private static float ClearDistance(Vector3 origin, Vector3 dir, float max = 40f)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		RaycastHit best;
		GameObject go;
		return Cast(origin, dir, max, out best, out go) ? best.distance : max;
	}
}
