using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Canvas;
using EekCharacterEngine.Components;
using EekCharacterEngine.Interaction;
using EekCharacterEngine.Motion;
using EekEvents.Characters;
using EekEvents.Dialogues;
using EekEvents.Items;
using EekEvents.Stories;
using EekUI;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HouseAccess.Game;

public static class GameRefs
{
	public enum Shot
	{
		Face,
		Chest,
		Full
	}

	public sealed class Room
	{
		public string Name;

		public Zone Zone;

		public readonly List<Bounds> Volumes = new List<Bounds>();

		public readonly List<Collider> Shapes = new List<Collider>();

		public readonly List<Vector3> Anchors = new List<Vector3>();

		private Vector3 _standingSpot;

		private bool _spotKnown;

		public bool Contains(Vector3 p)
		{
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			foreach (Collider shape in Shapes)
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)shape))
				{
					continue;
				}
				try
				{
					Vector3 val = shape.ClosestPoint(p) - p;
					if (val.sqrMagnitude < 0.0001f)
					{
						return true;
					}
				}
				catch
				{
					Bounds bounds = shape.bounds;
					if (bounds.Contains(p))
					{
						return true;
					}
				}
			}
			if (Shapes.Count > 0)
			{
				return false;
			}
			foreach (Bounds volume in Volumes)
			{
				Bounds current2 = volume;
				if (current2.Contains(p))
				{
					return true;
				}
			}
			return false;
		}

		public Vector3 FloorNearest(Vector3 from)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			if (_spotKnown)
			{
				return _standingSpot;
			}
			_spotKnown = true;
			_standingSpot = Middle();
			try
			{
				NavMeshHit val = default(NavMeshHit);
				if (NavMesh.SamplePosition(_standingSpot, out val, 4f, -1) && Mathf.Abs(val.position.y - _standingSpot.y) < 1.5f && (Contains(val.position) || Volumes.Count == 0))
				{
					_standingSpot = val.position;
				}
			}
			catch
			{
			}
			return _standingSpot;
		}

		private Vector3 Middle()
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			Bounds val = default(Bounds);
			float num = -1f;
			bool flag = false;
			foreach (Bounds volume in Volumes)
			{
				Bounds current = volume;
				float num2 = current.size.x * current.size.z;
				if (!(num2 <= num))
				{
					num = num2;
					val = current;
					flag = true;
				}
			}
			if (flag)
			{
				return new Vector3(val.center.x, val.min.y + 0.2f, val.center.z);
			}
			if (Anchors.Count > 0)
			{
				return Anchors[0];
			}
			return Vector3.zero;
		}

		public float DistanceFrom(Vector3 from)
		{
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			float num = float.MaxValue;
			if (Anchors.Count > 0)
			{
				foreach (Vector3 anchor in Anchors)
				{
					num = Mathf.Min(num, Vector3.Distance(from, anchor));
				}
				return num;
			}
			foreach (Bounds volume in Volumes)
			{
				Bounds current2 = volume;
				num = Mathf.Min(num, Vector3.Distance(from, current2.ClosestPoint(from)));
			}
			return (num == float.MaxValue) ? Vector3.Distance(from, Middle()) : num;
		}
	}

	public sealed class Verb
	{
		public string Name;

		public bool Available;
	}

	private static PlayerCharacter _player;

	private static Camera _camera;

	private static DialogueUI _dialogue;

	private static InventoryUI _inventory;

	private static UIRadialMenu _radial;

	private static UseSelectUI _useSelect;

	private static NarratorManager _narrator;

	private static readonly Dictionary<Type, float> NextScan = new Dictionary<Type, float>();

	private static float _sceneLoadedAt;

	private static readonly List<Character> NpcCache = new List<Character>();

	private static float _npcCacheAt;

	private static readonly List<Room> RoomCache = new List<Room>();

	private static float _roomsBuiltAt;

	private static readonly Dictionary<long, string> OwnerCache = new Dictionary<long, string>();

	private static float _ownerCacheAt;

	private static readonly Dictionary<int, string> LabelCache = new Dictionary<int, string>();

	private static RadialMenu _labelMenu;

	public static PlayerCharacter Player
	{
		get
		{
			PlayerCharacter val = Resolve<PlayerCharacter>(ref _player);
			if (Cpp.Alive((UnityEngine.Object)(object)val))
			{
				return val;
			}
			foreach (Character c in Cpp.FindAll<Character>(activeOnly: false))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)c))
				{
					continue;
				}
				PlayerCharacter val2 = ((Il2CppObjectBase)c).TryCast<PlayerCharacter>();
				if (Cpp.Alive((UnityEngine.Object)(object)val2))
				{
					_player = val2;
					return _player;
				}
			}
			return null;
		}
	}

	public static Camera Cam
	{
		get
		{
			if (Cpp.Alive((UnityEngine.Object)(object)_camera) && Cpp.Read(() => ((Behaviour)_camera).isActiveAndEnabled, fallback: false))
			{
				return _camera;
			}
			try
			{
				Camera main = Camera.main;
				if (Cpp.Alive((UnityEngine.Object)(object)main) && ((Behaviour)main).isActiveAndEnabled)
				{
					_camera = main;
					return _camera;
				}
			}
			catch
			{
			}
			Camera camera = null;
			float num = float.NegativeInfinity;
			foreach (Camera c in Cpp.FindAll<Camera>(activeOnly: false))
			{
				if (Cpp.Read(() => ((Behaviour)c).isActiveAndEnabled, fallback: false) && !((UnityEngine.Object)(object)Cpp.Read(() => c.targetTexture) != (UnityEngine.Object)null))
				{
					float num2 = Cpp.Read(() => c.depth, 0f);
					if (num2 > num)
					{
						num = num2;
						camera = c;
					}
				}
			}
			_camera = camera;
			return _camera;
		}
	}

	public static Transform PlayerTransform
	{
		get
		{
			PlayerCharacter p = Player;
			if ((UnityEngine.Object)(object)p != (UnityEngine.Object)null)
			{
				Transform val = Cpp.Read(() => ((Component)p).transform);
				if (Cpp.Alive((UnityEngine.Object)(object)val))
				{
					return val;
				}
			}
			return CameraRig;
		}
	}

	private static Transform CameraRig
	{
		get
		{
			Camera cam = Cam;
			if (!Cpp.Alive((UnityEngine.Object)(object)cam))
			{
				return null;
			}
			try
			{
				Transform val = ((Component)cam).transform;
				int num = 0;
				while ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && num++ < 24)
				{
					if ((UnityEngine.Object)(object)((Component)val).GetComponent<CharacterController>() != (UnityEngine.Object)null)
					{
						return val;
					}
					Rigidbody component = ((Component)val).GetComponent<Rigidbody>();
					if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null && !component.isKinematic)
					{
						return val;
					}
					val = val.parent;
				}
			}
			catch
			{
			}
			return null;
		}
	}

	public static CharacterController Controller
	{
		get
		{
			PlayerCharacter p = Player;
			if ((UnityEngine.Object)(object)p != (UnityEngine.Object)null)
			{
				CharacterController val = Cpp.Read(() => ((Character)p).Controller);
				if (Cpp.Alive((UnityEngine.Object)(object)val))
				{
					return val;
				}
			}
			Transform cameraRig = CameraRig;
			if ((UnityEngine.Object)(object)cameraRig == (UnityEngine.Object)null)
			{
				return null;
			}
			try
			{
				return ((Component)cameraRig).GetComponent<CharacterController>();
			}
			catch
			{
				return null;
			}
		}
	}

	public static Vector3 EyePos
	{
		get
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			Camera c = Cam;
			if (Cpp.Alive((UnityEngine.Object)(object)c))
			{
				return Cpp.Read(() => ((Component)c).transform.position, Vector3.zero);
			}
			Transform playerTransform = PlayerTransform;
			return ((UnityEngine.Object)(object)playerTransform == (UnityEngine.Object)null) ? Vector3.zero : (playerTransform.position + Vector3.up * 1.6f);
		}
	}

	public static Vector3 FeetPos
	{
		get
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			CharacterController controller = Controller;
			if (Cpp.Alive((UnityEngine.Object)(object)controller))
			{
				try
				{
					Vector3 position = ((Component)controller).transform.position;
					Vector3 center = controller.center;
					return new Vector3(position.x + center.x, position.y + center.y - controller.height * 0.5f, position.z + center.z);
				}
				catch
				{
				}
			}
			Camera cam = Cam;
			if (Cpp.Alive((UnityEngine.Object)(object)cam))
			{
				try
				{
					return ((Component)cam).transform.position - Vector3.up * 1.6f;
				}
				catch
				{
				}
			}
			Transform playerTransform = PlayerTransform;
			return ((UnityEngine.Object)(object)playerTransform != (UnityEngine.Object)null) ? playerTransform.position : Vector3.zero;
		}
	}

	public static Transform MovingTransform
	{
		get
		{
			CharacterController controller = Controller;
			if (Cpp.Alive((UnityEngine.Object)(object)controller))
			{
				try
				{
					return ((Component)controller).transform;
				}
				catch
				{
				}
			}
			return PlayerTransform;
		}
	}

	public static Vector3 Forward
	{
		get
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			Camera c = Cam;
			if (Cpp.Alive((UnityEngine.Object)(object)c))
			{
				return Cpp.Read(() => ((Component)c).transform.forward, Vector3.forward);
			}
			Transform playerTransform = PlayerTransform;
			return ((UnityEngine.Object)(object)playerTransform != (UnityEngine.Object)null) ? playerTransform.forward : Vector3.forward;
		}
	}

	public static bool MovementLocked
	{
		get
		{
			// This game build does not expose a player input-lock flag; movement simply is
			// not locked by the mod (menus are detected through the individual bridges).
			return false;
		}
	}

	public static bool InGame => InPlayScene && (UnityEngine.Object)(object)Player != (UnityEngine.Object)null;

	public static bool InPlayScene
	{
		get
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Scene activeScene = SceneManager.GetActiveScene();
				string name = activeScene.name;
				return !string.IsNullOrEmpty(name) && name.IndexOf("GameMain", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool CanMove => (UnityEngine.Object)(object)PlayerTransform != (UnityEngine.Object)null && !MovementLocked;

	public static float SceneAge => Time.unscaledTime - _sceneLoadedAt;

	public static bool Ready => (UnityEngine.Object)(object)Player != (UnityEngine.Object)null || Cpp.Alive((UnityEngine.Object)(object)Cam);

	public static DialogueUI Dialogue => Resolve<DialogueUI>(ref _dialogue);

	public static InventoryUI Inventory => Resolve<InventoryUI>(ref _inventory);

	public static UIRadialMenu Radial => Resolve<UIRadialMenu>(ref _radial);

	public static UseSelectUI UseSelect => Resolve<UseSelectUI>(ref _useSelect);

	public static NarratorManager Narrator => Resolve<NarratorManager>(ref _narrator);

	public static bool DialogueActive
	{
		get
		{
			// CurrentlyOnDisplay is now IDialogue, which has no Shown flag.
			// Read the canvas state instead of a story dialogue's historical state.
			DialogueUI ui = Dialogue;
			return Cpp.Alive(ui) && Cpp.Read(() => ui.IsShowing, fallback: false);
		}
	}

	public static bool InsideHouse
	{
		get
		{
			PlayerCharacter p = Player;
			return (UnityEngine.Object)(object)p != (UnityEngine.Object)null && Cpp.Read(() => ((Character)p).IsInsideHouse, fallback: false);
		}
	}

	public static void Invalidate()
	{
		_player = null;
		_camera = null;
		_dialogue = null;
		_inventory = null;
		_radial = null;
		_useSelect = null;
		_narrator = null;
		NextScan.Clear();
	}

	private static T Resolve<T>(ref T slot) where T : Component
	{
		if (Cpp.Alive((UnityEngine.Object)(object)slot))
		{
			return slot;
		}
		Type typeFromHandle;
		try
		{
			// A missing interop type must not throw out of here: this runs from
			// bridges every frame, and one TypeLoadException would take the whole
			// frame's work with it.
			typeFromHandle = typeof(T);
		}
		catch
		{
			return default(T);
		}
		float unscaledTime = Time.unscaledTime;
		if (NextScan.TryGetValue(typeFromHandle, out var value) && unscaledTime < value)
		{
			return default(T);
		}
		NextScan[typeFromHandle] = unscaledTime + 0.5f;
		try
		{
			slot = Cpp.FindOne<T>();
		}
		catch
		{
			return default(T);
		}
		return slot;
	}

	public static void Tick()
	{
	}

	public static void NoteSceneLoaded()
	{
		_sceneLoadedAt = Time.unscaledTime;
	}

	public static bool IsPlayerPart(Transform t)
	{
		if ((UnityEngine.Object)(object)t == (UnityEngine.Object)null)
		{
			return false;
		}
		Transform playerTransform = PlayerTransform;
		if ((UnityEngine.Object)(object)playerTransform == (UnityEngine.Object)null)
		{
			return false;
		}
		Transform val = t;
		int num = 0;
		while ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && num++ < 32)
		{
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)(object)playerTransform)
			{
				return true;
			}
			val = val.parent;
		}
		return false;
	}

	public static List<CanvasBase> OpenCanvases()
	{
		// This game build does not expose an IsShowing flag on CanvasBase; we treat a
		// modal canvas as open when its own GameObject is active.
		List<CanvasBase> list = new List<CanvasBase>();
		foreach (CanvasBase c in Cpp.FindAll<CanvasBase>(activeOnly: false))
		{
			if (Cpp.Alive((UnityEngine.Object)(object)c) && c.Modal)
			{
				try
				{
					if (((Component)c).gameObject.activeSelf)
					{
						list.Add(c);
					}
				}
				catch
				{
				}
			}
		}
		return list;
	}

	public static bool AnyModalOpen()
	{
		foreach (CanvasBase c in OpenCanvases())
		{
			if (Cpp.Read(() => c.Modal, fallback: false))
			{
				return true;
			}
		}
		return false;
	}

	public static List<Character> Npcs()
	{
		if (NpcCache.Count > 0 && Time.unscaledTime - _npcCacheAt < 1.5f)
		{
			for (int num = NpcCache.Count - 1; num >= 0; num--)
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)NpcCache[num]))
				{
					NpcCache.RemoveAt(num);
				}
			}
			if (NpcCache.Count > 0)
			{
				return NpcCache;
			}
		}
		_npcCacheAt = Time.unscaledTime;
		NpcCache.Clear();
		foreach (Character c in Cpp.FindAll<Character>(activeOnly: true))
		{
			if (Cpp.Alive((UnityEngine.Object)(object)c) && ((Il2CppObjectBase)c).TryCast<NonPlayerCharacter>() != null)
			{
				NpcCache.Add(c);
			}
		}
		return NpcCache;
	}

	public static string NameOf(Character c)
	{
		if ((UnityEngine.Object)(object)c == (UnityEngine.Object)null)
		{
			return null;
		}
		string text = Cpp.Read(() => ((CharacterMetaData)c).Name);
		return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
	}

	public static string NameOf(CharacterBase c)
	{
		if ((UnityEngine.Object)(object)c == (UnityEngine.Object)null)
		{
			return null;
		}
		string text = Cpp.Read(() => ((CharacterMetaData)c).Name);
		return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
	}

	public static void ShotFor(Character c, Shot kind, Vector3 from, out Vector3 aim, out float wantDistance)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		aim = FramePointOf(c, from);
		wantDistance = 2.5f;
		if (Cpp.Alive((UnityEngine.Object)(object)c))
		{
			Vector3 position;
			try
			{
				position = ((Component)c).transform.position;
			}
			catch
			{
				return;
			}
			float num = Mathf.Max(0.5f, AimPointOf(c).y - position.y);
			switch (kind)
			{
			case Shot.Face:
				aim = new Vector3(position.x, position.y + num * 0.92f, position.z);
				wantDistance = 1.2f;
				break;
			case Shot.Chest:
				aim = new Vector3(position.x, position.y + num * 0.72f, position.z);
				wantDistance = 2f;
				break;
			default:
				aim = new Vector3(position.x, position.y + num * 0.5f, position.z);
				wantDistance = 4f;
				break;
			}
		}
	}

	public static Vector3 FramePointOf(Character c, Vector3 from)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (!Cpp.Alive((UnityEngine.Object)(object)c))
		{
			return Vector3.zero;
		}
		Vector3 position;
		try
		{
			position = ((Component)c).transform.position;
		}
		catch
		{
			return AimPointOf(c);
		}
		Vector3 val = AimPointOf(c);
		float num = Mathf.Max(0.5f, val.y - position.y);
		float num2 = Vector3.Distance(from, position);
		float num3 = ((num2 < 2f) ? 0.62f : ((num2 < 4f) ? 0.55f : 0.5f));
		return new Vector3(position.x, position.y + num * num3, position.z);
	}

	public static Vector3 AimPointOf(Character c)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)c == (UnityEngine.Object)null)
		{
			return Vector3.zero;
		}
		Transform val = Cpp.Read(() => c.Head);
		if (Cpp.Alive((UnityEngine.Object)(object)val))
		{
			return val.position;
		}
		Transform val2 = Cpp.Read(() => ((Component)c).transform);
		return ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null) ? (val2.position + Vector3.up * 1.4f) : Vector3.zero;
	}

	public static string CurrentZoneName()
	{
		PlayerCharacter p = Player;
		if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
		{
			return null;
		}
		Zone z = Cpp.Read(() => ((Character)p).CurrentZone);
		if (!Cpp.Alive((UnityEngine.Object)(object)z))
		{
			return null;
		}
		string text = Cpp.Read(() => z.ZoneID);
		return string.IsNullOrWhiteSpace(text) ? null : text;
	}

	public static List<Zone> Zones()
	{
		List<Zone> list = new List<Zone>();
		foreach (Zone z in Cpp.FindAll<Zone>(activeOnly: false))
		{
			if (Cpp.Alive((UnityEngine.Object)(object)z))
			{
				string value = Cpp.Read(() => z.ZoneID);
				if (!string.IsNullOrWhiteSpace(value))
				{
					list.Add(z);
				}
			}
		}
		return list;
	}

	public static string NameOf(Zone z)
	{
		if ((UnityEngine.Object)(object)z == (UnityEngine.Object)null)
		{
			return null;
		}
		string text = Cpp.Read(() => z.ZoneID);
		return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
	}

	public static Vector3 CentreOf(Zone z)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)z == (UnityEngine.Object)null)
		{
			return Vector3.zero;
		}
		try
		{
			Collider val = Cpp.Read(() => ((Component)z).GetComponent<Collider>());
			if (Cpp.Alive((UnityEngine.Object)(object)val))
			{
				Bounds bounds = val.bounds;
				return bounds.center;
			}
		}
		catch
		{
		}
		Transform val2 = Cpp.Read(() => ((Component)z).transform);
		return ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null) ? val2.position : Vector3.zero;
	}

	public static List<Room> Rooms()
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		if (RoomCache.Count > 0 && Time.unscaledTime - _roomsBuiltAt < 10f)
		{
			return RoomCache;
		}
		_roomsBuiltAt = Time.unscaledTime;
		RoomCache.Clear();
		Dictionary<string, Room> dictionary = new Dictionary<string, Room>(StringComparer.OrdinalIgnoreCase);
		foreach (Zone z in Cpp.FindAll<Zone>(activeOnly: false))
		{
			if (!Cpp.Alive((UnityEngine.Object)(object)z))
			{
				continue;
			}
			string text = Cpp.Read(() => z.ZoneID);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = (dictionary[text] = new Room
				{
					Name = text.Trim(),
					Zone = z
				});
			}
			value.Volumes.AddRange(VolumesOf(z));
			try
			{
				Il2CppArrayBase<Collider> componentsInChildren = ((Component)z).GetComponentsInChildren<Collider>(true);
				if (componentsInChildren == null)
				{
					continue;
				}
				foreach (Collider item in componentsInChildren)
				{
					if (Cpp.Alive((UnityEngine.Object)(object)item))
					{
						value.Shapes.Add(item);
					}
				}
			}
			catch
			{
			}
		}
		RoomCache.AddRange(dictionary.Values);
		AddDoorwayAnchors();
		return RoomCache;
	}

	private static void AddDoorwayAnchors()
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		foreach (Door d in Cpp.FindAll<Door>(activeOnly: true))
		{
			if (!Cpp.Alive((UnityEngine.Object)(object)d))
			{
				continue;
			}
			bool[] array = new bool[2] { true, false };
			for (int i = 0; i < array.Length; i++)
			{
				GameObject val = (array[i] ? Cpp.Read(() => d.FrontKnob) : Cpp.Read(() => d.RearKnob));
				if (!Cpp.Alive((UnityEngine.Object)(object)val))
				{
					continue;
				}
				Vector3 position;
				try
				{
					position = val.transform.position;
				}
				catch
				{
					continue;
				}
				foreach (Room item in RoomCache)
				{
					if (!item.Contains(position))
					{
						continue;
					}
					item.Anchors.Add(position);
					break;
				}
			}
		}
	}

	private static List<Bounds> VolumesOf(Zone z)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		List<Bounds> list = new List<Bounds>();
		try
		{
			Il2CppArrayBase<Collider> componentsInChildren = ((Component)z).GetComponentsInChildren<Collider>(true);
			if (componentsInChildren != null)
			{
				foreach (Collider item in componentsInChildren)
				{
					if (Cpp.Alive((UnityEngine.Object)(object)item))
					{
						list.Add(item.bounds);
					}
				}
			}
			if (list.Count == 0)
			{
				Collider val = Cpp.Read(() => ((Component)z).GetComponent<Collider>());
				if (Cpp.Alive((UnityEngine.Object)(object)val))
				{
					list.Add(val.bounds);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	public static Room RoomAt(Vector3 point)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		foreach (Room item in Rooms())
		{
			if (item.Contains(point))
			{
				return item;
			}
		}
		return null;
	}

	public static Room RoomOwning(Vector3 point)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		if (Time.unscaledTime - _ownerCacheAt > 5f)
		{
			OwnerCache.Clear();
			_ownerCacheAt = Time.unscaledTime;
		}
		long key = ((long)(point.x * 3f) * 4093) ^ ((long)(point.y * 3f) * 8191) ^ ((long)(point.z * 3f) * 65521);
		if (OwnerCache.TryGetValue(key, out var value))
		{
			if (value == null)
			{
				return null;
			}
			foreach (Room item in Rooms())
			{
				if (string.Equals(item.Name, value, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
			}
			return null;
		}
		Room room = RoomOwningUncached(point);
		if (OwnerCache.Count > 4000)
		{
			OwnerCache.Clear();
		}
		OwnerCache[key] = room?.Name;
		return room;
	}

	private static Room RoomOwningUncached(Vector3 point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		Room room = RoomAt(point);
		if (room != null)
		{
			return room;
		}
		Room result = null;
		float num = 1.5f;
		foreach (Room item in Rooms())
		{
			foreach (Collider shape in item.Shapes)
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)shape))
				{
					continue;
				}
				Bounds bounds = shape.bounds;
				if (!(Mathf.Abs(bounds.min.y - point.y) > 2.5f))
				{
					float num2;
					try
					{
						num2 = Vector3.Distance(shape.ClosestPoint(point), point);
					}
					catch
					{
						continue;
					}
					if (!(num2 >= num))
					{
						num = num2;
						result = item;
					}
				}
			}
		}
		return result;
	}

	public static void ForgetRooms()
	{
		RoomCache.Clear();
		_roomsBuiltAt = 0f;
	}

	public static Zone ZoneAt(Vector3 point)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		foreach (Zone z in Zones())
		{
			try
			{
				Collider val = Cpp.Read(() => ((Component)z).GetComponent<Collider>());
				if (Cpp.Alive((UnityEngine.Object)(object)val))
				{
					Bounds bounds = val.bounds;
					if (bounds.Contains(point))
					{
						return z;
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}

	public static string DestinationOf(Door d, Zone from)
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)d == (UnityEngine.Object)null)
		{
			return null;
		}
		string text = NameOf(from);
		bool[] array = new bool[2] { true, false };
		for (int i = 0; i < array.Length; i++)
		{
			GameObject val = (array[i] ? Cpp.Read(() => d.FrontKnob) : Cpp.Read(() => d.RearKnob));
			if (Cpp.Alive((UnityEngine.Object)(object)val))
			{
				Zone z = ZoneAt(val.transform.position);
				string text2 = NameOf(z);
				if (!string.IsNullOrWhiteSpace(text2) && (string.IsNullOrWhiteSpace(text) || !string.Equals(text2, text, StringComparison.OrdinalIgnoreCase)))
				{
					return text2;
				}
			}
		}
		return null;
	}

	public static Zone CurrentZone()
	{
		PlayerCharacter p = Player;
		if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
		{
			return null;
		}
		return Cpp.Read(() => ((Character)p).CurrentZone);
	}

	public static string RoomOf(Character c)
	{
		if ((UnityEngine.Object)(object)c == (UnityEngine.Object)null)
		{
			return null;
		}
		Zone z = Cpp.Read(() => c.CurrentZone);
		return NameOf(z);
	}

	public static List<InteractiveItem> Items()
	{
		List<InteractiveItem> list = new List<InteractiveItem>();
		foreach (InteractiveItem item in Cpp.FindAll<InteractiveItem>(activeOnly: true))
		{
			if (Cpp.Alive((UnityEngine.Object)(object)item) && !((UnityEngine.Object)(object)((Il2CppObjectBase)item).TryCast<PlayerInteraction>() != (UnityEngine.Object)null) && !BelongsToCharacter(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<DistractableRigidItem> Props()
	{
		List<DistractableRigidItem> list = new List<DistractableRigidItem>();
		foreach (DistractableRigidItem item in Cpp.FindAll<DistractableRigidItem>(activeOnly: true))
		{
			if (!Cpp.Alive((UnityEngine.Object)(object)item) || (UnityEngine.Object)(object)((Il2CppObjectBase)item).TryCast<InteractiveItem>() != (UnityEngine.Object)null)
			{
				continue;
			}
			try
			{
				if (Cpp.Alive((UnityEngine.Object)(object)((Component)item).GetComponentInParent<Character>()))
				{
					continue;
				}
			}
			catch
			{
			}
			if (!IsSceneryMesh(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static string PropCensus()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		try
		{
			foreach (DistractableRigidItem item in Cpp.FindAll<DistractableRigidItem>(activeOnly: true))
			{
				if (!Cpp.Alive((UnityEngine.Object)(object)item))
				{
					continue;
				}
				num++;
				if ((UnityEngine.Object)(object)((Il2CppObjectBase)item).TryCast<InteractiveItem>() != (UnityEngine.Object)null)
				{
					num2++;
					continue;
				}
				try
				{
					if (Cpp.Alive((UnityEngine.Object)(object)((Component)item).GetComponentInParent<Character>()))
					{
						num3++;
						continue;
					}
				}
				catch
				{
				}
				if (string.IsNullOrWhiteSpace(NameOf(item)))
				{
					num4++;
				}
				else
				{
					num5++;
				}
			}
		}
		catch
		{
		}
		return $"{num} rigid items: {num2} interactive, {num3} on people, {num4} unnamed, {num5} movable props";
	}

	private static bool IsSceneryMesh(DistractableRigidItem d)
	{
		string name;
		try
		{
			name = ((UnityEngine.Object)((Component)d).gameObject).name;
		}
		catch
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			return true;
		}
		string text = name.ToLowerInvariant();
		if (text.Contains("lod"))
		{
			return true;
		}
		if (text.Contains("_low") || text.Contains("_high"))
		{
			return true;
		}
		if (text.Contains("mesh") || text.Contains("collider") || text.Contains("proxy"))
		{
			return true;
		}
		if (text.Contains("decor") || text.Contains("dressing") || text.Contains("prop_"))
		{
			return true;
		}
		if (Regex.IsMatch(name, "\\s[A-Z]$"))
		{
			return true;
		}
		return false;
	}

	public static string NameOf(DistractableRigidItem d)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)d))
		{
			return null;
		}
		string text = null;
		try
		{
			InteractiveItemBehavior beh = ((Component)d).GetComponentInChildren<InteractiveItemBehavior>(true);
			if (Cpp.Alive((UnityEngine.Object)(object)beh))
			{
				text = Cpp.Read(() => beh.DisplayName);
			}
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = ((UnityEngine.Object)((Component)d).gameObject).name;
			}
			catch
			{
			}
		}
		return string.IsNullOrWhiteSpace(text) ? null : TextUtil.Humanize(text);
	}

	private static bool BelongsToCharacter(InteractiveItem item)
	{
		try
		{
			return Cpp.Alive((UnityEngine.Object)(object)((Component)item).GetComponentInParent<Character>());
		}
		catch
		{
			return false;
		}
	}

	public static string NameOf(InteractiveItem item)
	{
		if ((UnityEngine.Object)(object)item == (UnityEngine.Object)null)
		{
			return null;
		}
		string text = null;
		try
		{
			InteractiveItemBehavior beh = ((Component)item).GetComponentInChildren<InteractiveItemBehavior>(true);
			if (Cpp.Alive((UnityEngine.Object)(object)beh))
			{
				text = Cpp.Read(() => beh.DisplayName);
			}
		}
		catch
		{
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		GameObject val = Cpp.Read(() => ((Component)item).gameObject);
		return ((UnityEngine.Object)(object)val == (UnityEngine.Object)null) ? null : TextUtil.Humanize(((UnityEngine.Object)val).name);
	}

	public static List<Door> Doors()
	{
		List<Door> list = new List<Door>();
		foreach (Door item in Cpp.FindAll<Door>(activeOnly: true))
		{
			if (Cpp.Alive((UnityEngine.Object)(object)item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static string StateOf(Door d)
	{
		if ((UnityEngine.Object)(object)d == (UnityEngine.Object)null)
		{
			return null;
		}
		if (Cpp.Read(() => d.IsLocked, fallback: false))
		{
			return "locked";
		}
		float num = DoorOpenness(d);
		if (num >= 0.85f)
		{
			return "open";
		}
		if (num >= 0.15f)
		{
			return "ajar";
		}
		return "closed";
	}

	// The door's animated open/closed state is not exposed directly on this game build,
	// so it is derived from how far the door has travelled between its closed and open
	// rotations/positions.
	private static float DoorOpenness(Door d)
	{
		try
		{
			Vector3 closed = Cpp.Read(() => d.ClosedRotation, Vector3.zero);
			Vector3 open = Cpp.Read(() => d.OpenRotation, Vector3.zero);
			if (open != closed)
			{
				Vector3 cur = Cpp.Read(() => ((Component)d).transform.eulerAngles, Vector3.zero);
				float diffOpen = Mathf.Abs(Mathf.DeltaAngle(cur.y, open.y));
				float full = Mathf.Abs(Mathf.DeltaAngle(closed.y, open.y));
				if (full > 0.1f)
				{
					return Mathf.Clamp01(1f - diffOpen / full);
				}
			}
			Vector3 openPos = Cpp.Read(() => d.OpenPosition, Vector3.zero);
			Vector3 closedPos = Cpp.Read(() => d.ClosedPosition, Vector3.zero);
			Vector3 pCur = Cpp.Read(() => ((Component)d).transform.position, Vector3.zero);
			float distOpen = Vector3.Distance(pCur, openPos);
			float distClosed = Vector3.Distance(pCur, closedPos);
			float total = distOpen + distClosed;
			return (total > 0.05f) ? Mathf.Clamp01(distClosed / total) : 0f;
		}
		catch
		{
			return 0f;
		}
	}

	public static bool IsWalkThrough(Door d)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if (!Cpp.Alive((UnityEngine.Object)(object)d))
		{
			return false;
		}
		try
		{
			Collider componentInChildren = ((Component)d).GetComponentInChildren<Collider>();
			if (!Cpp.Alive((UnityEngine.Object)(object)componentInChildren))
			{
				return false;
			}
			Bounds bounds = componentInChildren.bounds;
			if (bounds.size.y < 1.3f)
			{
				return false;
			}
			if (bounds.size.x < 0.5f && bounds.size.z < 0.5f)
			{
				return false;
			}
			float y = bounds.min.y;
			Room room = RoomAt(bounds.center);
			if (room != null)
			{
				float num = float.MaxValue;
				foreach (Bounds volume in room.Volumes)
				{
					Bounds current = volume;
					num = Mathf.Min(num, current.min.y);
				}
				if (num < float.MaxValue && y - num > 0.6f)
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsLocked(Door d)
	{
		return (UnityEngine.Object)(object)d != (UnityEngine.Object)null && Cpp.Read(() => d.IsLocked, fallback: false);
	}

	public static bool IsShut(Door d)
	{
		if ((UnityEngine.Object)(object)d == (UnityEngine.Object)null)
		{
			return false;
		}
		return DoorOpenness(d) < 0.15f;
	}

	public static string KeyFor(Door d)
	{
		if ((UnityEngine.Object)(object)d == (UnityEngine.Object)null)
		{
			return null;
		}
		string text = Cpp.Read(() => d.Key);
		return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
	}

	public static string ToggleDoor(Door d)
	{
		if ((UnityEngine.Object)(object)d == (UnityEngine.Object)null)
		{
			return null;
		}
		if (Cpp.Read(() => d.IsLocked, fallback: false))
		{
			string text = Cpp.Read(() => d.CustomLockedMessage);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return TextUtil.Clean(text);
			}
			string text2 = KeyFor(d);
			return string.IsNullOrEmpty(text2) ? "It is locked." : ("Locked. Needs the " + text2 + ".");
		}
		try
		{
			Character actor = Cpp.Read(() => ((Il2CppObjectBase)Player).TryCast<Character>());
			// Opening and closing are both driven through the door's open attempt; the
			// game itself decides whether a second attempt closes an open door.
			d.StartTryOpenDoor(actor);
			return IsShut(d) ? "Opening." : "Closing.";
		}
		catch (Exception ex)
		{
			Log.Warn("Door toggle failed: " + ex.Message);
			return "Could not move that door.";
		}
	}

	public static string TalkStatus(Character c)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)c))
		{
			return null;
		}
		InteractiveItem proxy = null;
		try
		{
			proxy = ((Component)c).GetComponentInChildren<InteractiveItem>();
		}
		catch
		{
		}
		if (!Cpp.Alive((UnityEngine.Object)(object)proxy))
		{
			return "cannot be approached";
		}
		if (!Cpp.Read(() => proxy.InPlayerInteractionRange(), fallback: false))
		{
			return "too far to talk";
		}
		List<Verb> list = VerbsOf(proxy);
		if (list.Count == 0)
		{
			return "nothing to say right now";
		}
		foreach (Verb item in list)
		{
			if (item.Name == null || item.Name.IndexOf("talk", StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			if (item.Available)
			{
				return null;
			}
			string text = Activity.Describe(c);
			return string.IsNullOrWhiteSpace(text) ? "cannot talk right now" : ("cannot talk, " + text);
		}
		return "no conversation available";
	}

	public static bool CanGrab(InteractiveItem item)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)item))
		{
			return false;
		}
		return Cpp.Read(() => item.canBeGrabbed, fallback: false);
	}

	public static InteractiveItem FocusedItem()
	{
		// This game build does not expose the interaction manager's focus slot; callers
		// fall back on the item's interaction range, which the manager itself uses.
		return null;
	}

	public static bool CanActOn(InteractiveItem item)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)item))
		{
			return false;
		}
		InteractiveItem val = FocusedItem();
		if (Cpp.Alive((UnityEngine.Object)(object)val) && val == item)
		{
			return true;
		}
		try
		{
			return Cpp.Read(() => item.InPlayerInteractionRange(), fallback: false);
		}
		catch
		{
			return false;
		}
	}

	public static string CrosshairText()
	{
		InteractionManager mgr = Cpp.FindOne<InteractionManager>(activeOnly: false);
		if (!Cpp.Alive((UnityEngine.Object)(object)mgr))
		{
			return null;
		}
		try
		{
			Text label = Cpp.Read(() => mgr.Text);
			if (!Cpp.Alive((UnityEngine.Object)(object)label))
			{
				return null;
			}
			string text = TextUtil.Clean(Cpp.Read(() => label.text));
			return string.IsNullOrWhiteSpace(text) ? null : text;
		}
		catch
		{
			return null;
		}
	}

	public static string GoalText()
	{
		InteractionManager mgr = Cpp.FindOne<InteractionManager>(activeOnly: false);
		if (!Cpp.Alive((UnityEngine.Object)(object)mgr))
		{
			return null;
		}
		try
		{
			Text label = Cpp.Read(() => mgr.Text);
			if (!Cpp.Alive((UnityEngine.Object)(object)label))
			{
				return null;
			}
			string text = TextUtil.Clean(Cpp.Read(() => label.text));
			return string.IsNullOrWhiteSpace(text) ? null : text;
		}
		catch
		{
			return null;
		}
	}

	public static bool OpenWheelFor(InteractiveItem item)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (!Cpp.Alive((UnityEngine.Object)(object)item))
		{
			return false;
		}
		RadialMenu val = Cpp.FindOne<RadialMenu>(activeOnly: false);
		if (!Cpp.Alive((UnityEngine.Object)(object)val))
		{
			return false;
		}
		try
		{
			Vector2 val2 = default(Vector2);
			val2 = new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f);
			WheelBridge.NotifyOpening();
			val.InteractWithCustomLocation(item, val2);
			return true;
		}
		catch (Exception ex)
		{
			Log.Debug("Could not open the wheel: " + ex.Message);
			return false;
		}
	}

	public static string DisplayLabel(InteractiveItem item)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)item))
		{
			return null;
		}
		int instanceID;
		try
		{
			instanceID = ((UnityEngine.Object)item).GetInstanceID();
		}
		catch
		{
			return null;
		}
		if (LabelCache.TryGetValue(instanceID, out var value))
		{
			return value;
		}
		// This game build has no radial-menu label helper; the item's own display name
		// (behaviour name, else GameObject name) is used instead.
		string text = NameOf(item);
		if (LabelCache.Count > 2000)
		{
			LabelCache.Clear();
		}
		LabelCache[instanceID] = text;
		return text;
	}

	public static void ForgetLabels()
	{
		LabelCache.Clear();
		_labelMenu = null;
		NpcCache.Clear();
	}

	public static bool CanAttackNow()
	{
		try
		{
			PlayerCharacter p = Player;
			if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
			{
				return false;
			}
			CCombat combat = Cpp.Read(() => ((Character)p).Combat);
			if (combat == null)
			{
				return false;
			}
			if (Cpp.Read(() => combat.IsAttacking, fallback: false))
			{
				return false;
			}
			// The per-attempt cooldown timestamp is not readable on this game build; the
			// game's own cooldown still applies to the attack call itself.
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static string SideOf(Door d)
	{
		if (!Cpp.Alive((UnityEngine.Object)(object)d))
		{
			return null;
		}
		bool flag = PlayerOnFrontSide(d);
		string text = RoomNameAtSpot(d, flag);
		string text2 = RoomNameAtSpot(d, !flag);
		if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
		{
			return flag ? "you are on the front side" : "you are on the back side";
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "you are in the " + text;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "the " + text2 + " is on the other side";
		}
		return $"you are in the {text}, the {text2} is on the other side";
	}

	private static bool PlayerOnFrontSide(Door d)
	{
		try
		{
			GameObject front = Cpp.Read(() => d.FrontKnob);
			GameObject rear = Cpp.Read(() => d.RearKnob);
			if (!Cpp.Alive((UnityEngine.Object)(object)front) && !Cpp.Alive((UnityEngine.Object)(object)rear))
			{
				return true;
			}
			Vector3 me = FeetPos;
			if (Cpp.Alive((UnityEngine.Object)(object)front) && Cpp.Alive((UnityEngine.Object)(object)rear))
			{
				return Vector3.Distance(me, front.transform.position) <= Vector3.Distance(me, rear.transform.position);
			}
			return Cpp.Alive((UnityEngine.Object)(object)front);
		}
		catch
		{
			return true;
		}
	}

	private static string RoomNameAtSpot(Door d, bool front)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			GameObject val = (front ? Cpp.Read(() => d.FrontKnob) : Cpp.Read(() => d.RearKnob));
			if (!Cpp.Alive((UnityEngine.Object)(object)val))
			{
				return null;
			}
			Room room = RoomOwning(val.transform.position);
			return (room == null) ? null : TextUtil.Humanize(room.Name);
		}
		catch
		{
			return null;
		}
	}

	public static bool StandUp()
	{
		PlayerCharacter player = Player;
		if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null)
		{
			return false;
		}
		Character c = ((Il2CppObjectBase)player).TryCast<Character>();
		if (!Cpp.Alive((UnityEngine.Object)(object)c))
		{
			return false;
		}
		ActionItem val = Cpp.Read(() => c.CurrentActionItem);
		bool result = false;
		if (Cpp.Alive((UnityEngine.Object)(object)val))
		{
			try
			{
				val.StopAction(true, true);
				result = true;
			}
			catch (Exception ex)
			{
				Log.Warn("Could not leave the seat: " + ex.Message);
			}
		}
		ActionItem val2 = Cpp.Read(() => c.CurrentSecondaryActionItemActor);
		if (Cpp.Alive((UnityEngine.Object)(object)val2))
		{
			try
			{
				val2.StopAction(true, true);
				result = true;
			}
			catch
			{
			}
		}
		return result;
	}

	public static bool Seated()
	{
		PlayerCharacter player = Player;
		if ((UnityEngine.Object)(object)player == (UnityEngine.Object)null)
		{
			return false;
		}
		Character c = ((Il2CppObjectBase)player).TryCast<Character>();
		if (!Cpp.Alive((UnityEngine.Object)(object)c))
		{
			return false;
		}
		try
		{
			if (Cpp.Read(() => c.IsSitting, fallback: false))
			{
				return true;
			}
			return Cpp.Alive((UnityEngine.Object)(object)Cpp.Read(() => c.CurrentActionItem));
		}
		catch
		{
			return false;
		}
	}

	public static List<Verb> VerbsOf(InteractiveItem item)
	{
		List<Verb> list = new List<Verb>();
		// Only the game's currently available interactions are returned here.
		foreach (string item2 in InteractionsOf(item))
		{
			list.Add(new Verb
			{
				Name = item2,
				Available = true
			});
		}
		return list;
	}

	/// <summary>Ask the game for actions whose current story criteria pass.</summary>
	public static List<string> InteractionsOf(InteractiveItem item)
	{
		if (!Cpp.Alive(item))
		{
			return new List<string>();
		}
		try
		{
			// Native RadialMenu uses InteractiveItem.Interactions, populated by this
			// virtual method. Declared ItemActions include actions whose criteria fail.
			return Cpp.ToManagedStrings(item.CalculateCurrentInteractionsFromStory(GameManager.GetActiveStory()));
		}
		catch (Exception ex)
		{
			Log.Warn("Could not read current interactions: " + ex.Message);
			throw;
		}
	}

	private static List<string> ReadList(Func<List<string>> getter)
	{
		List<string> list = Cpp.Read(getter);
		return Cpp.ToManagedStrings(list);
	}

	public static InteractiveItem SelfInteraction()
	{
		PlayerCharacter p = Player;
		if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
		{
			return null;
		}
		PlayerInteraction val = Cpp.Read(() => p.Interaction);
		if (Cpp.Alive((UnityEngine.Object)(object)val))
		{
			return ((Il2CppObjectBase)val).TryCast<InteractiveItem>();
		}
		try
		{
			Transform playerTransform = PlayerTransform;
			if ((UnityEngine.Object)(object)playerTransform != (UnityEngine.Object)null)
			{
				return ((Component)playerTransform).GetComponentInChildren<InteractiveItem>();
			}
		}
		catch
		{
		}
		return null;
	}
}
