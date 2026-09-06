using System;
using BepInEx.Logging;

namespace HouseAccess;

public static class Log
{
	private static ManualLogSource _src;

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
		}
		catch (Exception e)
		{
			Error("[" + what + "] threw", e);
		}
	}
}
