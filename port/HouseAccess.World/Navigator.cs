using System;
using System.Collections.Generic;
using HouseAccess.Game;
using HouseAccess.InputLayer;
using HouseAccess.Speech;
using HouseAccess.Util;
using EekCharacterEngine;
using EekCharacterEngine.Interaction;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace HouseAccess.World;

public static class Navigator
{
	private static bool _turning;

	private static Quaternion _yawGoal;

	private static float _holdUntil;

	private static Entry _walkTarget;

	private static readonly List<Vector3> Path = new List<Vector3>();

	private static int _corner;

	private static float _legDeadline;

	private static float _repathAt;

	private static float _nextProgress;

	private static float _lastAnnounced = float.MaxValue;

	private static bool _partialRoute;

	private static bool _warnedPartial;

	private static Vector3 _lastGoal;

	private static Vector3 _fixedGoal;

	private static float _nextDoorTry;

	private static string _moverForThisWalk;

	private static float _watchSince;

	private static int _nudges;

	private static Vector3 _legStart;

	private static int _warpFailures;

	// Whichever movement tier (warp / frame / direct) last moved the player in this
	// scene. On some game builds the warp and frame tiers never move the player, and
	// only driving the CharacterController directly does; remembering the tier that
	// worked stops every walk from re-proving the two dead tiers first.
	private static string _lastGoodMover;

	private static Vector3 _controllerStep;
	private static int _controllerStepFrame = -1;
	private static float _pausedAt = -1f;
	private static float _bestRemaining;
	private static bool _controllerStepLogged;

	// Match the real controller's skin and step height rather than skipping a
	// turn 0.6 metres early or treating vertically stacked corners as one point.
	private static float CornerRadius => Cpp.Alive(GameRefs.Controller) ? Mathf.Max(0.02f, GameRefs.Controller.skinWidth) : 0.05f;
	private static float CornerHeight => Cpp.Alive(GameRefs.Controller) ? GameRefs.Controller.stepOffset + GameRefs.Controller.skinWidth : 0.1f;

	/// <summary>Suspend a route while a popup owns input; keep its destination.</summary>
	public static void PauseForPopup()
	{
		_controllerStepFrame = -1;
		if (IsWalking && _pausedAt < 0f)
		{
			_pausedAt = Time.unscaledTime;
			CancelWarps();
			Log.Info("Auto-walk paused for popup.");
		}
	}

	/// <summary>Add one planned step before the game's own controller move and gravity.</summary>
	public static void BeforeControllerMove(PlayerCharacter player)
	{
		if (_controllerStepFrame != Time.frameCount) return;
		_controllerStepFrame = -1;
		if (!IsWalking || PopupBridge.BlocksGameplay || GameRefs.MovementLocked || Keys.MovementKeyHeld) return;
		if (!Cpp.Alive(player) || player != GameRefs.Player || !Cpp.Alive(GameRefs.Controller) || !GameRefs.Controller.enabled) return;
		// Native TryMoveController consumes _movement, adds its falling motion,
		// calls CharacterController.Move, then clears _movement. GetSpeed already
		// includes deltaTime and the current crouch/run/drunk modifiers.
		float allowed = Mathf.Max(0f, player.GetSpeed);
		player._movement += Vector3.ClampMagnitude(_controllerStep, allowed);
		if (!_controllerStepLogged)
		{
			_controllerStepLogged = true;
			CharacterController controller = GameRefs.Controller;
			Log.Info($"Auto-walk uses native controller movement; radius={controller.radius:0.00}, step={controller.stepOffset:0.00}, slope={controller.slopeLimit:0.0}.");
		}
	}

	public static bool IsWalking => _walkTarget != null && _walkTarget.Alive;

	public static void Reset()
	{
		_turning = false;
		_controllerStepFrame = -1;
		_pausedAt = -1f;
		CancelWarps();
		_walkTarget = null;
		Path.Clear();
		_corner = 0;
		_lastGoodMover = null;
	}

	private static string CurrentMover()
	{
		return (_moverForThisWalk ?? _lastGoodMover ?? Prefs.MoveMode?.Value ?? Prefs.DefaultMoveMode).Trim().ToLowerInvariant();
	}

