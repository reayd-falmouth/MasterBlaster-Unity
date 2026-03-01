# Scripts

## Run tests with code coverage

1. **Close the Unity Editor** (only one Unity instance can use the project at a time). The script checks for this and exits with a clear message if Unity is running.
2. From the project root, run:
   ```powershell
   .\scripts\run_tests_with_coverage.ps1
   ```
3. Open `CoverageResults\Report\index.html` for the HTML coverage report.
4. Coverage is generated for the **Scripts** assembly (game code under `Assets/Scripts`); the **Tests** assembly is excluded.

To reach high or 100% coverage, add EditMode (and optionally PlayMode) tests for any uncovered types. Pure logic (e.g. `SceneFlowMapper`, `ArenaLogic`, `SessionManager`) is easiest to cover; MonoBehaviours may require more setup or PlayMode tests.
