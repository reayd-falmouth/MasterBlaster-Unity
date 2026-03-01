# Enable Git hooks for this repo (run from repo root)
$ErrorActionPreference = "Stop"
$root = git rev-parse --show-toplevel 2>$null
if (-not $root) { Write-Error "Not in a Git repo."; exit 1 }
Push-Location $root
git config core.hooksPath .githooks
Write-Host "Hooks enabled. Pre-commit will run from .githooks/"
git config core.hooksPath
Pop-Location
