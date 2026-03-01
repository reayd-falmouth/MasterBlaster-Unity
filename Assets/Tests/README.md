# Unity tests

## EditMode vs PlayMode

- **EditMode** (default): Run with **EditMode** selected in the Test Runner. Use `[Test]`. No game loop; fast. Use for logic that doesn’t need `Time`, physics, or coroutines.
  - Examples: `SceneFlowMapperTests`, `SessionManagerTests`, `WinStateTests`, `ShopItemExtensionsTests`, `PlayerSetupTests`, `SceneNamesConfigTests`, `SingletonTests`, `ShopPurchaseLogicTests`.

- **PlayMode**: Run with **PlayMode** selected in the Test Runner. Use `[UnityTest]` and return `IEnumerator`. Full game loop; use for code that needs `Time`, physics, or `WaitForSeconds`.
  - Examples: `CountdownControllerPlayModeTests`, `BombControllerPlayModeTests` in the `PlayMode` folder.

## How to run

1. **In Editor**: **Window → General → Test Runner** (or **Analysis → Test Runner**). Choose **EditMode** or **PlayMode**, then **Run All** (or run individual tests).
2. **CI**: The GitHub Actions **Test** workflow runs both EditMode and PlayMode on push/PR to `main`/`develop`.

See **TESTING.md** for which scripts are covered by unit tests vs integration/PlayMode/manual testing.
