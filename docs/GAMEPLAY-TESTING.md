# Gameplay follow-up: 1.1.17

The supplied long gameplay run loaded House Access 1.1.16 with NVDA and all 36
hooks. It recorded many accepted wheel choices and conversations, one warning
about dialogue responses not appearing, and no mod exception entries. Its loader
warnings concerned interop generation. The separate error log contains Steam
initialization lines rather than a mod exception.

That log does not contain route coordinates, corner transitions or controller
parameters. The user identified the initial walk to Brittney as a repeatable
stair failure, and reported corner and obstacle failures elsewhere.

## Verified offline

The current game's metadata and native methods establish that:

- Translated popup overloads call `PopupManager.DisplayText`, bypassing the old
  two-string `Display` hook. `DisplayText` sets the displayed text and focuses OK.
- `DialogueUI.responsesDisplayed` exposes response readiness. Waiting for another
  canvas to close need not finish within the old four-second timeout.
- `PlayerCharacter.LateUpdate` calls `TryMoveController`. That method consumes
  `_movement`, adds falling motion, moves the controller, and clears `_movement`.
  `GetSpeed` already includes delta time and native movement modifiers.
- `IgnoringMovementInput`, `FPInput.LockPosition` and the player's cutscene state
  are available; the previous movement-lock helper always returned false.

The mod now uses those native paths. It preserves waypoint height, follows corners
within controller-derived tolerances, and stops at an unreachable route endpoint.
It no longer invents staircase destinations from room names or makes an unplanned
straight-line approach through obstacles after a failed path.

The Release build passes with zero errors and five existing warnings. A metadata
audit resolves 52 game type references, 145 member references and all 41 hook
targets with their consumed argument types.

Sixty-three local fixture checks cover popup priority and restoration, wheel
numbering and readiness, audio navigation, route geometry, and selected actual
navigator methods. Movement checks cover stale or repeated steps, native speed
limits, movement locks, manual input, popup pause, live target replanning, partial
route completion, false arrival across a wall, and oscillation detection. Fixtures
do not execute Unity physics or prove audible behavior. Inspection output and
fixtures remain local; game files and generated artifacts are not published.

## Gameplay checks still needed

Use the default bindings below, or their equivalents in the saved configuration.

1. At the beginning, select Brittney with **Ctrl+PageUp/PageDown** and press
   **End** to auto-walk. Check that the stair route advances without pushing
   downward, skipping a landing or reporting arrival on the wrong floor. Repeat
   upstairs to downstairs, and around a door frame or tight corner. Manual WASD
   should stop auto-walk immediately.
2. Open a character's wheel with **=**, then trigger a tutorial. It should read
   automatically. **F2** should read only that popup, including the end of a long
   instruction. A number should read a popup control; **Ctrl+number** should
   activate it. **Enter** should activate the native focused control.
3. Leave the tutorial open for more than 30 seconds. Close it using its native
   control. The underlying wheel should remain usable, and the dismissal key must
   not also choose an action beneath the popup. The action should not be announced
   as "not ready" merely because it opened a tutorial.
4. Trigger a tutorial during auto-walk. Movement should pause while it is open
   and resume afterward. **Comma** cancels auto-walk while leaving the popup open.
5. In dialogue, press **Up/Down** once at a time. Each press should move one reply.
   Let a voiced line or popup delay the replies; they should appear and be read
   when the game displays them. A disabled reply must not be invoked.
6. Select a moving person and auto-walk. The route should update to their current
   position. A blocked or partial route should stop with a spoken explanation,
   rather than claim arrival or oscillate indefinitely.

Keep `BepInEx/LogOutput.log` from that run. Version 1.1.17 logs route starts,
controller parameters when movement is first applied, recovery attempts, popup
ownership changes and stop reasons. Those will help distinguish a route problem
from an input lock or a collision without guessing from silence.
