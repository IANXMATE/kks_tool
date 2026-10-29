# KKS AI Material Controller

Experimental BepInEx 5 plugin for Koikatsu Sunshine (KKS). Select any material in MaterialEditor 5.x, inspect every shader-declared property, change supported numeric/vector/color fields through MaterialEditor's persistent edit service, and export/import small JSON snapshots for AI-assisted workflows.

> Status **v0.1.3 source prototype**. The repository is generated and schema-tested, but **not compiled against a local KKS install or in-game tested**. The `.dll` is not included. Verify build and load before using on your main game installation.

## What works in the source

- Subscribes to MaterialEditor's semantic **SelectionEvents** (does not search for private UI controls).
- Enumerates each declared shader Float / Range / Color / Vector / Texture property and current values.
- Edits Float, Range, Color, Vector via `MaterialEditorEditService`. Checks setter availability before enabling Apply. **No transient Unity `Material.Set*` fallback**.
- Export parameter snapshots to JSON (`schemaVersion=1`, shader, material name, typed values, texture slot metadata and UV offsets/scales).
- Import after checking shader identity, property existence/type, finite values and Range limits. On apply failure, attempts rollback. Undo uses snapshots (maximum 12).
- Export texture **GPU preview** PNG with readback (not source-exact texture data).
- Dedicated BepInEx config directory; no network calls or API keys.

## Not implemented yet

- Texture import/replacement, keyword editors, UV transforms persistence, shader switching, render queue and Renderer flags. These are marked read-only or excluded from import rather than pretending that an unsaved edit succeeded.
- Face / outfit-wide batch selection, automated shader migration, cross-character presets.
- No background file polling, no remote code execution, no LLM service integration.
- Studio mode selection handling has **not** been tested; focus is KKS Maker.

## Build fix v0.1.3

Fixed `EnableDefaultCompileItems` in the root `.csproj`. In v0.1.2, the SDK recursively compiled `deps/KK_Plugins/src/**/*.cs` into our project, causing unrelated `CS1525`/`CS8641` errors in ClothesToAccessories, SpawnLocker, etc. The root project now compiles *only* `src/**/*.cs`; the MaterialEditor project remains a separate ProjectReference. No changes to the upstream dependency source are required.

## Build (no game installation path required)

The project uses official **IllusionMods + BepInEx NuGet feeds** (configured in `NuGet.Config`) for BepInEx/Unity references, and a local clone of the public `KK_Plugins` source as a project reference for MaterialEditor 5.x. The first build downloads these development dependencies. **No KKS game directory, proprietary DLL copy, or `-GamePath` is needed.**

Prerequisites: Git, .NET SDK, .NET Framework 4.6 Developer Pack or targeting pack.

```powershell
# From this repository root; works even when PowerShell .ps1 scripts are disabled:
.\build.cmd
```

Or manually, to be able to use plain `dotnet build` later:

```powershell
git clone --depth 1 https://github.com/IllusionMods/KK_Plugins.git .\deps\KK_Plugins
dotnet restore --configfile .\NuGet.Config
dotnet build -c Release --no-restore
```

The output is `bin/Release/KKS_AIMaterialController.dll`. Copy only this plugin DLL to the KKS `BepInEx/plugins` directory *after* the build, and test against a game installed with compatible MaterialEditor and KKSAPI. The external source checkout is ignored by Git; pin a specific upstream commit before a reproducible release.

**Status:** development build recipe, not compiled or runtime-verified in the assistant environment. Report the first compiler error if a NuGet package or API changed.

## In-game usage

1. Enter KKS Character Maker. Open the existing **MaterialEditor** and click/activate a material row (e.g. `cf_m_panst_02`).
2. Press **F8** to show the controller. Check displayed material and Shader names match.
3. Search a property, input numbers using `.` as decimal separator, click **Apply**. All supported edits go via MaterialEditor persistence, so save the character/coordinate card using the game's normal Save command.
4. **Export JSON** writes to `BepInEx/config/KKS-AIMaterialController/material-YYYYMMDD-HHMMSS.json`.
5. Put an edited JSON named `inbox.json` in that same folder. Click **Import JSON**. The selected material and shader name MUST match the file. `Texture` entries are metadata-only and **never applied**.
6. **Undo** to revert the last application (in this session); no automatic file/card save occurs.

**Important:** MaterialEditor `Copy Edits` / Clipboard XML contains modified properties and sometimes embedded textures. This plugin's JSON contains *current shader-exposed parameters*, not those full texture bytes or MaterialEditor’s saved edit history. Editing JSON values is designed for tiny AI prompts; **only import matching material snapshots**.

## Security and recovery

- JSON input is limited to 1 MB and filename must be a `.json` basename inside the dedicated plugin folder.
- Never attempts shader replacement during import. No web/network access.
- Prevalidates schema, shader, property type, finite number values and range properties.
- Preserves original PNG cards unless you explicitly save inside the game; still make a backup first.
- If an exception occurs during a multi-property import, it tries to reapply pre-import values; inspect game log if rollback fails.

## Development / testing

```powershell
# In a KKS-enabled Windows environment
.\build.cmd
# Offline schema tests, no game required:
python -m unittest discover -s tests -v
```

References:
- [MaterialEditor Extension API](https://github.com/IllusionMods/KK_Plugins/blob/master/Guides/Material%20Editor%20Guide/Extension%20API.md)
- [MaterialEditor public API compatibility](https://github.com/IllusionMods/KK_Plugins/blob/master/Guides/Material%20Editor%20Guide/Public%20API%20Compatibility.md)

This project is an independent companion plugin, not part of IllusionMods or StarPlugins. It does not redistribute proprietary game binaries or original author textures.

## Convert an existing Clipboard XML to a lightweight JSON patch

If you already have a huge `MEClipboard-...xml`, only the edited Float/Color/Vector values can be converted. This does **not** extract the complete Shader metadata or copy author images into the repository.

```powershell
python .\tools\clipboard_to_json.py "D:\path\MEClipboard.xml" -o .\inbox.json
python .\tools\validate_snapshot.py .\inbox.json
```

Copy the resulting `inbox.json` to `BepInEx/config/KKS-AIMaterialController/` for guarded import into an identically named live material.

## Publish on GitHub

```powershell
git init
git add .
git commit -m "Initial KKS AI Material Controller prototype"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/KKS-AI-Material-Controller.git
git push -u origin main
```

Create the empty repository on GitHub first. The `.gitignore` excludes DLLs, compiled files, and image textures. Don't commit your game folder, original cards, or the full MEClipboard XML with third-party PNG textures.

## NuGet package source configuration (v0.1.2)

`IllusionLibs.BepInEx` and `IllusionLibs.KoikatsuSunshine.AllPackages` are **not** on nuget.org. `NuGet.Config` explicitly includes the feeds specified by [IllusionMods/IllusionLibs](https://github.com/IllusionMods/IllusionLibs):

- `https://nuget.bepinex.dev/v3/index.json`
- `https://pkgs.dev.azure.com/IllusionMods/Nuget/_packaging/IllusionMods/nuget/v3/index.json`

To repeat only restore, use `dotnet restore .\KKS-AI-Material-Controller.csproj --configfile .\NuGet.Config`. A NuGet error after this should be reported verbatim; package restore and the C# build have **not** been verified in this environment.
