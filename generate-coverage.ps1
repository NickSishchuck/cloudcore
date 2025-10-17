# ------------------------------
# Cross-platform Coverage Generator
# ------------------------------

param(
    [string]$TestProject = "backend/CloudCore.Tests/CloudCore.Tests.csproj",
    [string]$CoverageDir = "backend/CloudCore.Tests/coverage-report"
)


Write-Host "Cleaning old coverage results..."
Remove-Item -Recurse -Force "$CoverageDir" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$($TestProject | Split-Path)/TestResults" -ErrorAction SilentlyContinue


Write-Host "Running tests with code coverage..."
dotnet test $TestProject --collect:"XPlat Code Coverage"


$coverageFile = Get-ChildItem -Recurse -Path "$(Split-Path $TestProject)" -Filter "coverage.cobertura.xml" |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $coverageFile) {
    Write-Host "❌ coverage.cobertura.xml not found"
    exit 1
}


Write-Host "Generating HTML report..."
reportgenerator -reports:$coverageFile.FullName -targetdir:$CoverageDir -reporttypes:"HtmlSummary;HtmlChart"


try {
    if ($IsWindows) {
        Start-Process "$CoverageDir/index.html"
    } else {
        # Linux/macOS
        if (Get-Command xdg-open -ErrorAction SilentlyContinue) {
            xdg-open "$CoverageDir/index.html"
        } elseif (Get-Command open -ErrorAction SilentlyContinue) {
            open "$CoverageDir/index.html"
        }
    }
} catch {
    Write-Host "⚠️ Could not open browser automatically. Open manually: $CoverageDir/index.html"
}

Write-Host "✅ Coverage report is ready: $CoverageDir/index.html"