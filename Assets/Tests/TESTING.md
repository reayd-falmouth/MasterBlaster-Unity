# Test coverage for Assets/Scripts

This document lists which scripts have **unit (EditMode) tests**, **PlayMode tests**, or are covered only by **integration / manual** testing.

## Logic-tested (EditMode)

| Script(s) | Test file |
|-----------|-----------|
| `Core/SceneNamesConfig` | SceneNamesConfigTests.cs |
| `Core/SceneFlowMapper` | SceneFlowMapperTests.cs |
| `Core/SessionManager` | SessionManagerTests.cs |
| `Scenes/Shop/ShopItemTypeExtensions`, `ShopItemType` | ShopItemExtensionsTests.cs |
| `Scenes/Shop/ShopPurchaseLogic` | ShopPurchaseLogicTests.cs |
| `Scenes/Arena/ArenaLogic`, `WinStateResult`, `PlayerSlot`, `WinOutcome` | PlayerSetupTests.cs, WinStateTests.cs |
| `Utilities/Singleton`, `PersistentSingleton` | SingletonTests.cs |

## PlayMode tests

| Script(s) | Test file |
|-----------|-----------|
| `Scenes/Arena/CountdownController` | PlayMode/CountdownControllerPlayModeTests.cs |
| `Scenes/Arena/Bomb/BombController` | PlayMode/BombControllerPlayModeTests.cs |

## Integration / PlayMode / manual only

No dedicated unit tests; covered by integration, PlayMode, or manual testing.

- **Core:** AudioController, ContinueOnAnyInput, SceneFlowManager, AnimatedSpriteRenderer
- **Scenes/Shop:** ShopController (logic in ShopPurchaseLogic is unit-tested)
- **Scenes/Arena:** GameManager, MapSelector, ArenaShrinker, ItemPickup, Destructible, Indestructible, Bomb/Explosion, Bomb/RemoteBombController
- **Scenes/Arena/Player:** PlayerController, DebugItemSpawner, PlayerDebugPlayerPrefs, Abilities (Ghost, Protection, Superman)
- **Scenes:** MainMenuController, StandingsController, WheelController, OversController (GameOver)
