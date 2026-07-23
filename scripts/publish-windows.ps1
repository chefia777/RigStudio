param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "0.1.0"
)

$outputDir = "$PSScriptRoot\..\artifacts\publish\windows-x64"
$archiveName = "SpriteRigStudio-win-x64-$Version.zip"

Write-Host "Publishing Sprite Rig Studio $Version ($Configuration, $Runtime)..." -ForegroundColor Cyan

# Publish Desktop
dotnet publish "$PSScriptRoot\..\src\SpriteRigStudio.Desktop\SpriteRigStudio.Desktop.csproj" `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$outputDir"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Desktop publish failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

# Publish CLI
dotnet publish "$PSScriptRoot\..\src\SpriteRigStudio.Cli\SpriteRigStudio.Cli.csproj" `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -o "$outputDir"

if ($LASTEXITCODE -ne 0) {
    Write-Host "CLI publish failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Publishing succeeded." -ForegroundColor Green
Write-Host "Output: $outputDir"
