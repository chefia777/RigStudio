Write-Host "Verifying Sprite Rig Studio..." -ForegroundColor Cyan

$ErrorActionPreference = "Stop"

# Build
Write-Host "`n=== Build ===" -ForegroundColor Yellow
dotnet build "$PSScriptRoot\..\SpriteRigStudio.sln" -c Release

# Test
Write-Host "`n=== Tests ===" -ForegroundColor Yellow
dotnet test "$PSScriptRoot\..\SpriteRigStudio.sln" -c Release --no-build

Write-Host "`n=== Verification complete ===" -ForegroundColor Green
