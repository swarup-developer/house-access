using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HouseAccess.Util;

public static class TextUtil
{
	private static readonly Regex RichTag = new Regex("<[^<>]{1,80}>", RegexOptions.Compiled);

	private static readonly Regex Whitespace = new Regex("\\s+", RegexOptions.Compiled);

	private static readonly Regex CamelSplit = new Regex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

	private static readonly Regex TrailingNumber = new Regex("[ _\\-]?\\(?\\d+\\)?$", RegexOptions.Compiled);

	/// <summary>
	/// Layout and plumbing words that Unity object names carry and that are only noise when
	/// spoken, so "BtnStartGame" reads as "Start game" rather than "Btn start game". Words
	/// that can carry meaning of their own - item, slot, group, element - are left alone.
	/// </summary>
	private static readonly Regex UiNoise = new Regex("\\b(btn|buttons?|img|images?|icons?|txt|texts?|labels?|lbl|panels?|holders?|containers?|wrappers?|gameobjects?|prefabs?|ui|gui)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static string Clean(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return string.Empty;
		}
		try
		{
			s = RichTag.Replace(s, " ");
			s = s.Replace('\u00a0', ' ').Replace('\r', ' ').Replace('\n', ' ')
				.Replace('\t', ' ');
			s = s.Replace("\\n", " ");
			s = Whitespace.Replace(s, " ");
			return s.Trim();
		}
		catch
		{
			return s;
		}
	}

	public static string Humanize(string rawName)
	{
		if (string.IsNullOrEmpty(rawName))
		{
			return string.Empty;
		}
		try
		{
			string text = rawName;
			int num = text.IndexOf("(Clone)", StringComparison.OrdinalIgnoreCase);
			if (num >= 0)
			{
				text = text.Substring(0, num);
			}
			text = text.Replace('_', ' ').Replace('-', ' ').Replace('.', ' ');
			text = TrailingNumber.Replace(text, string.Empty);
			text = CamelSplit.Replace(text, " ");
			string text2 = Whitespace.Replace(UiNoise.Replace(text, " "), " ").Trim();
			if (text2.Length > 0)
			{
				// Only take the stripped form when something is left of it: a control
				// actually called "Button" must still be spoken as "Button".
				text = text2;
			}
			text = Whitespace.Replace(text, " ").Trim();
			if (text.Length == 0)
			{
				return string.Empty;
			}
			return char.ToUpper(text[0], CultureInfo.InvariantCulture) + text.Substring(1);
		}
		catch
		{
			return rawName;
		}
	}

	public static string Distance(float metres)
	{
		if (!(metres < 1f))
		{
			if (!(metres < 10f))
			{
				return $"{Mathf.RoundToInt(metres)} metres";
			}
			return $"{metres:0.0} metres";
		}
		return $"{Mathf.RoundToInt(metres * 100f)} centimetres";
	}

	public static int ClockBearing(Vector3 cameraForward, Vector3 toTarget)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		val = new Vector3(cameraForward.x, 0f, cameraForward.z);
		Vector3 val2 = default(Vector3);
		val2 = new Vector3(toTarget.x, 0f, toTarget.z);
		if (val.sqrMagnitude < 1E-06f || val2.sqrMagnitude < 1E-06f)
		{
			return 12;
		}
		float num = Vector3.SignedAngle(val.normalized, val2.normalized, Vector3.up);
		float num2 = (num + 360f) % 360f;
		int num3 = Mathf.RoundToInt(num2 / 30f);
		if (num3 == 0 || num3 > 12)
		{
			num3 = 12;
		}
		return num3;
	}

	public static string ClockPhrase(int clock)
	{
		return clock switch
		{
			12 => "straight ahead", 
			6 => "behind you", 
			_ => $"{clock} o'clock", 
		};
	}

	public static string Elevation(Vector3 fromPos, Vector3 targetPos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		float num = targetPos.y - fromPos.y;
		if (num > 1.6f)
		{
			return "above";
		}
		if (num < -1.2f)
		{
			return "below";
		}
		return null;
	}

	public static string DescribeTarget(string label, string kind, float distance, int clock, string elevation)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(string.IsNullOrEmpty(label) ? "Unnamed" : label);
		if (!string.IsNullOrEmpty(kind))
		{
			stringBuilder.Append(", ");
			stringBuilder.Append(kind);
		}
		stringBuilder.Append(", ");
		stringBuilder.Append(Distance(distance));
		stringBuilder.Append(", ");
		stringBuilder.Append(ClockPhrase(clock));
		if (!string.IsNullOrEmpty(elevation))
		{
			stringBuilder.Append(", ");
			stringBuilder.Append(elevation);
		}
		return stringBuilder.ToString();
	}

	public static string Pluralise(int count, string singular, string plural)
	{
		return (count == 1) ? ("1 " + singular) : $"{count} {plural}";
	}

	public static string Cap(string s, int maxChars)
	{
		if (string.IsNullOrEmpty(s) || s.Length <= maxChars)
		{
			return s;
		}
		int num = s.LastIndexOf(' ', Math.Min(maxChars, s.Length - 1));
		if (num < maxChars / 2)
		{
			num = maxChars;
		}
		return s.Substring(0, num) + ", truncated";
	}
}
