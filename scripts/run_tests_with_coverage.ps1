# Run Unity EditMode tests with code coverage.
# Only one Unity instance can have the project open. Close the Editor before running.
# Output: CoverageResults/ (HTML report) and TestResults-editmode.xml

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

# Block if Unity is running so we get a clear message instead of "another instance"
$unityProcesses = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
if ($unityProcesses) {
    Write-Host "Unity Editor is running ($($unityProcesses.Count) process(es))." -ForegroundColor Yellow
    Write-Host "Please close the Unity Editor completely, then run this script again." -ForegroundColor Yellow
    exit 1
}

# Remove stale lock file if Unity crashed or was closed without releasing it
$lockFile = "$projectRoot\Temp\UnityLockfile"
if (Test-Path $lockFile) {
    Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
    Write-Host "Removed stale Unity lock file."
}

$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
$version = (Get-Content "$projectRoot\ProjectSettings\ProjectVersion.txt" | Where-Object { $_ -match "m_EditorVersion:" }) -replace "m_EditorVersion:\s*", ""
if ($version) {
    $altPath = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (Test-Path $altPath) { $unity = $altPath }
}

$coveragePath = "$projectRoot\CoverageResults"
New-Item -ItemType Directory -Force -Path $coveragePath | Out-Null

Write-Host "Project: $projectRoot"
Write-Host "Coverage output: $coveragePath"
Write-Host "Running EditMode tests with coverage (this may take several minutes)..."
$args = @(
    "-batchmode",
    "-projectPath", $projectRoot,
    "-runTests", "-testPlatform", "editmode",
    "-testResults", "$projectRoot\TestResults-editmode.xml",
    "-enableCodeCoverage", "-coverageResultsPath", $coveragePath,
    "-debugCodeOptimization",
    "-coverageOptions", "generateHtmlReport;generateAdditionalMetrics;assemblyFilters:+Scripts;pathFilters:-**/Tests/**",
    "-logFile", "$projectRoot\unity_coverage_log.txt",
    "-quit"
)
& $unity @args
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    Write-Host "Unity exited with code $exitCode. Check unity_coverage_log.txt for details." -ForegroundColor Red
    exit $exitCode
}
$reportPath = "$coveragePath\Report\index.html"
if (Test-Path $reportPath) {
    Write-Host "Done. Coverage report: $reportPath" -ForegroundColor Green
} else {
    Write-Host "Tests finished but report not found at $reportPath. Check unity_coverage_log.txt." -ForegroundColor Yellow
}
