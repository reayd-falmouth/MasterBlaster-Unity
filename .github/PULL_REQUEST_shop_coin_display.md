# Pull request: Fix shop player coins display

**Branch:** `fix/shop-player-coins-display` → `main`

**Open PR (after pushing):** https://github.com/reayd-falmouth/MasterBlaster-Unity/compare/main...fix/shop-player-coins-display?expand=1

---

## Summary

Fixes the bug where Player 1 always saw 2 coins in the shop regardless of actual coins, and Player 2 often showed no (or stale) coins.

## Cause

- The Shop scene has two pre-made coin UI objects as children of the coin container.
- `RefreshCoinsDisplay()` used `Destroy()`, which is deferred to end of frame, so when the code added the correct number of coins from `SessionManager`, the two placeholders stayed visible. When switching to Player 2, the display could show the wrong count until the next frame.

## Changes

- **ShopController.RefreshCoinsDisplay()**: Use `DestroyImmediate()` when clearing the coin container so scene placeholders are removed before repopulating. Add a null check for `coinContainer`.
- **ShopController**: Add public static `GetCoinsToDisplayForPlayer(int playerId)` and use it in `RefreshCoinsDisplay()` for testability.
- **ShopControllerTests**: Add SetUp/TearDown with SessionManager; add tests for `GetCoinsToDisplayForPlayer` (returns SessionManager coins, returns 0 when SessionManager is null).
- **TESTING.md**: Document ShopController coin-display coverage.
- **.github/ISSUE_shop_coin_display.md**: Issue body for tracking (create the issue on GitHub from this if not already created).

## Testing

- Existing SessionManager tests unchanged.
- New/updated EditMode tests in `ShopControllerTests` for pointer logic and coin-display logic. Run via Unity Test Runner (EditMode).

## Issue

Create the issue from `.github/ISSUE_shop_coin_display.md` if needed, then add **Fixes #XX** to this PR description so the issue closes on merge.