	public static void FaceCurrentQuiet()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Entry current = Radar.Current;
		if (current != null)
		{
			FacePoint(current.Point);
		}
	}

	public static void FaceCurrent()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		Entry current = Radar.Current;
		if (current == null)
		{
			Speaker.Say("No target selected.", Pri.High);
			return;
		}
		FacePoint(current.Point);
		Speaker.Say($"Facing {current.Label}, {TextUtil.Distance(current.Distance)}.", Pri.High);
	}

	public static void TurnBy(float degrees)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		Transform movingTransform = GameRefs.MovingTransform;
		if ((UnityEngine.Object)(object)movingTransform == (UnityEngine.Object)null)
		{
			Speaker.SayNow("Cannot turn right now.");
			return;
		}
		try
		{
			_yawGoal = Quaternion.Euler(0f, movingTransform.eulerAngles.y + degrees, 0f);
			_turning = true;
			_holdUntil = Time.unscaledTime + Mathf.Max(0.4f, Prefs.FaceHoldSeconds.Value);
		}
		catch
		{
			return;
		}
		Speaker.Say($"{Mathf.Abs(Mathf.RoundToInt(degrees))} degrees {((degrees < 0f) ? "left" : "right")}.");
	}

	public static void FaceBearing(float degrees)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Transform movingTransform = GameRefs.MovingTransform;
		if (!((UnityEngine.Object)(object)movingTransform == (UnityEngine.Object)null))
		{
			_yawGoal = Quaternion.Euler(0f, degrees, 0f);
			_turning = true;
			_holdUntil = Time.unscaledTime + Mathf.Max(0.4f, Prefs.FaceHoldSeconds.Value);
		}
	}

	public static void FacePoint(Vector3 world)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		Transform movingTransform = GameRefs.MovingTransform;
		if (!((UnityEngine.Object)(object)movingTransform == (UnityEngine.Object)null))
		{
			Vector3 val = world - movingTransform.position;
			val.y = 0f;
			if (!(val.sqrMagnitude < 0.0001f))
			{
				_yawGoal = Quaternion.LookRotation(val.normalized, Vector3.up);
				_turning = true;
				_holdUntil = Time.unscaledTime + Mathf.Max(0.4f, Prefs.FaceHoldSeconds.Value);
			}
		}
	}

	private static void ApplyFacing(float dt)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (!_turning || IsWalking || PopupBridge.BlocksGameplay || GameRefs.MovementLocked)
		{
			return;
		}
		if (PlayerIsLooking())
		{
			_turning = false;
			return;
		}
		Transform movingTransform = GameRefs.MovingTransform;
		if ((UnityEngine.Object)(object)movingTransform == (UnityEngine.Object)null)
		{
			_turning = false;
			return;
		}
		float num = Mathf.Max(90f, Prefs.TurnSpeedDegPerSec.Value) * dt;
		try
		{
			movingTransform.rotation = Quaternion.RotateTowards(movingTransform.rotation, _yawGoal, num);
		}
		catch
		{
			_turning = false;
			return;
		}
		if (Quaternion.Angle(movingTransform.rotation, _yawGoal) < 0.5f && Time.unscaledTime > _holdUntil)
		{
			_turning = false;
		}
	}

	public static void ToggleWalk()
	{
		if (IsWalking)
		{
			Stop("Stopped.");
			return;
		}
		Entry current = Radar.Current;
		if (current == null)
		{
			Speaker.Say("No target selected.", Pri.High);
		}
		else if (CanStart())
		{
			_moverForThisWalk = null;
			if (current.Kind == EntryKind.Person && Prefs.HoldWhileWalking.Value)
			{
				Hold.Begin(current.Person, announce: false);
			}
			Begin(current, $"Walking to {current.Label}, {TextUtil.Distance(current.Distance)}.");
		}
	}

	public static void WalkToPoint(Vector3 world, string label)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (CanStart())
		{
			_moverForThisWalk = null;
			Entry entry = new Entry
			{
				Kind = EntryKind.Room,
				Zone = null,
				Label = (string.IsNullOrEmpty(label) ? "the spot" : label),
				Point = world
			};
			float metres = Vector3.Distance(GameRefs.FeetPos, world);
			Begin(entry, $"Walking to {entry.Label}, {TextUtil.Distance(metres)}.");
		}
	}

	private static bool CanStart()
	{
		if ((UnityEngine.Object)(object)GameRefs.PlayerTransform == (UnityEngine.Object)null)
		{
			Speaker.SayNow("Not in the game world yet.");
			return false;
		}
		if (GameRefs.MovementLocked)
		{
			Speaker.SayNow("The game has taken control of movement right now.");
			return false;
		}
		return true;
	}

	private static void Begin(Entry e, string announcement)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		_turning = false;
		_walkTarget = e;
		_controllerStepLogged = false;
		_controllerStepFrame = -1;
		_pausedAt = -1f;
		_partialRoute = false;
		Path.Clear();
		_corner = 0;
		_repathAt = 0f;
		_legDeadline = 0f;
		_warnedPartial = false;
		_lastAnnounced = float.MaxValue;
		_nextProgress = Time.unscaledTime + 1f;
		if (LiveReach(e))
		{
			_walkTarget = null;
			FacePoint(e.Point);
			Speaker.SayNow(e.Label + " is already beside you.");
			return;
		}
		_lastGoal = e.GroundPoint;
		_fixedGoal = e.GroundPoint;
		_warpFailures = 0;
		_legDeadline = 0f;
		_watchSince = 0f;
		_nudges = 0;
		BuildPath();
		if (!IsWalking) return;
		_bestRemaining = RouteProgress.Remaining(GameRefs.FeetPos, Path, _corner);
		Log.Info($"Auto-walk started: {e.Label}, {Path.Count} corners, partial={_partialRoute}, mover={CurrentMover()}.");
		Speaker.SayNow(announcement);
	}

	public static void Stop(string reason)
	{
		if (Hold.Active)
		{
			Hold.Release(null);
		}
		bool isWalking = IsWalking;
		_controllerStepFrame = -1;
		_pausedAt = -1f;
		_moverForThisWalk = null;
		CancelWarps();
		_walkTarget = null;
		Path.Clear();
		_corner = 0;
		if (isWalking && !string.IsNullOrEmpty(reason))
		{
			Log.Info("Auto-walk stopped: " + reason);
			Speaker.SayNow(reason);
		}
	}

	private static void BuildPath()
	{
		Path.Clear();
		_corner = 0;
		_partialRoute = false;
		if (!IsWalking) return;
		if (!TryNavMesh(GameRefs.FeetPos, _fixedGoal))
		{
			Stop("No walkable route found to " + _walkTarget.Label + ". Try a nearer target.");
			return;
		}
		Log.Debug($"Route: from {GameRefs.FeetPos} to {_fixedGoal}, {Path.Count} corners, partial={_partialRoute}.");
	}

	private static bool TryNavMesh(Vector3 from, Vector3 to)
	{
		if (!CalculateRoute(from, to, Path, out _partialRoute)) return false;
		if (_partialRoute && !_warnedPartial)
		{
			_warnedPartial = true;
			Speaker.Say("Only a partial route. I will stop at its reachable end.", Pri.High);
		}
		return true;
	}

	public static bool RouteTo(Vector3 from, Vector3 to, List<Vector3> corners)
	{
		if (CalculateRoute(from, to, corners, out bool partial) && !partial) return true;
		corners.Clear();
		return false;
	}

	private static bool CalculateRoute(Vector3 from, Vector3 to, List<Vector3> corners, out bool partial)
	{
		corners.Clear();
		partial = false;
		try
		{
			NavMeshPath best = null;
			foreach (Vector3 start in Candidates(from))
			{
				foreach (Vector3 end in Candidates(to))
				{
					NavMeshPath route = new NavMeshPath();
					if (!NavMesh.CalculatePath(start, end, NavMesh.AllAreas, route) || route.status == NavMeshPathStatus.PathInvalid) continue;
					if (best == null || route.status == NavMeshPathStatus.PathComplete) best = route;
					if (route.status == NavMeshPathStatus.PathComplete) break;
				}
				if (best != null && best.status == NavMeshPathStatus.PathComplete) break;
			}
			if (best == null || best.corners == null || best.corners.Length == 0) return false;
			partial = best.status != NavMeshPathStatus.PathComplete;
			foreach (Vector3 corner in best.corners) corners.Add(corner);
			// Keep the sampled start if the player still has to walk to it.
			if (corners.Count > 1 && RouteProgress.Reached(from, corners[0], CornerRadius, CornerHeight)) corners.RemoveAt(0);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("NavMesh route failed: " + ex.Message);
			return false;
		}
	}

	private static List<Vector3> Candidates(Vector3 point)
	{
		List<Vector3> candidates = new List<Vector3>();
		CharacterController controller = GameRefs.Controller;
		float height = Cpp.Alive(controller) ? controller.height * 0.5f : 1f;
		// Large 5/9 metre samples could select another floor. Limit the search to
		// the player's size, and reject a hit outside that floor's vertical band.
		if (NavMesh.SamplePosition(point, out NavMeshHit hit, height * 2f, NavMesh.AllAreas)
			&& Mathf.Abs(hit.position.y - point.y) <= height)
			candidates.Add(hit.position);
		return candidates;
	}

	private static void ApplyWalk(float dt)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		_controllerStepFrame = -1;
		if (!IsWalking)
		{
			if (_walkTarget != null) Stop("The target is no longer available.");
			return;
		}
		if (PopupBridge.BlocksGameplay) { PauseForPopup(); return; }
		if (_pausedAt >= 0f)
		{
			_pausedAt = -1f;
			_watchSince = Time.unscaledTime;
			_repathAt = 0f;
			Log.Info("Auto-walk resumed after popup.");
		}
		if (Keys.MovementKeyHeld)
		{
			Stop("Manual control.");
			return;
		}
		if (GameRefs.MovementLocked)
		{
			Stop("Movement locked.");
			return;
		}
		PlayerCharacter player = GameRefs.Player;
		Vector3 feetPos = GameRefs.FeetPos;
		Vector3 fixedGoal = _walkTarget.Kind == EntryKind.Person ? _walkTarget.GroundPoint : _fixedGoal;
		float num2 = Mathf.Max(0.4f, Prefs.StopDistance.Value);
		bool flag = Mathf.Abs(feetPos.y - fixedGoal.y) <= CornerHeight;
		bool reachable = LiveReach(_walkTarget);
		bool flag4 = Cpp.Alive(_walkTarget.Item) ? reachable
			: (_walkTarget.Kind == EntryKind.Room && InsideZone(_walkTarget, feetPos))
				|| (!_partialRoute && flag && RouteProgress.Remaining(feetPos, Path, _corner) <= num2);
		if (flag4)
		{
			Arrive(fixedGoal);
			return;
		}

		if (Time.unscaledTime >= _repathAt && !IsWarping(player))
		{
			_repathAt = Time.unscaledTime + 1.5f;
			if (Vector3.Distance(fixedGoal, _lastGoal) > 0.75f)
			{
				_lastGoal = fixedGoal;
				_fixedGoal = fixedGoal;
				BuildPath();
				if (!IsWalking) return;
				_bestRemaining = RouteProgress.Remaining(feetPos, Path, _corner);
			}
		}
		if (Prefs.AutoOpenDoors.Value)
		{
			Vector3 val = NextWaypoint(feetPos) - feetPos;
			if (!IsWalking) return;
			val.y = 0f;
			OpenDoorAhead(feetPos, val.normalized);
		}
		if (!IsWalking) return;
		StepMove(player, feetPos, dt);
		if (!IsWalking) return;
		Watchdog(feetPos);
		AnnounceProgress(RouteProgress.Remaining(feetPos, Path, _corner));
	}

	private static void StepMove(PlayerCharacter p, Vector3 pos, float dt)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		string text2 = CurrentMover();
		string text3 = text2;
		if (!(text3 == "frame"))
		{
			if (text3 == "direct")
			{
				StepDirect(pos, dt);
			}
			else
			{
				StepWarp(p, pos);
			}
		}
		else
		{
			StepFrameMovement(p, pos, dt);
		}
	}

	private static void Watchdog(Vector3 pos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		float remaining = RouteProgress.Remaining(pos, Path, _corner);
		if (_bestRemaining - remaining > CornerRadius)
		{
			_bestRemaining = remaining;
			_watchSince = Time.unscaledTime;
			_nudges = 0;
			// Whatever tier actually moved the player is the one worth trying first on
			// the next walk of this scene.
			string mover = CurrentMover();
			if (mover == "warp" || mover == "frame" || mover == "direct")
			{
				_lastGoodMover = mover;
			}
			return;
		}
		if (_watchSince <= 0f)
		{
			_watchSince = Time.unscaledTime;
			return;
		}
		float num = Time.unscaledTime - _watchSince;
		if (!(num < 1.6f))
		{
			string text = CurrentMover();
			float remain = Vector3.Distance(pos, _fixedGoal);
			if (text == "warp")
			{
				_moverForThisWalk = "frame";
				_watchSince = Time.unscaledTime;
				Log.Warn($"Warping did not move the player (still {remain:0.0} m away); trying frame movement.");
			}
			else if (text == "frame")
			{
				_moverForThisWalk = "direct";
				_watchSince = Time.unscaledTime;
				Log.Warn($"Frame movement did not move the player (still {remain:0.0} m away); driving the controller.");
			}
			else if (_nudges < 3)
			{
				_nudges++;
				_watchSince = Time.unscaledTime;
				_corner = 0;
				BuildPath();
				_bestRemaining = RouteProgress.Remaining(pos, Path, _corner);
				Log.Info($"Auto-walk route recovery {_nudges}: {_walkTarget?.Label}, {remain:0.0} m away, position={pos}, next={(Path.Count > 0 ? Path[0].ToString() : "none")}.");
			}
			else if (!(num < 5f))
			{
				string text2 = DescribeBlocker();
				Stop(string.IsNullOrEmpty(text2) ? "Stuck. Try walking manually, or pick a nearer target." : ("Stuck against " + text2 + ". Try walking manually, or pick a nearer target."));
			}
		}
	}

	private static void StepDirect(Vector3 pos, float dt)
	{
		Vector3 waypoint = NextWaypoint(pos);
		if (!IsWalking) return;
		CharacterController controller = GameRefs.Controller;
		if (!Cpp.Alive(controller) || !controller.enabled)
		{
			Stop("The player controller is unavailable.");
			return;
		}
		Vector3 step = RouteProgress.HorizontalStep(pos, waypoint, Mathf.Max(0f, Prefs.WalkSpeed.Value) * dt);
		if (!IsFinite(step) || step.sqrMagnitude <= 0f) return;
		// Stay on the computed corridor at corners. The controller handles physical
		// contact and stairs; a random sidestep can push the route into a wall.
		_controllerStep = step;
		_controllerStepFrame = Time.frameCount;
		Steer(step.normalized, dt);
	}

	private static bool IsWarping(PlayerCharacter p)
	{
		if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
		{
			return false;
		}
		return Cpp.Read(() => ((Character)p).IsWarping, fallback: false);
	}

	private static void StepWarp(PlayerCharacter p, Vector3 pos)
	{
		try
		{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		if (IsWarping(p))
		{
			if (_legDeadline > 0f && Time.unscaledTime > _legDeadline)
			{
				CancelWarps();
			}
			return;
		}
		if (_legDeadline > 0f)
		{
			_legDeadline = 0f;
			if (Vector3.Distance(pos, _legStart) < 0.15f)
			{
				_warpFailures++;
				if (_warpFailures >= 2)
				{
					Log.Warn("WarpOverTime is not moving the player; switching to frame movement.");
					_moverForThisWalk = "frame";
					_warpFailures = 0;
					return;
				}
			}
			else
			{
				_warpFailures = 0;
			}
		}
		if (Path.Count == 0 || _corner >= Path.Count)
		{
			FinishRoute();
			return;
		}
		Vector3 val = Path[_corner];
		_corner++;
		float num = Vector3.Distance(pos, val);
		float num2 = Mathf.Max(0.5f, Prefs.WalkSpeed.Value);
		float num3 = Mathf.Clamp(num / num2, 0.15f, 6f);
		FacePoint(val);
		try
		{
			_legStart = pos;
			((Character)p).WarpOverTime(val, num3);
			_legDeadline = Time.unscaledTime + num3 + 1.5f;
		}
			catch (Exception ex)
			{
				Log.Warn("WarpOverTime failed, falling back to frame movement: " + ex.Message);
				_moverForThisWalk = "frame";
			}
		}
		catch (Exception ex2)
		{
			// The warp bookkeeping (path, corners, walk target) can throw when the
			// walk state was disturbed, e.g. right after a cutscene; never let that
			// kill the walk or spam the log with a raw NullReferenceException.
			Log.Warn("Warp step failed, falling back to frame movement: " + ex2.Message);
			_moverForThisWalk = "frame";
			CancelWarps();
		}
	}

	private static void StepFrameMovement(PlayerCharacter p, Vector3 pos, float dt)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		if ((UnityEngine.Object)(object)p == (UnityEngine.Object)null)
		{
			return;
		}
		Vector3 val = NextWaypoint(pos);
		if (!IsWalking) return;
		Vector3 val2 = val - pos;
		val2.y = 0f;
		if (val2.sqrMagnitude < 0.0001f)
		{
			return;
		}
		val2.Normalize();
		Vector3 val3 = val2 * Mathf.Max(0.5f, Prefs.WalkSpeed.Value) * dt;
		if (!IsFinite(val3))
		{
			return;
		}
		try
		{
			((Character)p).AddToFrameMovement(val3);
		}
		catch (Exception ex)
		{
			Log.Warn("AddToFrameMovement failed, using the controller: " + ex.Message);
			CharacterController cc = GameRefs.Controller;
			if (Cpp.Alive((UnityEngine.Object)(object)cc) && Cpp.Read(() => ((Collider)cc).enabled, fallback: false))
			{
				try
				{
					_controllerStep = val3;
					_controllerStepFrame = Time.frameCount;
				}
				catch
				{
					Stop("Cannot move.");
				}
			}
			else
			{
				Stop("Cannot move.");
			}
		}
		Steer(val2, dt);
	}

	private static Vector3 NextWaypoint(Vector3 pos)
	{
		while (_corner < Path.Count && RouteProgress.Reached(pos, Path[_corner], CornerRadius, CornerHeight))
		{
			Log.Debug($"Route corner {_corner + 1}/{Path.Count} reached at {pos}.");
			_corner++;
		}
		if (_corner < Path.Count) return Path[_corner];
		FinishRoute();
		return pos;
	}

	private static void FinishRoute()
	{
		if (!IsWalking) return;
		if (LiveReach(_walkTarget) || (_walkTarget.Kind == EntryKind.Room && InsideZone(_walkTarget, GameRefs.FeetPos)))
			Arrive(LivePoint(_walkTarget));
		else
			Stop(_partialRoute ? "Reached the end of the partial route. The target is still out of reach." : "Reached the nearest walkable point. The target is still out of reach.");
	}

	private static void Steer(Vector3 dir, float dt)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		Transform movingTransform = GameRefs.MovingTransform;
		if ((UnityEngine.Object)(object)movingTransform == (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			Quaternion val = Quaternion.LookRotation(dir, Vector3.up);
			movingTransform.rotation = Quaternion.RotateTowards(movingTransform.rotation, val, Mathf.Max(90f, Prefs.TurnSpeedDegPerSec.Value * 0.6f) * dt);
		}
		catch
		{
		}
	}

	private static void Arrive(Vector3 goal)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		_controllerStepFrame = -1;
		CancelWarps();
		FacePoint(goal);
		string text = ((_walkTarget != null) ? _walkTarget.Label : "the spot");
		bool flag = _walkTarget != null && _walkTarget.Kind == EntryKind.Person;
		float num = ((_walkTarget != null) ? (_walkTarget.Point.y - GameRefs.FeetPos.y) : 0f);
		string text2 = ((num > 1.8f) ? " It is above you." : ((num < -1.2f) ? " It is below you." : string.Empty));
		_walkTarget = null;
		Path.Clear();
		_corner = 0;
		Speaker.SayNow((flag && Hold.Active) ? ("Arrived at " + text + ". Still holding." + text2) : ("Arrived at " + text + "." + text2));
	}

	private static void CancelWarps()
	{
		try
		{
			PlayerCharacter player = GameRefs.Player;
			if ((UnityEngine.Object)(object)player != (UnityEngine.Object)null)
			{
				((Character)player).CancelAllWarps();
			}
		}
		catch
		{
		}
		_legDeadline = 0f;
	}

	private static Vector3 LivePoint(Entry e)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		if (e == null)
		{
			return Vector3.zero;
		}
		try
		{
			if (e.Kind == EntryKind.Person && Cpp.Alive((UnityEngine.Object)(object)e.Person))
			{
				return GameRefs.AimPointOf(e.Person);
			}
			if (Cpp.Alive((UnityEngine.Object)(object)e.Item))
			{
				Collider componentInChildren = ((Component)e.Item).GetComponentInChildren<Collider>();
				if (Cpp.Alive((UnityEngine.Object)(object)componentInChildren))
				{
					Bounds bounds = componentInChildren.bounds;
					return bounds.center;
				}
				return ((Component)e.Item).transform.position;
			}
		}
		catch
		{
		}
		return e.Point;
	}

	private static bool LiveReach(Entry e)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if (e == null)
		{
			return false;
		}
		try
		{
			if (Cpp.Alive((UnityEngine.Object)(object)e.Item))
			{
				return Cpp.Read(() => e.Item.InPlayerInteractionRange(), fallback: false);
			}
		}
		catch
		{
		}
		try
		{
			return Vector3.Distance(GameRefs.FeetPos, LivePoint(e)) <= 1.6f;
		}
		catch
		{
			return false;
		}
	}

	private static bool InsideZone(Entry e, Vector3 feet)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (e == null || e.RoomRef == null)
		{
			return false;
		}
		try
		{
			string text = GameRefs.CurrentZoneName();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return string.Equals(text.Trim(), e.RoomRef.Name, StringComparison.OrdinalIgnoreCase);
			}
			return e.RoomRef.Contains(feet);
		}
		catch
		{
			return false;
		}
	}

	private static void AnnounceProgress(float remaining)
	{
		if (Prefs.AnnounceProgress.Value && !(Time.unscaledTime < _nextProgress) && (_lastAnnounced == float.MaxValue || !(_lastAnnounced - remaining < 1.5f)))
		{
			_nextProgress = Time.unscaledTime + 1.4f;
			_lastAnnounced = remaining;
			Speaker.Say(TextUtil.Distance(remaining), Pri.Low);
		}
	}

	private static void OpenDoorAhead(Vector3 pos, Vector3 dir)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (Time.unscaledTime < _nextDoorTry || !IsFinite(dir) || dir.sqrMagnitude < 0.0001f)
		{
			return;
		}
		try
		{
			Vector3 val = pos + Vector3.up * 1f;
			RaycastHit val2 = default(RaycastHit);
			if (!Physics.SphereCast(val + dir * 0.4f, 0.3f, dir, out val2, 2.5f, -1, (QueryTriggerInteraction)1) || (UnityEngine.Object)(object)val2.collider == (UnityEngine.Object)null || IsOurs(val2.transform))
			{
				return;
			}
			Door componentInParent = ((Component)val2.collider).gameObject.GetComponentInParent<Door>();
			if (Cpp.Alive((UnityEngine.Object)(object)componentInParent) && GameRefs.IsWalkThrough(componentInParent))
			{
				_nextDoorTry = Time.unscaledTime + 1.5f;
				if (GameRefs.IsLocked(componentInParent))
				{
					string text = GameRefs.KeyFor(componentInParent);
					Stop(string.IsNullOrEmpty(text) ? "That door is locked." : ("That door is locked. It needs the " + text + "."));
				}
				else if (GameRefs.IsShut(componentInParent))
				{
					Speaker.Say("Opening " + (GameRefs.NameOf((InteractiveItem)(object)componentInParent) ?? "the door") + ".");
					GameRefs.ToggleDoor(componentInParent);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Door open attempt failed: " + ex.Message);
		}
	}

	private static string DescribeBlocker()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Transform playerTransform = GameRefs.PlayerTransform;
			if ((UnityEngine.Object)(object)playerTransform == (UnityEngine.Object)null)
			{
				return null;
			}
			Vector3 val = GameRefs.FeetPos + Vector3.up * 1f;
			Vector3 forward = playerTransform.forward;
			RaycastHit val2 = default(RaycastHit);
			if (!Physics.SphereCast(val + forward * 0.4f, 0.28f, forward, out val2, 1.2f, -1, (QueryTriggerInteraction)1))
			{
				return null;
			}
			if (IsOurs(val2.transform))
			{
				return null;
			}
			GameObject val3 = (((UnityEngine.Object)(object)val2.collider != (UnityEngine.Object)null) ? ((Component)val2.collider).gameObject : null);
			if ((UnityEngine.Object)(object)val3 == (UnityEngine.Object)null)
			{
				return null;
			}
			try
			{
				InteractiveItem componentInParent = val3.GetComponentInParent<InteractiveItem>();
				if (Cpp.Alive((UnityEngine.Object)(object)componentInParent))
				{
					string text = GameRefs.NameOf(componentInParent);
					if (!string.IsNullOrWhiteSpace(text))
					{
						return "the " + text;
					}
				}
				Character componentInParent2 = val3.GetComponentInParent<Character>();
				if (Cpp.Alive((UnityEngine.Object)(object)componentInParent2))
				{
					string text2 = GameRefs.NameOf(componentInParent2);
					if (!string.IsNullOrWhiteSpace(text2))
					{
						return text2;
					}
				}
			}
			catch
			{
			}
			string text3 = TextUtil.Humanize(((UnityEngine.Object)val3).name);
			return string.IsNullOrWhiteSpace(text3) ? null : ("the " + text3);
		}
		catch
		{
			return null;
		}
	}

	private static bool PlayerIsLooking()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (Keys.Held((KeyCode)114) || Keys.Held((KeyCode)116) || Keys.Held((KeyCode)102) || Keys.Held((KeyCode)118))
			{
				return true;
			}
			Vector2 val = ((Mouse.current != null) ? ((InputControl<Vector2>)(object)((Pointer)Mouse.current).delta).ReadValue() : Vector2.zero);
			return val.sqrMagnitude > 4f;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsOurs(Transform t)
	{
		return GameRefs.IsPlayerPart(t);
	}

	private static bool IsFinite(Vector3 v)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
	}

	public static void Tick()
	{
		float num = Time.unscaledDeltaTime;
		if (num <= 0f || num > 0.25f)
		{
			num = 0.0166f;
		}
		ApplyWalk(num);
		ApplyFacing(num);
	}

	public static void LateTick()
	{
		float num = Time.unscaledDeltaTime;
		if (num <= 0f || num > 0.25f)
		{
			num = 0.0166f;
		}
		ApplyFacing(num);
	}

	public static void LookAhead()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		string text = GameRefs.CrosshairText();
		if (!string.IsNullOrWhiteSpace(text))
		{
			Speaker.Say(text, Pri.High);
			return;
		}
		Camera cam = GameRefs.Cam;
		if (!Cpp.Alive((UnityEngine.Object)(object)cam))
		{
			Speaker.Say("No camera.", Pri.High);
			return;
		}
		try
		{
			RaycastHit val = default(RaycastHit);
			if (!Physics.Raycast(((Component)cam).transform.position, ((Component)cam).transform.forward, out val, 30f, -1, (QueryTriggerInteraction)1))
			{
				Speaker.Say("Nothing ahead.", Pri.High);
				return;
			}
			GameObject val2 = (((UnityEngine.Object)(object)val.collider != (UnityEngine.Object)null) ? ((Component)val.collider).gameObject : null);
			if ((UnityEngine.Object)(object)val2 == (UnityEngine.Object)null)
			{
				Speaker.Say("Nothing ahead.", Pri.High);
				return;
			}
			string text2 = null;
			try
			{
				InteractiveItem componentInParent = val2.GetComponentInParent<InteractiveItem>();
				if (Cpp.Alive((UnityEngine.Object)(object)componentInParent))
				{
					text2 = GameRefs.NameOf(componentInParent);
				}
				if (text2 == null)
				{
					Character componentInParent2 = val2.GetComponentInParent<Character>();
					if (Cpp.Alive((UnityEngine.Object)(object)componentInParent2))
					{
						text2 = GameRefs.NameOf(componentInParent2);
					}
				}
			}
			catch
			{
			}
			if (string.IsNullOrEmpty(text2))
			{
				text2 = TextUtil.Humanize(((UnityEngine.Object)val2).name);
			}
			Speaker.Say(text2 + ", " + TextUtil.Distance(val.distance) + " ahead.", Pri.High);
		}
		catch (Exception ex)
		{
			Log.Warn("LookAhead failed: " + ex.Message);
		}
	}
}
