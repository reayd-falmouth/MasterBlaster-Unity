# Fix: Menu immediately advancing to countdown

Fixes #ISSUE_NUMBER (create the issue from `.github/MENU_BUG_ISSUE_BODY.md` and replace ISSUE_NUMBER)

## Problem

From the title screen, a button press correctly goes to the menu, but the menu then immediately advances to the countdown and starts the game.

## Cause

The Menu scene also uses `ContinueOnAnyInput`. The same key press (or key repeat) is detected on the first frame(s) after loading Menu, so `SignalScreenDone()` runs again and triggers `SignalMenuStart()` → Countdown.

## Change

- Only allow "continue on any input" on **Credits** and **Title**.
- Added `SceneFlowManager.CurrentState` and `SceneFlowManager.ShouldAdvanceOnAnyInput(FlowState)`.
- `ContinueOnAnyInput` now calls `SignalScreenDone()` only when `ShouldAdvanceOnAnyInput(CurrentState)` is true, so Menu no longer auto-advances.
- Fixed legacy input branch: `GameFlowManager.I` → `SceneFlowManager.I`.
- Null check: do not advance if `SceneFlowManager.I` is null.

## Tests

New EditMode tests in `SceneFlowManagerTests.cs` for `ShouldAdvanceOnAnyInput`: Credits and Title return true; Menu, Countdown, and all other states return false.

## Branch

Push manually if needed:
```bash
git push -u origin fix/menu-immediate-countdown
```
Then create the PR from this branch into `main` (or `develop`).
