using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HouseAccess.Speech;

internal static class NativeProbe
{
	private static readonly HashSet<string> Loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static bool Preload(string fileName)
	{
		if (Loaded.Contains(fileName))
		{
			return true;
		}
		foreach (string item in SearchDirs())
		{
			string text;
			try
			{
				text = Path.Combine(item, fileName);
			}
			catch
			{
				continue;
			}
			if (!File.Exists(text))
			{
				continue;
			}
			try
			{
				if (NativeLibrary.TryLoad(text, out var handle) && handle != IntPtr.Zero)
				{
					Loaded.Add(fileName);
					Log.Info("[TTS] Loaded native library: " + text);
					return true;
				}
			}
			catch (Exception ex)
			{
				Log.Warn("[TTS] Failed loading " + text + ": " + ex.Message);
			}
		}
		try
		{
			if (NativeLibrary.TryLoad(fileName, out var handle2) && handle2 != IntPtr.Zero)
			{
				Loaded.Add(fileName);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static IEnumerable<string> SearchDirs()
	{
		string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
		yield return baseDir;
		yield return Path.Combine(baseDir, "UserLibs");
		yield return Path.Combine(baseDir, "Mods");
		yield return Path.Combine(baseDir, "MelonLoader");
		yield return Path.Combine(baseDir, "BepInEx");
		yield return Path.Combine(baseDir, "BepInEx", "Doorstop");
		yield return Path.Combine(baseDir, "BepInEx", "native");
		yield return Path.Combine(baseDir, "universal speech");
		yield return Directory.GetCurrentDirectory();
		string asmDir = null;
		try
		{
			asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}
		catch
		{
		}
		if (!string.IsNullOrEmpty(asmDir))
		{
			yield return asmDir;
		}
	}
}
