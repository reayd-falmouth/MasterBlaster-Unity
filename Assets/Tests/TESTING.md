# Test coverage for Assets/Scripts

This document lists which scripts have **unit (EditMode) tests**, **PlayMode tests**, or are covered only by **integration / manual** testing.

## Logic-tested (EditMode)

EditMode tests live in **EditMode/**.

| Script(s) | Test file |
|-----------|-----------|
| `Core/SceneNamesConfig` | EditMode/SceneNamesConfigTests.cs |
| `Core/SceneFlowMapper` | EditMode/SceneFlowMapperTests.cs |
| `Core/SessionManager` (upgrades and coins session-only; shop and arena use it) | EditMode/SessionManagerTests.cs |
| `Scenes/Shop/ShopItemTypeExtensions`, `ShopItemType` | EditMode/ShopItemExtensionsTests.cs |
| `Scenes/Shop/ShopPurchaseLogic` | EditMode/ShopPurchaseLogicTests.cs |
| `Scenes/Shop/ShopController` (GetPointerTextForIndex, GetCoinsToDisplayForPlayer / coin display) | EditMode/ShopControllerTests.cs |
| `Scenes/Arena/ArenaLogic`, `WinStateResult`, `PlayerSlot`, `WinOutcome` | EditMode/PlayerSetupTests.cs, EditMode/WinStateTests.cs |
| `Utilities/Singleton` | EditMode/SingletonTests.cs |
| `Core/SceneFlowManager` (ShouldAdvanceOnAnyInput, GetNextState transition logic) | EditMode/SceneFlowManagerTests.cs |

## PlayMode tests

| Script(s) | Test file |
|-----------|-----------|
| `Utilities/PersistentSingleton` | PlayMode/SingletonPlayModeTests.cs |
| `Scenes/Arena/CountdownController` | PlayMode/CountdownControllerPlayModeTests.cs |
| `Scenes/Arena/Bomb/BombController` | PlayMode/BombControllerPlayModeTests.cs |

## Integration / PlayMode / manual only

No dedicated unit tests; covered by integration, PlayMode, or manual testing.

- **Core:** AudioController, ContinueOnAnyInput, AnimatedSpriteRenderer
- **Scenes/Shop:** ShopController (pointer logic, coin-display logic, and ShopPurchaseLogic are unit-tested)
- **Scenes/Arena:** GameManager, MapSelector, ArenaShrinker, ItemPickup, Destructible, Indestructible, Bomb/Explosion, Bomb/RemoteBombController
- **Scenes/Arena/Player:** PlayerController, DebugItemSpawner, PlayerDebugPlayerPrefs, Abilities (Ghost, Protection, Superman)
- **Scenes:** MainMenuController, StandingsController, WheelController, OversController (GameOver)
