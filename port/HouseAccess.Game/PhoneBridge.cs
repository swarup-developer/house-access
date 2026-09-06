using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Canvas;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class PhoneBridge
{
	private static readonly string[] ScreenNames = new string[8] { "Photos", "Messages", "Music", "Phone", "Mail", "Facebook", "Reddit", "Hearthstone" };

	private static int _appIndex = -1;

	private static bool _wasUp;

	private static bool _inGallery;

	private static MadisonPhone _phone;

	private static int _phoneFrame = -1;

	private static bool _wasViewing;

	private static MadisonPhone Phone
	{
		get
		{
			int frameCount = Time.frameCount;
			if (frameCount == _phoneFrame && Cpp.Alive((UnityEngine.Object)(object)_phone))
			{
				return _phone;
			}
			_phoneFrame = frameCount;
			try
			{
				_phone = Cpp.FindOne<MadisonPhone>(activeOnly: false);
			}
			catch
			{
				_phone = null;
			}
			return _phone;
		}
	}

	public static bool IsUp
	{
		get
		{
			try
			{
				Character p = ((Il2CppObjectBase)GameRefs.Player).TryCast<Character>();
				if (Cpp.Alive((UnityEngine.Object)(object)p) && Cpp.Read(() => p.IsOnPhone, fallback: false))
				{
					return true;
				}
			}
			catch
			{
			}

			MadisonPhone mp = Phone;
			if (!Cpp.Alive((UnityEngine.Object)(object)mp))
			{
				return false;
			}
			try
			{
				GameObject home = Cpp.Read(() => mp.IIEKHDEHAKL);
				if (Cpp.Alive((UnityEngine.Object)(object)home) && home.activeInHierarchy)
				{
					return true;
				}
				GameObject lockSc = Cpp.Read(() => mp.GFECLGMNFMO);
				if (Cpp.Alive((UnityEngine.Object)(object)lockSc) && lockSc.activeInHierarchy)
				{
					return true;
				}
				if (GalleryOpen)
				{
					return true;
				}
			}
			catch
			{
			}
			return false;
		}
	}

	public static bool Active => ViewerOpen || (IsUp && _appIndex >= 0);

	private static bool GalleryOpen
	{
		get
		{
			if (_inGallery)
			{
				return true;
			}
			MadisonPhone p = Phone;
			if (!Cpp.Alive((UnityEngine.Object)(object)p))
			{
				return false;
			}
			try
			{
				GameObject photos = Cpp.Read(() => p.FKOFHLHGFOD);
				return Cpp.Alive((UnityEngine.Object)(object)photos) && photos.activeInHierarchy;
			}
			catch
			{
				return false;
			}
		}
	}

	private static CameraViewController Viewer
	{
		get
		{
			try
			{
				return Cpp.FindOne<CameraViewController>(activeOnly: false);
			}
			catch
			{
				return null;
			}
		}
	}

	public static bool ViewerOpen
	{
		get
		{
			CameraViewController v = Viewer;
			if (!Cpp.Alive((UnityEngine.Object)(object)v))
			{
				return false;
			}
			try
			{
				Image val = Cpp.Read(() => v.DisplayImage);
				return Cpp.Alive((UnityEngine.Object)(object)val) && ((Component)val).gameObject.activeInHierarchy;
			}
			catch
			{
				return false;
			}
		}
	}

	public static void Reset()
	{
		_appIndex = -1;
		_wasUp = false;
		_inGallery = false;
		_wasViewing = false;
	}

	private static List<string> PhotoNames()
	{
		List<string> list = new List<string>();
		MadisonPhone p = Phone;
		if (!Cpp.Alive((UnityEngine.Object)(object)p))
		{
			return list;
		}
		try
		{
			Il2CppStringArray val = Cpp.Read(() => p.BCPOJONKFNK);
			if (val != null)
			{
				for (int i = 0; i < ((Il2CppArrayBase<string>)(object)val).Length; i++)
				{
					string text = ((Il2CppArrayBase<string>)(object)val)[i];
					if (!string.IsNullOrWhiteSpace(text))
					{
						list.Add(TextUtil.Humanize(text));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Could not read photo names: " + ex.Message);
		}
		return list;
	}

	public static void ReadCurrentPhoto()
	{
		MadisonPhone p = Phone;
		if (!Cpp.Alive((UnityEngine.Object)(object)p))
		{
			Speaker.SayNow("No phone here.");
			return;
		}
		List<string> list = PhotoNames();
		if (list.Count == 0)
		{
			Speaker.SayNow("No photos on this phone.");
			return;
		}
		int num = Cpp.Read(() => p.GJKJMMLFGFG, 0);
		if (num < 0 || num >= list.Count)
		{
			num = 0;
		}
		Speaker.Say($"{list[num]}. Photo {num + 1} of {list.Count}.", Pri.High);
	}

	public static void StepPhoto(int dir)
	{
		MadisonPhone p = Phone;
		if (!Cpp.Alive((UnityEngine.Object)(object)p))
		{
			return;
		}
		List<string> list = PhotoNames();
		if (list.Count == 0)
		{
			Speaker.SayNow("No photos on this phone.");
			return;
		}
		if (dir > 0)
		{
			try
			{
				p.NextPic();
			}
			catch (Exception ex)
			{
				Log.Warn("Could not page forward: " + ex.Message);
				return;
			}
			ReadCurrentPhoto();
			return;
		}
		int num = Cpp.Read(() => p.GJKJMMLFGFG, 0) - 1;
		if (num < 0)
		{
			num = list.Count - 1;
		}
		try
		{
			p.AABKNDCAJHH(num);
		}
		catch (Exception ex2)
		{
			Log.Warn("Could not show the photo: " + ex2.Message);
			return;
		}
		Speaker.Say($"{list[num]}. Photo {num + 1} of {list.Count}.", Pri.High);
	}

	private static void CloseAllScreens()
	{
		MadisonPhone phone = Phone;
		if (!Cpp.Alive((UnityEngine.Object)(object)phone))
		{
			return;
		}
		try
		{
			phone.ShowHomeScreen();
		}
		catch
		{
		}
	}

	private static string OpenScreenName()
	{
		return GalleryOpen ? "Photos" : null;
	}

	private static void OpenApp(int i)
	{
		MadisonPhone phone = Phone;
		if (!Cpp.Alive((UnityEngine.Object)(object)phone) || i < 0 || i >= ScreenNames.Length)
		{
			return;
		}
		string text = ScreenNames[i];
		CloseAllScreens();
		_inGallery = false;
		Speaker.SayNow(text + ".");
		try
		{
			string text2 = text.Replace(" ", string.Empty).ToLowerInvariant();
			if (text2.Contains("photo"))
			{
				phone.PhotosSelected();
			}
			else if (text2.Contains("message") || text2.Contains("text"))
			{
				phone.MessagesSelected();
			}
			else if (text2.Contains("music"))
			{
				phone.MusicSelected();
			}
			else if (text2.Contains("mail"))
			{
				phone.MailSelected();
			}
			else if (text2.Contains("face"))
			{
				phone.FacebookSelected();
			}
			else if (text2.Contains("reddit"))
			{
				phone.RedditSelected();
			}
			else if (text2.Contains("hearth"))
			{
				phone.HearthstoneSelected();
			}
			else if (text2.Contains("phone") || text2.Contains("call"))
			{
				phone.PhoneSelected();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Could not open " + text + ": " + ex.Message);
			Speaker.SayNow("That did not open.");
			return;
		}
		if (text.ToLowerInvariant().Contains("photo"))
		{
			_inGallery = true;
			ReadCurrentPhoto();
		}
	}

	private static void SpeakApp()
	{
		if (_appIndex >= 0 && _appIndex < ScreenNames.Length)
		{
			Speaker.Say($"{ScreenNames[_appIndex]}, {_appIndex + 1} of {ScreenNames.Length}.", Pri.High);
		}
	}

	public static string ScreenTime()
	{
		MadisonPhone p = Phone;
		if (!Cpp.Alive((UnityEngine.Object)(object)p))
		{
			return null;
		}
		try
		{
			foreach (TMP_Text t in ((Component)p).GetComponentsInChildren<TMP_Text>(true))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)t))
				{
					continue;
				}
				string text = Cpp.Read(() => t.text);
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string input = text.Trim();
				Match match = Regex.Match(input, "^(\\d{1,2})[:\\s](\\d{2})$");
				if (!match.Success)
				{
					continue;
				}
				int num = int.Parse(match.Groups[1].Value);
				int num2 = int.Parse(match.Groups[2].Value);
				if (num > 23 || num2 > 59)
				{
					continue;
				}
				string value = ((num < 12) ? "a m" : "p m");
				int num3 = num % 12;
				if (num3 == 0)
				{
					num3 = 12;
				}
				return (num2 == 0) ? $"{num3} {value}" : $"{num3} {num2:00} {value}";
			}
		}
		catch
		{
		}
		return null;
	}

	private static List<string> ViewerFiles()
	{
		List<string> list = new List<string>();
		CameraViewController v = Viewer;
		if (!Cpp.Alive((UnityEngine.Object)(object)v))
		{
			return list;
		}
		try
		{
			Il2CppSystem.Collections.Generic.List<string> val = Cpp.Read(() => v.OIMBEMFLDCO);
			int num = Cpp.CountOf<string>(val);
			for (int i = 0; i < num; i++)
			{
				string text = Cpp.AtOf<string>(val, i);
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(TextUtil.Humanize(text));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Could not read photograph files: " + ex.Message);
		}
		return list;
	}

	public static void ReadCurrentShot()
	{
		CameraViewController v = Viewer;
		if (!Cpp.Alive((UnityEngine.Object)(object)v))
		{
			return;
		}
		List<string> list = ViewerFiles();
		if (list.Count == 0)
		{
			Speaker.Say("No photographs taken yet.", Pri.High);
			return;
		}
		int num = Cpp.Read(() => v.EIAONIBGHEM, 0);
		if (num < 0 || num >= list.Count)
		{
			num = 0;
		}
		Speaker.Say($"{list[num]}. Photograph {num + 1} of {list.Count}.", Pri.High);
	}

	public static void StepShot(int dir)
	{
		CameraViewController v = Viewer;
		if (!Cpp.Alive((UnityEngine.Object)(object)v))
		{
			return;
		}
		List<string> list = ViewerFiles();
		if (list.Count == 0)
		{
			Speaker.Say("No photographs taken yet.", Pri.High);
			return;
		}
		int num = Cpp.Read(() => v.EIAONIBGHEM, 0) + dir;
		if (num < 0)
		{
			num = list.Count - 1;
		}
		if (num >= list.Count)
		{
			num = 0;
		}
		bool flag = false;
		try
		{
			flag = v.EGPGLHJCMEO(num);
		}
		catch (Exception ex)
		{
			Log.Warn("Could not load photograph: " + ex.Message);
			return;
		}
		Speaker.Say(flag ? $"{list[num]}. Photograph {num + 1} of {list.Count}." : (list[num] + " would not load."), Pri.High);
	}

	public static void CloseViewer()
	{
		CameraViewController viewer = Viewer;
		_wasViewing = false;
		try
		{
			if (Cpp.Alive((UnityEngine.Object)(object)viewer))
			{
				viewer.Toggle();
			}
			Speaker.SayNow("Photographs closed.");
		}
		catch (Exception ex)
		{
			Log.Warn("Could not close photo viewer: " + ex.Message);
			Speaker.SayNow("Could not close it. Press escape.");
		}
	}

	public static void Tick()
	{
		if (ViewerOpen)
		{
			if (!_wasViewing)
			{
				_wasViewing = true;
				Speaker.Say("Photographs. Arrows to look through them.", Pri.High);
				ReadCurrentShot();
			}
			else if (Keys.Hit(Prefs.KeyUiNext))
			{
				StepShot(1);
			}
			else if (Keys.Hit(Prefs.KeyUiPrev))
			{
				StepShot(-1);
			}
			else if (Keys.Hit(Prefs.KeyRepeatTarget))
			{
				ReadCurrentShot();
			}
			else if (Keys.Hit(Prefs.KeyCancel) || Input.GetKeyDown(KeyCode.Escape))
			{
				CloseViewer();
			}
			return;
		}

		_wasViewing = false;
		if (!IsUp)
		{
			if (_wasUp)
			{
				_wasUp = false;
				_appIndex = -1;
				_inGallery = false;
			}
			return;
		}

		if (!_wasUp)
		{
			_wasUp = true;
			_appIndex = 0;
			string time = ScreenTime();
			string timeStr = !string.IsNullOrEmpty(time) ? $" Time {time}." : string.Empty;
			Speaker.Say($"Phone.{timeStr} {ScreenNames.Length} apps. Arrows to choose, enter to open.", Pri.High);
			SpeakApp();
			return;
		}

		if (GalleryOpen)
		{
			if (Keys.Hit(Prefs.KeyUiNext) || Input.GetKeyDown(KeyCode.RightArrow))
			{
				StepPhoto(1);
			}
			else if (Keys.Hit(Prefs.KeyUiPrev) || Input.GetKeyDown(KeyCode.LeftArrow))
			{
				StepPhoto(-1);
			}
			else if (Keys.Hit(Prefs.KeyRepeatTarget))
			{
				ReadCurrentPhoto();
			}
			else if (Keys.Hit(Prefs.KeyCancel) || Input.GetKeyDown(KeyCode.Escape))
			{
				CloseAllScreens();
				_inGallery = false;
				Speaker.SayNow("Back to the home screen.");
			}
			return;
		}

		if (Keys.Hit(Prefs.KeyCancel) || Input.GetKeyDown(KeyCode.Escape))
		{
			string text = OpenScreenName();
			if (!string.IsNullOrWhiteSpace(text))
			{
				CloseAllScreens();
				try
				{
					MadisonPhone p = Phone;
					if (Cpp.Alive((UnityEngine.Object)(object)p))
					{
						p.ShowHomeScreen();
					}
				}
				catch
				{
				}
				Speaker.SayNow("Closed " + text + ".");
				return;
			}
		}

		if (Keys.Hit(Prefs.KeyUiNext) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow))
		{
			_appIndex = (_appIndex + 1) % ScreenNames.Length;
			SpeakApp();
			return;
		}
		if (Keys.Hit(Prefs.KeyUiPrev) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow))
		{
			_appIndex = (_appIndex - 1 + ScreenNames.Length) % ScreenNames.Length;
			SpeakApp();
			return;
		}
		if (Keys.Hit(Prefs.KeyUiActivate) || Input.GetKeyDown(KeyCode.Return))
		{
			OpenApp(_appIndex);
			return;
		}
		if (Keys.Hit(Prefs.KeyRepeatTarget))
		{
			SpeakApp();
			return;
		}

		for (int i = 0; i < Math.Min(9, ScreenNames.Length); i++)
		{
			if (Input.GetKeyDown((KeyCode)(KeyCode.Alpha1 + i)) || Input.GetKeyDown((KeyCode)(KeyCode.Keypad1 + i)))
			{
				if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
				{
					OpenApp(i);
					break;
				}
				_appIndex = i;
				SpeakApp();
				break;
			}
		}
	}
}
