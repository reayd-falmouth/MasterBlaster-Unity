# GitHub Issue: Shop player coins display bug

**Create this issue at:** https://github.com/reayd-falmouth/MasterBlaster-Unity/issues/new

## Title
Shop: Player 1 always shows 2 coins; Player 2 coins not updating correctly

## Labels (optional)
`bug`, `shop`

## Body

**Description**

- Player 1's coin count in the shop always shows **2** regardless of actual coins earned.
- Player 2 often shows **no coins** (or a stale count) when they enter the shop.

**Cause**

- The Shop scene has two pre-made coin UI objects as children of the coin container (placeholders).
- `ShopController.RefreshCoinsDisplay()` uses `Destroy()` to clear the container. In Unity, `Destroy()` is deferred to the end of the frame, so when the code then adds the correct number of coins from `SessionManager`, the two placeholders are still present. Result: Player 1 always sees at least 2 coins, and when switching to Player 2 the display can show the wrong count until the next frame.

**Expected**

- The number of coin images shown should always match `SessionManager.GetCoins(currentPlayer)` for the current player.
- When switching to Player 2, their actual coin count (e.g. 0) should be shown immediately.

**Fix (in branch)**

- In `RefreshCoinsDisplay()`, use `DestroyImmediate()` when clearing the coin container so placeholders are removed before repopulating, and add a null check for the container.
