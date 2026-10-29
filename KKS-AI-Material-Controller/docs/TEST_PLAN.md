# First in-game verification checklist

Use a throwaway character; do not overwrite the author's original card.

1. Run `build.ps1` against the exact KKS install and current KKS_MaterialEditor.dll.
2. Place resulting KKS_AIMaterialController.dll inside BepInEx/plugins, reboot game, inspect LogOutput.log for "SelectionEvents registered".
3. Open Maker > Clothes > Pantyhose > MaterialEditor. Activate `cf_m_panst_02` material label. Press F8.
4. Confirm the panel shows the current material and `KKUTSalpha`.
5. Export JSON. Confirm a small file with `Float`, `Color`, and `Texture` metadata in `BepInEx/config/KKS-AIMaterialController`.
6. Try a reversible change to `MatCap` (e.g. from 0.7 to 0.65). If `Apply` is disabled, see BepInEx log: selected MaterialEditor version does not expose the required setter.
7. Click Undo; compare appearance and exported parameter value.
8. Edit a generated JSON: change only MatCap to the original value, save as inbox.json, import.
9. Save a *new* coordinate card and reload it. Confirm MaterialEditor persisted the edited value. If not, report the logs and exact MaterialEditor version.
10. Test shader mismatch by selecting another material and importing previous inbox.json: it must refuse to apply anything.
11. Test unknown property in JSON: it must refuse to apply anything.
12. Test a Texture PNG preview: confirm file is created; do not expect original PNG bit identity.

**Report to developer**: first compiler error (if any), BepInEx load lines, current material/shader, and whether step 9 persists. Do not publicly upload author PNG cards, texture files, or game DLLs.
