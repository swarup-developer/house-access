using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HouseAccess.Game;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.UI;
using HouseAccess.Util;
using HouseAccess.World;
using EekCharacterEngine;
using EekCharacterEngine.Canvas;
using EekCharacterEngine.Interaction;
using EekEvents.Values;
using HouseParty;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Speaker = HouseAccess.Speech.Speaker;

namespace HouseAccess;

public static class Diagnostics
{
	public static string Directory
	{
		get
		{
			string path = AppDomain.CurrentDomain.BaseDirectory ?? System.IO.Directory.GetCurrentDirectory();
			return Path.Combine(path, "UserData", "HouseAccess");
		}
	}

	private static void DumpFocusedControl(StringBuilder sb)
	{
		sb.AppendLine("Focused control:");
		try
		{
			EventSystem es = EventSystem.current;
			if (!Cpp.Alive((UnityEngine.Object)(object)es))
			{
				sb.AppendLine("  (no EventSystem)");
				sb.AppendLine();
				return;
			}
			GameObject val = Cpp.Read(() => es.currentSelectedGameObject);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				sb.AppendLine("  (nothing selected)");
				sb.AppendLine();
				return;
			}
			StringBuilder stringBuilder = sb;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder);
			handler.AppendLiteral("  object   : ");
			handler.AppendFormatted(((UnityEngine.Object)val).name);
			stringBuilder2.AppendLine(ref handler);
			stringBuilder = sb;
			StringBuilder stringBuilder3 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder);
			handler.AppendLiteral("  path     : ");
			handler.AppendFormatted(PathOf(val.transform));
			stringBuilder3.AppendLine(ref handler);
			stringBuilder = sb;
			StringBuilder stringBuilder4 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder);
			handler.AppendLiteral("  spoken as: ");
			handler.AppendFormatted(MenuReader.LabelFor(val));
			stringBuilder4.AppendLine(ref handler);
			sb.AppendLine("  components:");
			Il2CppArrayBase<Component> components = val.GetComponents<Component>();
			if (components != null)
			{
				foreach (Component item in components)
				{
					if (!((UnityEngine.Object)(object)item == (UnityEngine.Object)null))
					{
						string text = Cpp.ClassName((Il2CppObjectBase)(object)item);
						sb.AppendLine("    " + (string.IsNullOrEmpty(text) ? "(unknown)" : text));
					}
				}
			}
			sb.AppendLine("  text beneath it (depth, value):");
			DumpTexts(sb, val);
			Transform parent = val.transform.parent;
			if ((UnityEngine.Object)(object)parent != (UnityEngine.Object)null)
			{
				stringBuilder = sb;
				StringBuilder stringBuilder5 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder);
				handler.AppendLiteral("  parent   : ");
				handler.AppendFormatted(((UnityEngine.Object)parent).name);
				stringBuilder5.AppendLine(ref handler);
				sb.AppendLine("  text beneath the parent:");
				DumpTexts(sb, ((Component)parent).gameObject);
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine("  (failed: " + ex.Message + ")");
		}
		sb.AppendLine();
	}

	private static void DumpTexts(StringBuilder sb, GameObject root)
	{
		try
		{
			Il2CppArrayBase<Text> componentsInChildren = root.GetComponentsInChildren<Text>(true);
			if (componentsInChildren != null)
			{
				foreach (Text t in componentsInChildren)
				{
					if ((UnityEngine.Object)(object)t == (UnityEngine.Object)null)
					{
						continue;
					}
					string text = Cpp.Read(() => t.text);
					if (!string.IsNullOrWhiteSpace(text))
					{
						bool flag = Cpp.Read(() => ((Component)t).gameObject.activeInHierarchy, fallback: false);
						StringBuilder stringBuilder = sb;
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 3, stringBuilder);
						handler.AppendLiteral("    [");
						handler.AppendFormatted(Depth(root.transform, ((Component)t).transform));
						handler.AppendLiteral("]");
						handler.AppendFormatted(flag ? "" : " (inactive)");
						handler.AppendLiteral(" Text: ");
						handler.AppendFormatted(Trim(text));
						stringBuilder2.AppendLine(ref handler);
					}
				}
			}
			Il2CppArrayBase<TMP_Text> componentsInChildren2 = root.GetComponentsInChildren<TMP_Text>(true);
			if (componentsInChildren2 == null)
			{
				return;
			}
			foreach (TMP_Text t2 in componentsInChildren2)
			{
				if ((UnityEngine.Object)(object)t2 == (UnityEngine.Object)null)
				{
					continue;
				}
				string text2 = Cpp.Read(() => t2.text);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					bool flag2 = Cpp.Read(() => ((Component)t2).gameObject.activeInHierarchy, fallback: false);
					StringBuilder stringBuilder = sb;
					StringBuilder stringBuilder3 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 3, stringBuilder);
					handler.AppendLiteral("    [");
					handler.AppendFormatted(Depth(root.transform, t2.transform));
					handler.AppendLiteral("]");
					handler.AppendFormatted(flag2 ? "" : " (inactive)");
					handler.AppendLiteral(" TMP: ");
					handler.AppendFormatted(Trim(text2));
					stringBuilder3.AppendLine(ref handler);
				}
			}
		}
		catch
		{
			sb.AppendLine("    (text scan failed)");
		}
	}

	private static string Trim(string v)
	{
		v = v.Replace("\n", " ").Replace("\r", " ").Trim();
		return (v.Length > 90) ? (v.Substring(0, 90) + "...") : v;
	}

	private static int Depth(Transform root, Transform child)
	{
		int num = 0;
		Transform val = child;
		while ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && num < 32)
		{
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)(object)root)
			{
				return num;
			}
			val = val.parent;
			num++;
		}
		return -1;
	}

	private static string PathOf(Transform t)
	{
		List<string> list = new List<string>();
		int num = 0;
		while ((UnityEngine.Object)(object)t != (UnityEngine.Object)null && num++ < 12)
		{
			list.Insert(0, ((UnityEngine.Object)t).name);
			t = t.parent;
		}
		return string.Join("/", list);
	}

	public static void Dump()
	{
		//IL_0b0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1490: Unknown result type (might be due to invalid IL or missing references)
		//IL_1495: Unknown result type (might be due to invalid IL or missing references)
		//IL_149b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0924: Unknown result type (might be due to invalid IL or missing references)
		//IL_0930: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0791: Unknown result type (might be due to invalid IL or missing references)
		//IL_0796: Unknown result type (might be due to invalid IL or missing references)
		//IL_073d: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a82: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c45: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c54: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c75: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c84: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c89: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d02: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d23: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d67: Unknown result type (might be due to invalid IL or missing references)
		//IL_129d: Unknown result type (might be due to invalid IL or missing references)
		//IL_12a4: Unknown result type (might be due to invalid IL or missing references)
		string text;
		try
		{
			System.IO.Directory.CreateDirectory(Directory);
			text = Path.Combine(Directory, $"dump_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
		}
		catch (Exception e)
		{
			Log.Error("Could not create dump directory", e);
			Speaker.SayNow("Could not write diagnostics.");
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		try
		{
			stringBuilder.AppendLine("House Access diagnostics");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Time           : ");
			handler.AppendFormatted(DateTime.Now, "u");
			stringBuilder3.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Unity          : ");
			handler.AppendFormatted(Application.unityVersion);
			stringBuilder4.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Speech backend : ");
			handler.AppendFormatted(Speaker.BackendName);
			stringBuilder5.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Input backend  : ");
			handler.AppendFormatted(Keys.ActiveBackend);
			stringBuilder6.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(35, 2, stringBuilder2);
			handler.AppendLiteral("Harmony        : ");
			handler.AppendFormatted(Patches.Applied);
			handler.AppendLiteral(" applied, ");
			handler.AppendFormatted(Patches.Failed);
			handler.AppendLiteral(" skipped");
			stringBuilder7.AppendLine(ref handler);
			stringBuilder.AppendLine();
			PlayerCharacter p = GameRefs.Player;
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Player found   : ");
			handler.AppendFormatted((UnityEngine.Object)(object)p != (UnityEngine.Object)null);
			stringBuilder8.AppendLine(ref handler);
			if ((UnityEngine.Object)(object)p != (UnityEngine.Object)null)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder9 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  position     : ");
				handler.AppendFormatted<Vector3>(GameRefs.FeetPos);
				stringBuilder9.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder10 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  root position: ");
				handler.AppendFormatted(((UnityEngine.Object)(object)GameRefs.PlayerTransform != (UnityEngine.Object)null) ? ((object)GameRefs.PlayerTransform.position/*cast due to .constrained prefix*/).ToString() : "(none)");
				stringBuilder10.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder11 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  moving xform : ");
				handler.AppendFormatted(((UnityEngine.Object)(object)GameRefs.MovingTransform != (UnityEngine.Object)null) ? ((UnityEngine.Object)GameRefs.MovingTransform).name : "(none)");
				stringBuilder11.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder12 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
				handler.AppendLiteral("  body facing  : ");
				handler.AppendFormatted(((UnityEngine.Object)(object)GameRefs.MovingTransform != (UnityEngine.Object)null) ? GameRefs.MovingTransform.eulerAngles.y.ToString("0") : "?");
				handler.AppendLiteral(" degrees");
				stringBuilder12.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder13 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
				handler.AppendLiteral("  view facing  : ");
				handler.AppendFormatted(Cpp.Alive((UnityEngine.Object)(object)GameRefs.Cam) ? ((Component)GameRefs.Cam).transform.eulerAngles.y.ToString("0") : "?");
				handler.AppendLiteral(" degrees");
				stringBuilder13.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder14 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  zone         : ");
				handler.AppendFormatted(GameRefs.CurrentZoneName() ?? "(none)");
				stringBuilder14.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder15 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  inside house : ");
				handler.AppendFormatted(GameRefs.InsideHouse);
				stringBuilder15.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder16 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  movement lock: ");
				handler.AppendFormatted(GameRefs.MovementLocked);
				stringBuilder16.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder17 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  move mode    : ");
				handler.AppendFormatted(Prefs.MoveMode.Value);
				stringBuilder17.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder18 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  walking      : ");
				handler.AppendFormatted(Navigator.IsWalking);
				stringBuilder18.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder19 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  is warping   : ");
				handler.AppendFormatted(Cpp.Read(() => ((Character)p).IsWarping, fallback: false));
				stringBuilder19.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder20 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  controller   : ");
				handler.AppendFormatted(Cpp.Alive((UnityEngine.Object)(object)GameRefs.Controller));
				stringBuilder20.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder21 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  max energy   : ");
				handler.AppendFormatted(Cpp.Read(() => PlayerCharacter.MaxEnergyPoints, -1f));
				stringBuilder21.AppendLine(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder22 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("  bladder      : ");
				handler.AppendFormatted("n/a");
				stringBuilder22.AppendLine(ref handler);
			}
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder23 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Camera found   : ");
			handler.AppendFormatted(Cpp.Alive((UnityEngine.Object)(object)GameRefs.Cam));
			stringBuilder23.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder24 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("In game        : ");
			handler.AppendFormatted(GameRefs.InGame);
			stringBuilder24.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder25 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Can move       : ");
			handler.AppendFormatted(GameRefs.CanMove);
			stringBuilder25.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder26 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Player root    : ");
			handler.AppendFormatted(((UnityEngine.Object)(object)GameRefs.PlayerTransform != (UnityEngine.Object)null) ? ((UnityEngine.Object)GameRefs.PlayerTransform).name : "(none)");
			stringBuilder26.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder27 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("Active scene   : ");
			Scene activeScene = SceneManager.GetActiveScene();
			handler.AppendFormatted(activeScene.name);
			stringBuilder27.AppendLine(ref handler);
			stringBuilder.AppendLine();
			DumpFocusedControl(stringBuilder);
			stringBuilder.AppendLine("Movable props: " + GameRefs.PropCensus());
			stringBuilder.AppendLine("Nearby items and the room each is attributed to:");
			try
			{
				GameRefs.Room room = GameRefs.RoomOwning(GameRefs.FeetPos);
				int num = 0;
				foreach (InteractiveItem item in GameRefs.Items())
				{
					if (Cpp.Alive((UnityEngine.Object)(object)item) && num < 12)
					{
						Vector3 position;
						try
						{
							position = ((Component)item).transform.position;
						}
						catch
						{
							continue;
						}
						float num2 = Vector3.Distance(GameRefs.FeetPos, position);
						if (!(num2 > 8f))
						{
							GameRefs.Room room2 = GameRefs.RoomOwning(position);
							string value = ((room2 != null) ? room2.Name : "(no room)");
							bool flag = ((room2 == null) ? (num2 <= 6f) : (room != null && string.Equals(room2.Name, room.Name, StringComparison.OrdinalIgnoreCase)));
							stringBuilder2 = stringBuilder;
							StringBuilder stringBuilder28 = stringBuilder2;
							handler = new StringBuilder.AppendInterpolatedStringHandler(15, 4, stringBuilder2);
							handler.AppendLiteral("  ");
							handler.AppendFormatted(GameRefs.NameOf(item));
							handler.AppendLiteral("  ");
							handler.AppendFormatted(num2, "0.0");
							handler.AppendLiteral("m  owner=");
							handler.AppendFormatted(value);
							handler.AppendLiteral("  ");
							handler.AppendFormatted(flag ? "kept" : "REJECTED");
							stringBuilder28.AppendLine(ref handler);
							num++;
						}
					}
				}
			}
			catch (Exception ex)
			{
				stringBuilder.AppendLine("  (failed: " + ex.Message + ")");
			}
			stringBuilder.AppendLine();
			foreach (DistractableRigidItem item2 in GameRefs.Props())
			{
				string value2 = GameRefs.NameOf(item2) ?? "(unnamed)";
				float value3 = 999f;
				try
				{
					value3 = Vector3.Distance(GameRefs.FeetPos, ((Component)item2).transform.position);
				}
				catch
				{
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder29 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(value2);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(value3, "0.0");
				handler.AppendLiteral("m");
				stringBuilder29.AppendLine(ref handler);
			}
			stringBuilder.AppendLine("Physics objects within 3m:");
			try
			{
				foreach (Collider item3 in (Il2CppArrayBase<Collider>)(object)Physics.OverlapSphere(GameRefs.FeetPos, 3f))
				{
					if (!((UnityEngine.Object)(object)item3 == (UnityEngine.Object)null))
					{
						Rigidbody attachedRigidbody = item3.attachedRigidbody;
						if (Cpp.Alive((UnityEngine.Object)(object)attachedRigidbody) && !GameRefs.IsPlayerPart(((Component)item3).transform))
						{
							stringBuilder2 = stringBuilder;
							StringBuilder stringBuilder30 = stringBuilder2;
							handler = new StringBuilder.AppendInterpolatedStringHandler(16, 3, stringBuilder2);
							handler.AppendLiteral("  ");
							handler.AppendFormatted(((UnityEngine.Object)((Component)attachedRigidbody).gameObject).name);
							handler.AppendLiteral("  ");
							handler.AppendFormatted(Vector3.Distance(GameRefs.FeetPos, ((Component)attachedRigidbody).transform.position), "0.0");
							handler.AppendLiteral("m ");
							handler.AppendLiteral("kinematic=");
							handler.AppendFormatted(attachedRigidbody.isKinematic);
							stringBuilder30.AppendLine(ref handler);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				stringBuilder.AppendLine("  (scan failed: " + ex2.Message + ")");
			}
			GameRefs.Room room3 = GameRefs.RoomOwning(GameRefs.FeetPos);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder31 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(30, 2, stringBuilder2);
			handler.AppendLiteral("Room owning you: ");
			handler.AppendFormatted((room3 != null) ? room3.Name : "(none)");
			handler.AppendLiteral(" ");
			handler.AppendLiteral("(game says ");
			handler.AppendFormatted(GameRefs.CurrentZoneName() ?? "(none)");
			handler.AppendLiteral(")");
			stringBuilder31.AppendLine(ref handler);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Key bindings (name, configured text, parsed result):");
			foreach (string item4 in Prefs.Bindings())
			{
				stringBuilder.AppendLine("  " + item4);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Rooms (volumes, doorway spots, nearest distance):");
			foreach (GameRefs.Room item5 in GameRefs.Rooms())
			{
				Vector3 val = default(Vector3);
				foreach (Bounds volume in item5.Volumes)
				{
					Bounds current6 = volume;
					val = new Vector3(Mathf.Max(val.x, current6.size.x), Mathf.Max(val.y, current6.size.y), Mathf.Max(val.z, current6.size.z));
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder32 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(31, 6, stringBuilder2);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(item5.Name);
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item5.Volumes.Count);
				handler.AppendLiteral(" vol, largest ");
				handler.AppendFormatted(val.x, "0.#");
				handler.AppendLiteral("x");
				handler.AppendFormatted(val.z, "0.#");
				handler.AppendLiteral("m, ");
				handler.AppendFormatted(item5.Anchors.Count);
				handler.AppendLiteral(" spots, ");
				handler.AppendFormatted(item5.DistanceFrom(GameRefs.FeetPos), "0.0");
				handler.AppendLiteral("m");
				stringBuilder32.AppendLine(ref handler);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Status bars on screen:");
			foreach (string item6 in StatusBars.Read())
			{
				stringBuilder.AppendLine("  " + item6);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Open canvases:");
			foreach (CanvasBase c in GameRefs.OpenCanvases())
			{
				string value4 = Cpp.Read(() => ((UnityEngine.Object)((Component)c).gameObject).name, "?");
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder33 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(10, 2, stringBuilder2);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(value4);
				handler.AppendLiteral("  modal=");
				handler.AppendFormatted(Cpp.Read(() => c.Modal, fallback: false));
				stringBuilder33.AppendLine(ref handler);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Input owners (any of these true will quiet some hotkeys):");
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder34 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  dialogue choices : ");
			handler.AppendFormatted(DialogueBridge.HasChoices);
			stringBuilder34.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder35 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  radial menu      : ");
			handler.AppendFormatted(RadialBridge.Active);
			stringBuilder35.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder36 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  action picker    : ");
			handler.AppendFormatted(ActionPicker.Active);
			stringBuilder36.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder37 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  game wheel       : ");
			handler.AppendFormatted(WheelBridge.Active);
			stringBuilder37.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder38 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  phone up         : ");
			handler.AppendFormatted(PhoneBridge.IsUp);
			stringBuilder38.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder39 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  opportunities    : ");
			handler.AppendFormatted(OpportunityBridge.Active);
			stringBuilder39.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder40 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  inventory        : ");
			handler.AppendFormatted(InventoryBridge.Active);
			stringBuilder40.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder41 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  customizer       : ");
			handler.AppendFormatted(CustomizeBridge.Active);
			stringBuilder41.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder42 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  menu on screen   : ");
			handler.AppendFormatted(MenuReader.Active);
			stringBuilder42.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder43 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  cursor visible   : ");
			handler.AppendFormatted(Cursor.visible);
			stringBuilder43.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder44 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  customizer found : ");
			handler.AppendFormatted(false);   // no character customizer on this build
			stringBuilder44.AppendLine(ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder45 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
			handler.AppendLiteral("  click managers   : ");
			handler.AppendFormatted(0);   // no click managers on this build
			stringBuilder45.AppendLine(ref handler);
			stringBuilder.AppendLine();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder46 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(64, 5, stringBuilder2);
			handler.AppendLiteral("UI singletons: dialogue=");
			handler.AppendFormatted((UnityEngine.Object)(object)GameRefs.Dialogue != (UnityEngine.Object)null);
			handler.AppendLiteral(" inventory=");
			handler.AppendFormatted((UnityEngine.Object)(object)GameRefs.Inventory != (UnityEngine.Object)null);
			handler.AppendLiteral(" ");
			handler.AppendLiteral("radial=");
			handler.AppendFormatted((UnityEngine.Object)(object)GameRefs.Radial != (UnityEngine.Object)null);
			handler.AppendLiteral(" useSelect=");
			handler.AppendFormatted((UnityEngine.Object)(object)GameRefs.UseSelect != (UnityEngine.Object)null);
			handler.AppendLiteral(" narrator=");
			handler.AppendFormatted((UnityEngine.Object)(object)GameRefs.Narrator != (UnityEngine.Object)null);
			stringBuilder46.AppendLine(ref handler);
			stringBuilder.AppendLine();
			List<Character> list = GameRefs.Npcs();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder47 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("NPCs loaded (");
			handler.AppendFormatted(list.Count);
			handler.AppendLiteral("):");
			stringBuilder47.AppendLine(ref handler);
			foreach (Character item7 in list)
			{
				string value5 = GameRefs.NameOf(item7) ?? "(unnamed)";
				float value6 = Vector3.Distance(GameRefs.EyePos, GameRefs.AimPointOf(item7));
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder48 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(value5);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(value6, "0.0");
				handler.AppendLiteral("m");
				stringBuilder48.AppendLine(ref handler);
			}
			stringBuilder.AppendLine();
			List<Entry> list2 = Radar.PeopleNearby();
			if (list2.Count > 0)
			{
				list2.Sort((Entry a, Entry b) => a.Distance.CompareTo(b.Distance));
				string text2 = GameRefs.NameOf(list2[0].Person);
				PlayerCharacter player = GameRefs.Player;
				string text3 = GameRefs.NameOf((player != null) ? ((Il2CppObjectBase)player).TryCast<Character>() : null);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder49 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(33, 2, stringBuilder2);
				handler.AppendLiteral("Relationship values for ");
				handler.AppendFormatted(text2);
				handler.AppendLiteral(" toward ");
				handler.AppendFormatted(text3);
				handler.AppendLiteral(":");
				stringBuilder49.AppendLine(ref handler);
				if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text3))
				{
					string[] array = new string[5] { "Friendship", "Romance", "Fear", "Trust", "Offense" };
					foreach (string text4 in array)
					{
						string value7 = "error";
						string value8 = "error";
						try
						{
							value7 = "n/a";
						}
						catch
						{
						}
						try
						{
							value8 = "n/a";
							value8 = "n/a";
						}
						catch
						{
						}
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder50 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(18, 3, stringBuilder2);
						handler.AppendLiteral("  ");
						handler.AppendFormatted(text4);
						handler.AppendLiteral(": named=");
						handler.AppendFormatted(value7);
						handler.AppendLiteral(" legacy=");
						handler.AppendFormatted(value8);
						stringBuilder50.AppendLine(ref handler);
					}
				}
				stringBuilder.AppendLine();
			}
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder51 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(47, 4, stringBuilder2);
			handler.AppendLiteral("Radar entries (");
			handler.AppendFormatted(Radar.All.Count);
			handler.AppendLiteral(" kept, cap ");
			handler.AppendFormatted(Prefs.MaxTargets.Value);
			handler.AppendLiteral(", radius ");
			handler.AppendFormatted(Prefs.ScanRadius.Value);
			handler.AppendLiteral("m, filter ");
			handler.AppendFormatted(Radar.Mode);
			handler.AppendLiteral("):");
			stringBuilder51.AppendLine(ref handler);
			foreach (Entry item8 in Radar.All)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder52 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(24, 5, stringBuilder2);
				handler.AppendLiteral("  [");
				handler.AppendFormatted(item8.Kind);
				handler.AppendLiteral("] ");
				handler.AppendFormatted(item8.Label);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(item8.Distance, "0.0");
				handler.AppendLiteral("m  clock ");
				handler.AppendFormatted(item8.Clock);
				handler.AppendLiteral("  reach=");
				handler.AppendFormatted(item8.InReach);
				stringBuilder52.AppendLine(ref handler);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Nearest interactive items and their verbs:");
			int num4 = 0;
			foreach (InteractiveItem item9 in GameRefs.Items())
			{
				if (num4++ > 40)
				{
					break;
				}
				string value9 = GameRefs.NameOf(item9) ?? "(unnamed)";
				List<string> list3 = new List<string>();
				foreach (GameRefs.Verb item10 in GameRefs.VerbsOf(item9))
				{
					list3.Add(item10.Available ? item10.Name : (item10.Name + "(no)"));
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder53 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder2);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(value9);
				handler.AppendLiteral(": ");
				handler.AppendFormatted((list3.Count == 0) ? "(none)" : string.Join(" | ", list3));
				stringBuilder53.AppendLine(ref handler);
			}
			File.WriteAllText(text, stringBuilder.ToString());
			Log.Info("Diagnostics written to " + text);
			Speaker.SayNow("Diagnostics written to user data, House Access folder.");
		}
		catch (Exception e2)
		{
			Log.Error("Diagnostics dump failed", e2);
			Speaker.SayNow("Diagnostics failed. See the MelonLoader log.");
		}
	}
}
