@echo off
setlocal
cd /d "%~dp0"
if not exist "deps" mkdir "deps"
if not exist "deps\KK_Plugins\src\MaterialEditor.KKS\KKS.MaterialEditor.csproj" (
    git clone --depth 1 https://github.com/IllusionMods/KK_Plugins.git deps\KK_Plugins
    if errorlevel 1 exit /b 1
)
echo [1/2] Restore packages via official IllusionMods and BepInEx feeds...
dotnet restore KKS-AI-Material-Controller.csproj --configfile NuGet.Config
if errorlevel 1 exit /b 1
echo [2/2] Build release...
dotnet build KKS-AI-Material-Controller.csproj -c Release --no-restore
if errorlevel 1 exit /b 1
echo Output: bin\Release\KKS_AIMaterialController.dll
endlocal
