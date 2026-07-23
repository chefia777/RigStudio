param(
    [string]$Configuration = "Debug"
)

Write-Host "Building Sprite Rig Studio ($Configuration)..." -ForegroundColor Cyan

dotnet build "$PSScriptRoot\..\SpriteRigStudio.sln" -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Build succeeded." -ForegroundColor Green
