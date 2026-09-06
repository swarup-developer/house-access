using System;
using HouseAccess.Game;
using HouseAccess.Speech;
using HouseAccess.Util;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace HouseAccess.World;

public static class Beacon
{
	private static GameObject _host;

	private static AudioSource _source;

	private static AudioClip _ping;

	private static AudioClip _arrive;

	private static float _nextPing;

	public static bool Enabled { get; private set; }

	public static void Toggle()
	{
		Enabled = !Enabled;
		if (!Enabled)
		{
			try
			{
				if (Cpp.Alive((UnityEngine.Object)(object)_source))
				{
					_source.Stop();
				}
			}
			catch
			{
			}
			Speaker.SayNow("Sonar off.");
		}
		else
		{
			EnsureHost();
			if (Cpp.Alive((UnityEngine.Object)(object)_source))
			{
				Speaker.SayNow("Sonar on.");
				return;
			}
			Enabled = false;
			Speaker.SayNow("Sonar unavailable.");
		}
	}

	public static void Shutdown()
	{
		try
		{
			if (Cpp.Alive((UnityEngine.Object)(object)_host))
			{
				UnityEngine.Object.Destroy((UnityEngine.Object)(object)_host);
			}
		}
		catch
		{
		}
		_host = null;
		_source = null;
	}

	private static void EnsureHost()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		if (Cpp.Alive((UnityEngine.Object)(object)_host) && Cpp.Alive((UnityEngine.Object)(object)_source))
		{
			return;
		}
		try
		{
			_host = new GameObject("HouseAccess_Beacon");
			UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)_host);
			((UnityEngine.Object)_host).hideFlags = (HideFlags)61;
			_source = Cpp.AddComponent<AudioSource>(_host);
			if (!Cpp.Alive((UnityEngine.Object)(object)_source))
			{
				Log.Warn("Could not create beacon AudioSource.");
				return;
			}
			_source.playOnAwake = false;
			_source.loop = false;
			_source.spatialBlend = 1f;
			_source.rolloffMode = (AudioRolloffMode)1;
			_source.minDistance = 1f;
			_source.maxDistance = Mathf.Max(12f, Prefs.ScanRadius.Value * 1.5f);
			_source.dopplerLevel = 0f;
			_source.bypassEffects = true;
			_source.bypassReverbZones = true;
			_source.priority = 0;
			_ping = Tone("ha_ping", 0.055f, 880f, 0.35f);
			_arrive = Tone("ha_arrive", 0.13f, 523.25f, 0.3f);
		}
		catch (Exception e)
		{
			Log.Error("Beacon setup failed", e);
			_source = null;
		}
	}

	private static AudioClip Tone(string name, float seconds, float frequency, float amplitude)
	{
		int num = Mathf.Max(64, Mathf.RoundToInt(44100f * seconds));
		float[] array = new float[num];
		for (int i = 0; i < num; i++)
		{
			float num2 = (float)i / 44100f;
			float num3 = (float)i / (float)num;
			float num4 = Mathf.Min(1f, num3 / 0.06f);
			float num5 = Mathf.Exp(-5.5f * num3);
			array[i] = Mathf.Sin((float)Math.PI * 2f * frequency * num2) * amplitude * num4 * num5;
		}
		AudioClip val = AudioClip.Create(name, num, 1, 44100, false);
		// A managed float[] is not an Il2CppStructArray<float>; casting it throws
		// InvalidCastException and the beacon never makes a sound. Build the interop
		// array explicitly instead.
		Il2CppStructArray<float> data = new Il2CppStructArray<float>(num);
		for (int j = 0; j < num; j++)
		{
			data[j] = array[j];
		}
		val.SetData(data, 0);
		return val;
	}

	public static void Cue(bool positive)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		EnsureHost();
		if (!Cpp.Alive((UnityEngine.Object)(object)_source))
		{
			return;
		}
		try
		{
			_host.transform.position = GameRefs.EyePos;
			_source.pitch = (positive ? 1.6f : 0.8f);
			_source.volume = Mathf.Clamp01(Prefs.BeaconVolume.Value);
			_source.PlayOneShot(positive ? _ping : _arrive, _source.volume);
		}
		catch
		{
		}
	}

	public static void Tick()
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		if (!Enabled)
		{
			return;
		}
		if (!Cpp.Alive((UnityEngine.Object)(object)_source))
		{
			EnsureHost();
			if (!Cpp.Alive((UnityEngine.Object)(object)_source))
			{
				Enabled = false;
				return;
			}
		}
		Entry current = Radar.Current;
		if (current == null)
		{
			return;
		}
		Vector3 point = current.Point;
		float distance = current.Distance;
		try
		{
			_host.transform.position = point;
			if (!(Time.unscaledTime < _nextPing))
			{
				float num = Mathf.Max(3f, Prefs.ScanRadius.Value);
				float num2 = Mathf.Clamp01(distance / num);
				_nextPing = Time.unscaledTime + Mathf.Lerp(Mathf.Max(0.05f, Prefs.BeaconMinInterval.Value), Mathf.Max(0.2f, Prefs.BeaconMaxInterval.Value), num2);
				_source.pitch = Mathf.Lerp(1.5f, 0.7f, num2);
				_source.volume = Mathf.Clamp01(Prefs.BeaconVolume.Value);
				_source.PlayOneShot((distance <= Prefs.StopDistance.Value) ? _arrive : _ping, _source.volume);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Beacon tick failed: " + ex.Message);
			Enabled = false;
		}
	}
}
