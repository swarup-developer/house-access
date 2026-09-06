using System.Collections.Generic;
using System.Text;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace HouseAccess.Game;

public static class Photos
{
	private sealed class Shot
	{
		public int Number;

		public string Subject;

		public string Where;

		public string When;
	}

	private static readonly List<Shot> Taken = new List<Shot>();

	private static int _index = -1;

	public static int Count => Taken.Count;

	public static void Reset()
	{
		Taken.Clear();
		_index = -1;
	}

	public static void NotifyTaken()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Shot shot = new Shot
		{
			Number = Taken.Count + 1,
			Subject = DescribeSubject(),
			Where = GameRefs.RoomOwning(GameRefs.FeetPos)?.Name,
			When = Activity.ClockTime()
		};
		Taken.Add(shot);
		_index = Taken.Count - 1;
		Speaker.Say("Photo taken. " + Describe(shot), Pri.High);
	}

	private static bool CanSeeFrom(Vector3 eye, Vector3 target)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Vector3 val = target - eye;
			float magnitude = val.magnitude;
			if (magnitude < 0.5f)
			{
				return true;
			}
			Il2CppStructArray<RaycastHit> val2 = Physics.RaycastAll(eye, val.normalized, magnitude - 0.3f, -1, (QueryTriggerInteraction)1);
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
					if (Cpp.Alive((UnityEngine.Object)(object)((Component)current.collider).GetComponentInParent<Character>()))
					{
						continue;
					}
				}
				catch
				{
				}
				Bounds bounds = current.collider.bounds;
				if (bounds.size.x < 1.2f && bounds.size.z < 1.2f && bounds.size.y < 1.2f)
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

	private static string DescribeSubject()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		Vector3 eyePos = GameRefs.EyePos;
		Vector3 forward = GameRefs.Forward;
		List<string> list = new List<string>();
		string text = null;
		float num = float.MaxValue;
		foreach (Character item in GameRefs.Npcs())
		{
			if (!Cpp.Alive((UnityEngine.Object)(object)item))
			{
				continue;
			}
			Vector3 val = GameRefs.FramePointOf(item, eyePos);
			Vector3 val2 = val - eyePos;
			float magnitude = val2.magnitude;
			if (magnitude > 12f)
			{
				continue;
			}
			float num2 = Vector3.Angle(forward, val2);
			if (num2 > 30f || Mathf.Abs(val.y - eyePos.y) > 2.5f || !CanSeeFrom(eyePos, val))
			{
				continue;
			}
			string text2 = GameRefs.NameOf(item);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(text2);
				if (magnitude < num)
				{
					num = magnitude;
					text = text2;
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(string.Join(" and ", list));
		if (text != null)
		{
			stringBuilder.Append((num < 1.5f) ? ", very close" : ((num < 2.5f) ? ", head and chest" : ((num < 4f) ? ", most of them" : ", full length")));
		}
		return stringBuilder.ToString();
	}

	private static string Describe(Shot shot)
	{
		List<string> list = new List<string> { $"Photo {shot.Number}" };
		list.Add(string.IsNullOrWhiteSpace(shot.Subject) ? "nobody in shot" : shot.Subject);
		if (!string.IsNullOrWhiteSpace(shot.Where))
		{
			list.Add("in the " + TextUtil.Humanize(shot.Where));
		}
		if (!string.IsNullOrWhiteSpace(shot.When))
		{
			list.Add("at " + shot.When);
		}
		return string.Join(", ", list) + ".";
	}

	public static void ReadAll()
	{
		if (Taken.Count == 0)
		{
			Speaker.SayNow("No photos taken yet.");
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(TextUtil.Pluralise(Taken.Count, "photo", "photos"));
		stringBuilder.Append(". ");
		foreach (Shot item in Taken)
		{
			stringBuilder.Append(Describe(item));
			stringBuilder.Append(' ');
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 1400), Pri.High);
	}

	public static void Step(int dir)
	{
		if (Taken.Count == 0)
		{
			Speaker.SayNow("No photos taken yet.");
			return;
		}
		_index += dir;
		if (_index < 0)
		{
			_index = Taken.Count - 1;
		}
		if (_index >= Taken.Count)
		{
			_index = 0;
		}
		Speaker.Say(Describe(Taken[_index]), Pri.High);
	}
}
