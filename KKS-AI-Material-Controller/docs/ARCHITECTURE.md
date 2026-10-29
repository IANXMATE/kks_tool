# Architecture

## Runtime

- `Plugin.cs`: BepInEx lifecycle, semantic MaterialEditor selection events, IMGUI panel, JSON IO, and undo snapshots.
- `MaterialSnapshot.cs`: enumerates shader-declared property metadata from live Unity material and stores typed data.
- `MaterialBridge.cs`: validates property existence/type and delegates writes **only** to MaterialEditorEditService (`SetFloat`, `SetColor`, `SetVector`) after discovering available public methods. No UI hooking or direct non-persistent Material.SetFloat fallback.
- `tools/clipboard_to_json.py`: offline converter for StarPlugins MEClipboard XML **modified** scalar/color values; never embeds texture bytes in JSON.
- `tools/validate_snapshot.py`: offline schema validation.

## Persistence

1. User selects a material in MaterialEditor and extension receives `args.Context.Material` + `args.Context.EditService`.
2. UI reads current Unity Material values and shader-declared properties.
3. A proposed change is validated before it is sent through the MaterialEditor public facade.
4. User saves a card through the **existing game UI**; plugin does not write or alter card PNG directly.

## Asset safety

- Material identity and shader name must match on JSON import.
- JSON input filenames are restricted to the plugin config directory.
- No arbitrary external shader files / remote scripts / code execution.
- Texture readback is a display preview, not the source texture and never injected into the game in v0.1.
- No third-party card/texture/game DLL is bundled with this repository.

## Known gaps

- UI binding depends on MaterialEditor 5.x semantic selection event availability and correct game build. Test against installed DLL.
- Shader properties that require special modes (keywords, blending, render state, texture UV control) require a later adapter.
- Values are not semantic PBR conversions: their meaning follows the selected shader exactly.
