$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$editorProj = Join-Path $root 'deps\KK_Plugins\src\MaterialEditor.KKS\KKS.MaterialEditor.csproj'
if (-not (Test-Path $editorProj)) {
    git clone --depth 1 https://github.com/IllusionMods/KK_Plugins.git (Join-Path $root 'deps\KK_Plugins')
    if ($LASTEXITCODE -ne 0) { throw 'Could not clone KK_Plugins' }
}
dotnet restore .\KKS-AI-Material-Controller.csproj --configfile .\NuGet.Config
if ($LASTEXITCODE -ne 0) { throw 'Restore failed. Check NuGet feed access.' }
dotnet build .\KKS-AI-Material-Controller.csproj -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed; inspect the first error.' }
Write-Host 'Output: bin\Release\KKS_AIMaterialController.dll'
