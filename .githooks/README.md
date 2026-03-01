# Git hooks

Pre-commit runs **linting** (CSharpier on `Assets/`) and **Unity EditMode tests** (if Unity is found).

## Enable hooks

From the repo root:

```bash
git config core.hooksPath .githooks
```

To disable later:

```bash
git config --unset core.hooksPath
```

## Requirements

- **Lint:** .NET SDK and CSharpier (via `dotnet tool restore` from `.config/dotnet-tools.json`).
- **Tests:** Unity Editor matching `ProjectSettings/ProjectVersion.txt` (optional). If Unity is not found, the hook skips tests and still passes; CI will run them.

## Optional

- **Skip tests for a commit:** `SKIP_UNITY_TESTS=1 git commit ...`
- **Unity path:** Set `UNITY_EDITOR_PATH` (or `UNITY_HOME`) to your Unity executable so the hook can run tests (e.g. `Unity.exe` on Windows, `Unity` on macOS).
