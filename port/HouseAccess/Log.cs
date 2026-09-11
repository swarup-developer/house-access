using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace HouseAccess;

public static class Log
{
	private static ManualLogSource _src;

	private sealed class Failure
	{
		public string Signature;
		public DateTime NextReport;
		public int Suppressed;
	}

	private static readonly Dictionary<string, Failure> Failures = new Dictionary<string, Failure>();

	public static bool Verbose;

	public static void Bind(ManualLogSource src)
	{
		_src = src;
	}

	public static void Info(string msg)
	{
		try
		{
			if (_src != null)
			{
				_src.LogInfo(msg);
			}
		}
		catch
		{
		}
	}

	public static void Debug(string msg)
	{
		if (Verbose)
		{
			Info(msg);
		}
	}

	public static void Warn(string msg)
	{
		try
		{
			if (_src != null)
			{
				_src.LogWarning(msg);
			}
		}
		catch
		{
		}
	}

	public static void Error(string msg)
	{
		try
		{
			if (_src != null)
			{
				_src.LogError(msg);
			}
		}
		catch
		{
		}
	}

	public static void Error(string msg, Exception e)
	{
		Error($"{msg}: {e}");
	}

	public static void Guard(string what, Action action)
	{
		try
		{
			action();
			lock (Failures)
			{
				Failures.Remove(what);
			}
		}
		catch (Exception e)
		{
			// A failed per-frame bridge must remain diagnosable without writing the
			// same stack trace thousands of times. Keep trying it on subsequent frames.
			string signature = e.GetType().FullName + ": " + e.Message;
			int suppressed = 0;
			lock (Failures)
			{
				DateTime now = DateTime.UtcNow;
				if (Failures.TryGetValue(what, out Failure failure) && failure.Signature == signature)
				{
					if (now < failure.NextReport)
					{
						failure.Suppressed++;
						return;
					}
					suppressed = failure.Suppressed;
				}
				Failures[what] = new Failure { Signature = signature, NextReport = now.AddSeconds(10) };
			}
			Error("[" + what + "] threw" + (suppressed > 0 ? $" ({suppressed} repeats suppressed)" : ""), e);
		}
	}
}
