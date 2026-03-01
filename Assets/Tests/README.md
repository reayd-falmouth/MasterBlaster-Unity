# Unity tests

## EditMode vs PlayMode

- **EditMode** (default): Run with **EditMode** selected in the Test Runner. Use `[Test]`. No game loop; fast. Use for logic that doesn’t need `Time`, physics, or coroutines.
  - Lives in **EditMode/** (e.g. `SceneFlowMapperTests`, `SessionManagerTests`, `WinStateTests`, `ShopItemExtensionsTests`, `PlayerSetupTests`, `SceneNamesConfigTests`, `SingletonTests`, `ShopPurchaseLogicTests`).

- **PlayMode**: Run with **PlayMode** selected in the Test Runner. Use `[UnityTest]` and return `IEnumerator`. Full game loop; use for code that needs `Time`, physics, or `WaitForSeconds`.
  - Lives in **PlayMode/** (e.g. `CountdownControllerPlayModeTests`, `BombControllerPlayModeTests`, `SingletonPlayModeTests`).

## How to run

1. **In Editor**: **Window → General → Test Runner** (or **Analysis → Test Runner**). Choose **EditMode** or **PlayMode**, then **Run All** (or run individual tests). PlayMode tests must be run from the **PlayMode** tab; if run in Edit Mode they are skipped with a clear message.
2. **CI**: The GitHub Actions **Test** workflow runs both EditMode and PlayMode on push/PR to `main`/`develop`.
3. **Local script** (`scripts/run_tests_with_coverage.ps1`): runs **Edit Mode only**; PlayMode tests are skipped when executed in that context.

See **TESTING.md** for which scripts are covered by unit tests vs integration/PlayMode/manual testing.
