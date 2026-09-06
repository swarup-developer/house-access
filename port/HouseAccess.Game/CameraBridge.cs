using System;
using HouseAccess.Speech;
using HouseAccess.Util;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class CameraBridge
{
	private static bool _viewing;

	private static int _shown;

	private static DigitalCamera Cam => Cpp.FindOne<DigitalCamera>(activeOnly: false);

	private static CameraController Controller => Cpp.FindOne<CameraController>(activeOnly: false);

	public static int Count
	{
		get
		{
			CameraController c = Controller;
			if (!Cpp.Alive((UnityEngine.Object)(object)c))
			{
				return 0;
			}
			try
			{
				// Photo count lives in a stripped member on this build; the camera's own
				// counter property is the only remaining candidate.
				return Cpp.Read(() => c._OOPHPDAKCJF_k__BackingField, 0);
			}
			catch
			{
				return 0;
			}
		}
	}

	public static bool Held
	{
		get
		{
			DigitalCamera cam = Cam;
			if (!Cpp.Alive((UnityEngine.Object)(object)cam))
			{
				return false;
			}
			try
			{
				// The camera is treated as held while its object is active in the world.
				return ((Component)cam).gameObject.activeInHierarchy;
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool Viewing => _viewing;

	public static void ToggleView()
	{
		DigitalCamera cam = Cam;
		if (!Cpp.Alive((UnityEngine.Object)(object)cam))
		{
			Speaker.SayNow("No camera in hand.");
			return;
		}
		int count = Count;
		if (_viewing || PhoneBridge.ViewerOpen)
		{
			try
			{
				cam.ShowCameraCrossHairs();
			}
			catch (Exception ex)
			{
				Log.Warn("Could not return to the viewfinder: " + ex.Message);
				return;
			}
			_viewing = false;
			Speaker.SayNow("Viewfinder. Ready to take a photo.");
			return;
		}
		if (count == 0)
		{
			Speaker.SayNow("No photos taken yet.");
			return;
		}
		try
		{
			cam.ShowPhotosTaken();
		}
		catch (Exception ex2)
		{
			Log.Warn("Could not show the photos: " + ex2.Message);
			return;
		}
		_viewing = true;
		_shown = 1;
		Speaker.SayNow("Viewing photos. " + TextUtil.Pluralise(count, "photo", "photos") + ". Enter for the next one.");
	}

	public static void Reset()
	{
		_viewing = false;
		_shown = 0;
	}

	public static void NextPhoto()
	{
		if (!_viewing)
		{
			Speaker.SayNow("Not viewing photos.");
			return;
		}
		int count = Count;
		if (count == 0)
		{
			Speaker.SayNow("No photos.");
			return;
		}
		bool flag = false;
		try
		{
			DigitalCamera cam = Cam;
			if (Cpp.Alive((UnityEngine.Object)(object)cam))
			{
				Il2CppArrayBase<Button> componentsInChildren = ((Component)cam).GetComponentsInChildren<Button>(false);
				if (componentsInChildren != null)
				{
					foreach (Button b in componentsInChildren)
					{
						if (!Cpp.Alive((UnityEngine.Object)(object)b) || !Cpp.Read(() => ((UIBehaviour)b).IsActive(), fallback: false))
						{
							continue;
						}
						((UnityEvent)b.onClick).Invoke();
						flag = true;
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Could not page the photos: " + ex.Message);
		}
		_shown++;
		if (_shown > count)
		{
			_shown = 1;
		}
		Speaker.Say(flag ? $"Photo {_shown} of {count}." : $"Photo {_shown} of {count}. The screen may not have changed.", Pri.High);
	}

	public static void Tick()
	{
		if (!Held && _viewing)
		{
			Reset();
		}
	}
}
