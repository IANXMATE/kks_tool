# Roadmap

## v0.1 — local prototype (source in this repository)

- [x] Maker MaterialEditor selection integration source
- [x] Enumerate all shader-declared property types (Float, Range, Color, Vector, Texture metadata)
- [x] Numeric / color / vector editor and persistent edit-service writes
- [x] JSON export, import validation and undo
- [x] GPU texture preview export
- [x] Offline XML-to-JSON converter and validation tests
- [ ] Compile on the user's KKS/.NET Framework 4.6 environment
- [ ] In-game test with `KKUTSalpha / cf_m_panst_02`
- [ ] Test card save/reload and coordinate switching

## v0.2 — verified multi-material

- [ ] Import PNG via MaterialEditor texture API with clear source-texture semantics
- [ ] Edit texture tiling/offset and keyword flags, persistent only
- [ ] Improve error/undo journal and batch preview/diff
- [ ] Material selector list when MaterialEditor has no active selection
- [ ] Expanding/collapsing property groups, numeric sliders with ranges

## v0.3 — standardized AI material library

- [ ] Shader schema snapshots with verified ranges and capabilities
- [ ] Versioned adapters for KKUTSalpha / Az Standard / Cetol
- [ ] Mesh/renderer state export and guarded writes
- [ ] AI-driven changes via local file handoff or optional explicit local service

**Not planned:** silently modifying authors' zipmods, uploading user assets, remotely running AI-generated code inside Unity.
