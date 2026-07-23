param(
    [string]$Configuration = "Debug"
)

Write-Host "Running Sprite Rig Studio tests ($Configuration)..." -ForegroundColor Cyan

dotnet test "$PSScriptRoot\..\SpriteRigStudio.sln" -c $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "All tests passed." -ForegroundColor Green
