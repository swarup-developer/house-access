using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using UnityEngine;
using UnityEngine.Playables;

namespace HouseAccess.Game;

public static class CutsceneBridge
{
	private static Dictionary<string, string> _written;

	private static CutSceneManager _manager;

	private static bool _wasPlaying;

	private static string _current;

	private static float _endsAt;

	private static float _nextCheck;

	public static bool Playing => _wasPlaying;

	public static string FilePath => Path.Combine(Diagnostics.Directory, "cutscenes.txt");

	private static CutSceneManager Manager
	{
		get
		{
			if (Cpp.Alive((UnityEngine.Object)(object)_manager))
			{
				return _manager;
			}
			_manager = Cpp.FindOne<CutSceneManager>(activeOnly: false);
			return _manager;
		}
	}

	public static void Reset()
	{
		_manager = null;
		_wasPlaying = false;
		_current = null;
		_endsAt = 0f;
	}

	public static void Load()
	{
		_written = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (!File.Exists(FilePath))
			{
				WriteTemplate();
				return;
			}
			string[] array = File.ReadAllLines(FilePath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i]?.Trim();
				if (string.IsNullOrEmpty(text) || text.StartsWith("#"))
				{
					continue;
				}
				int num = text.IndexOf('=');
				if (num > 0)
				{
					string text2 = text.Substring(0, num).Trim();
					string text3 = text.Substring(num + 1).Trim();
					if (text2.Length != 0 && text3.Length != 0)
					{
						_written[text2] = text3;
					}
				}
			}
			Log.Info($"Loaded {_written.Count} cutscene descriptions.");
		}
		catch (Exception ex)
		{
			Log.Warn("Could not load cutscene descriptions: " + ex.Message);
		}
	}

	private static void WriteTemplate()
	{
		try
		{
			Directory.CreateDirectory(Diagnostics.Directory);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# Descriptions of cutscenes, read aloud when one starts.");
			stringBuilder.AppendLine("#");
			stringBuilder.AppendLine("# One per line:   SceneName = what happens");
			stringBuilder.AppendLine("#");
			stringBuilder.AppendLine("# The mod already announces that a scene has begun, who is in it and how");
			stringBuilder.AppendLine("# long it lasts. What it cannot know is what the scene shows, so that is");
			stringBuilder.AppendLine("# what belongs here.");
			stringBuilder.AppendLine("#");
			stringBuilder.AppendLine("# Scene names are written to MelonLoader\\Latest.log as each one plays,");
			stringBuilder.AppendLine("# so play through once and the names you need will be in there.");
			stringBuilder.AppendLine();
			File.WriteAllText(FilePath, stringBuilder.ToString());
			Log.Info("Created a cutscene description file at " + FilePath);
		}
		catch (Exception ex)
		{
			Log.Warn("Could not create the cutscene file: " + ex.Message);
		}
	}

	private static CutScene Current()
	{
		CutSceneManager manager = Manager;
		if (!Cpp.Alive((UnityEngine.Object)(object)manager))
		{
			return null;
		}
		// This build keeps the playing-scene list in private state, so the running scene
		// is found instead by watching each loaded CutScene's own timeline director.
		try
		{
			foreach (CutScene scene in Cpp.FindAll<CutScene>(activeOnly: false))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)scene))
				{
					continue;
				}
				PlayableDirector director = Cpp.Read(() => scene.DODBABFKFJA);
				if (Cpp.Alive((UnityEngine.Object)(object)director) && Cpp.Read(() => director.state, PlayState.Paused) == PlayState.Playing)
				{
					return scene;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	public static void Tick()
	{
		if (Time.unscaledTime < _nextCheck)
		{
			return;
		}
		_nextCheck = Time.unscaledTime + 0.3f;
		CutSceneManager manager = Manager;
		bool flag = Cpp.Alive((UnityEngine.Object)(object)manager) && Current() != null;
		if (flag == _wasPlaying)
		{
			if (flag && _endsAt > 0f && Time.unscaledTime > _endsAt)
			{
				_endsAt = 0f;
				Speaker.Say("Scene still playing.", Pri.Low);
			}
		}
		else
		{
			_wasPlaying = flag;
			if (flag)
			{
				Began();
			}
			else
			{
				Ended();
			}
		}
	}

	private static void Began()
	{
		CutScene scene = Current();
		_current = NameOf(scene);
		Log.Info("[cutscene] started: " + (_current ?? "(unnamed)"));
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Scene starting");
		string value = Cast(scene);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.Append(", with ");
			stringBuilder.Append(value);
		}
		float num = Length(scene);
		if (num > 1f)
		{
			stringBuilder.Append(", about ");
			stringBuilder.Append(Mathf.RoundToInt(num));
			stringBuilder.Append(" seconds");
			_endsAt = Time.unscaledTime + num * 0.5f;
		}
		stringBuilder.Append('.');
		string value2 = Description(_current);
		if (!string.IsNullOrWhiteSpace(value2))
		{
			stringBuilder.Append(' ');
			stringBuilder.Append(value2);
		}
		Speaker.Say(TextUtil.Cap(stringBuilder.ToString(), 900), Pri.High);
	}

	private static void Ended()
	{
		_endsAt = 0f;
		Log.Info("[cutscene] ended: " + (_current ?? "(unnamed)"));
		Speaker.Say("Scene over. You have control again.", Pri.High);
		_current = null;
	}

	private static string NameOf(CutScene scene)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)scene))
		{
			return null;
		}
		try
		{
			GameObject val = Cpp.Read(() => ((Component)scene).gameObject);
			return ((UnityEngine.Object)(object)val == (UnityEngine.Object)null) ? null : ((UnityEngine.Object)val).name;
		}
		catch
		{
			return null;
		}
	}

	private static string Cast(CutScene scene)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)scene))
		{
			return null;
		}
		List<string> list = new List<string>();
		try
		{
			// The scene keeps its actors in private state, but the manager can say who is
			// acting in a scene right now, so the live cast is whoever it flags.
			foreach (Character c in Cpp.FindAll<Character>(activeOnly: true))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)c) || !CutSceneManager.IsActorInCutScene(c))
				{
					continue;
				}
				string text = GameRefs.NameOf(c);
				if (!string.IsNullOrWhiteSpace(text) && !list.Contains(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			try
			{
				string originalNPC1 = Cpp.Read(() => scene.OriginalNPC1);
				if (!string.IsNullOrWhiteSpace(originalNPC1))
				{
					list.Add(originalNPC1);
				}
				string originalNPC2 = Cpp.Read(() => scene.OriginalNPC2);
				if (!string.IsNullOrWhiteSpace(originalNPC2))
				{
					list.Add(originalNPC2);
				}
				string originalNPC3 = Cpp.Read(() => scene.OriginalNPC3);
				if (!string.IsNullOrWhiteSpace(originalNPC3))
				{
					list.Add(originalNPC3);
				}
			}
			catch
			{
			}
		}
		return (list.Count == 0) ? null : string.Join(" and ", list);
	}

	private static float Length(CutScene scene)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)scene))
		{
			return 0f;
		}
		try
		{
			PlayableDirector director = Cpp.Read(() => scene.DODBABFKFJA);
			if (Cpp.Alive((UnityEngine.Object)(object)director))
			{
				return (float)Cpp.Read(() => director.duration, 0.0);
			}
		}
		catch
		{
		}
		return 0f;
	}

	private static string Description(string sceneName)
	{
		if (_written == null || string.IsNullOrWhiteSpace(sceneName))
		{
			return null;
		}
		if (_written.TryGetValue(sceneName.Trim(), out var value))
		{
			return value;
		}
		foreach (KeyValuePair<string, string> item in _written)
		{
			if (sceneName.IndexOf(item.Key, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return item.Value;
			}
		}
		return null;
	}
}
