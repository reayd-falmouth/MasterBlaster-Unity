# Unity tests

Edit Mode and Play Mode tests are in **separate assemblies** so each mode only runs its own tests (no skipped Play Mode tests in the Edit Mode run).

- **EditMode** assembly (`Tests.EditMode.asmdef` in **EditMode/**): Run with **EditMode** in the Test Runner. Use `[Test]`. No game loop; fast.
  - Examples: `SceneFlowMapperTests`, `SessionManagerTests`, `WinStateTests`, `ShopItemExtensionsTests`, `PlayerSetupTests`, `SceneNamesConfigTests`, `SingletonTests`, `ShopPurchaseLogicTests`.

- **PlayMode** assembly (`Tests.PlayMode.asmdef` in **PlayMode/**): Run with **PlayMode** in the Test Runner. Use `[UnityTest]` and return `IEnumerator`. Full game loop.
  - Examples: `CountdownControllerPlayModeTests`, `BombControllerPlayModeTests`, `SingletonPlayModeTests`.

## How to run

1. **In Editor**: **Window → General → Test Runner**. Use the **EditMode** tab for Edit Mode tests and the **PlayMode** tab for Play Mode tests; each tab only lists and runs tests from its assembly.
2. **CI**: The GitHub Actions **Test** workflow runs **Edit Mode** and **Play Mode** in separate steps, so each mode only runs its own tests (separate check runs: "Edit Mode Test Results" and "Play Mode Test Results").
3. **Local script** (`scripts/run_tests_with_coverage.ps1`): runs **Edit Mode only** (Tests.EditMode assembly).

See **TESTING.md** for which scripts are covered by unit tests vs integration/PlayMode/manual testing.
