using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using MaterialEditorAPI;
using UnityEngine;

namespace KKSAIMaterialController
{
    [BepInPlugin(Guid, "KKS AI Material Controller", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "org.kks.aimaterialcontroller";
        private ConfigEntry<KeyboardShortcut> toggleKey;
        private IDisposable selectionHook;
        private Material selected;
        private MaterialBridge bridge;
        private bool shown;
        private bool registered;
        private string message = "Choose a material in MaterialEditor to start.";
        private string search = "";
        private Rect windowRect = new Rect(80, 80, 650, 710);
        private Vector2 scroll;
        private readonly Dictionary<string, string[]> editBuffers = new Dictionary<string, string[]>();
        private readonly Stack<MaterialSnapshot> undo = new Stack<MaterialSnapshot>();
        private const int MaxUndo = 12;
        private string fileRoot;
        private string importName = "inbox.json";
        private bool showTextures = true;

        private void Awake()
        {
            toggleKey = Config.Bind("UI", "Toggle window", new KeyboardShortcut(KeyCode.F8), "Open the material controller panel.");
            fileRoot = Path.Combine(Paths.ConfigPath, "KKS-AIMaterialController");
            Directory.CreateDirectory(fileRoot);
            Logger.LogInfo("Ready. Select a material in MaterialEditor, then press F8.");
        }

        private void Update()
        {
            if (toggleKey.Value.IsDown()) shown = !shown;
            if (selected == null) { selected = null; bridge = null; }
            if (registered) return;
            try
            {
                var required = MaterialEditorApiCapability.SelectionEvents | MaterialEditorApiCapability.EditServiceFacade;
                if (!MaterialEditorExtensionApi.Supports(required)) return;
                selectionHook = MaterialEditorExtensionApi.RegisterSelectionHandler(OnSelection);
                registered = selectionHook != null;
                if (registered) Logger.LogInfo("MaterialEditor SelectionEvents registered.");
            }
            catch (Exception ex)
            {
                // Avoid spamming the log on every frame if API initialization failed.
                registered = true;
                Logger.LogError("Cannot subscribe to MaterialEditor events: " + ex);
                message = "Selection API unavailable. See BepInEx log.";
            }
        }

        private void OnSelection(MaterialEditorSelectionEventArgs args)
        {
            try
            {
                if (args == null) return;
                var material = args.Context.Material;
                if (args.Action == MaterialEditorSelectionAction.Deselected)
                {
                    if (ReferenceEquals(material, selected)) ClearSelection();
                    return;
                }
                if (material == null || args.Context.EditService == null) return;
                if (!ReferenceEquals(material, selected))
                {
                    selected = material;
                    bridge = new MaterialBridge(args.Context.EditService);
                    editBuffers.Clear();
                    undo.Clear();
                    message = "Selected " + material.name + " (" + material.shader.name + ")";
                }
                else
                {
                    // A shader dropdown change can rebuild the bound edit service.
                    bridge = new MaterialBridge(args.Context.EditService);
                }
            }
            catch (Exception ex) { Logger.LogError("Material selection failed: " + ex); }
        }

        private void ClearSelection()
        {
            selected = null; bridge = null; editBuffers.Clear(); undo.Clear();
            message = "Select a material in MaterialEditor.";
        }

        private void OnDestroy() { selectionHook?.Dispose(); }

        private void OnGUI()
        {
            if (!shown) return;
            windowRect = GUI.Window(762830, windowRect, DrawWindow, "KKS AI Material Controller 0.1  |  Drag here");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("MaterialEditor selection + persistent edits  |  F8 to hide");
            GUILayout.Label(message);
            if (GUILayout.Button("Refresh fields (discard un-applied text)", GUILayout.Height(24))) editBuffers.Clear();
            if (selected == null || bridge == null || !bridge.IsReady)
            {
                GUILayout.Label("Open MaterialEditor, click a material row/label, then return here.");
                GUILayout.FlexibleSpace(); GUILayout.EndVertical(); GUI.DragWindow(new Rect(0, 0, 650, 26)); return;
            }
            GUILayout.Label("Material: " + selected.name + "    Shader: " + selected.shader.name);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export JSON", GUILayout.Width(115))) Safe(Export);
            if (GUILayout.Button("Import JSON", GUILayout.Width(115))) Safe(Import);
            if (GUILayout.Button("Undo", GUILayout.Width(80))) Safe(Undo);
            if (GUILayout.Button("Texture preview PNG", GUILayout.Width(150))) showTextures = !showTextures;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Import file in config/KKS-AIMaterialController:", GUILayout.Width(315));
            importName = GUILayout.TextField(importName, GUILayout.MinWidth(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(48));
            search = GUILayout.TextField(search);
            GUILayout.EndHorizontal();
            GUILayout.Label("Changes use MaterialEditor EditService; unsupported fields remain read-only.");
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(490));
            try
            {
                var snapshot = MaterialSnapshot.Capture(selected);
                foreach (var p in snapshot.properties)
                {
                    if (search.Length > 0 && p.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (p.type == "Texture" && !showTextures) continue;
                    DrawProperty(p);
                }
            }
            catch (Exception ex) { GUILayout.Label("Cannot inspect material: " + ex.Message); }
            GUILayout.EndScrollView();
            GUILayout.Label("Export folder: " + fileRoot);
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 650, 26));
        }

        private void DrawProperty(PropertySnapshot p)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(p.name + " (" + p.type + ")", GUILayout.Width(284));
            if (p.type == "Texture")
            {
                GUILayout.Label((p.textureName ?? "<none>") + " " + p.textureWidth + "x" + p.textureHeight);
                if (GUILayout.Button("PNG preview", GUILayout.Width(105))) Safe(() => ExportTexture(p.name));
            }
            else
            {
                var buffer = GetBuffer(p);
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = GUILayout.TextField(buffer[i], GUILayout.Width(buffer.Length == 1 ? 93 : 65));
                GUI.enabled = bridge.CanWrite(p.type);
                if (GUILayout.Button("Apply", GUILayout.Width(63))) Safe(() => ApplyOne(p, buffer));
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            if (p.hasRange) GUILayout.Label("Valid range: " + Numeric.Text(p.min) + " .. " + Numeric.Text(p.max));
            if (p.type == "Texture")
                GUILayout.Label("Texture slot metadata + UV offset/scale exported; modification not enabled in v0.1.");
            GUILayout.EndVertical();
        }

        private string[] GetBuffer(PropertySnapshot p)
        {
            string[] values;
            if (editBuffers.TryGetValue(p.name, out values)) return values;
            values = p.type == "Color" || p.type == "Vector"
                ? p.vector.Select(Numeric.Text).ToArray() : new[] { Numeric.Text(p.number) };
            editBuffers[p.name] = values;
            return values;
        }

        private void SaveUndo()
        {
            if (undo.Count >= MaxUndo)
            {
                var keep = undo.ToArray().Take(MaxUndo - 1).Reverse().ToArray();
                undo.Clear(); foreach (var item in keep) undo.Push(item);
            }
            undo.Push(MaterialSnapshot.Capture(selected));
        }

        private void ApplyOne(PropertySnapshot old, string[] buffer)
        {
            var next = new PropertySnapshot { name = old.name, type = old.type, hasRange = old.hasRange, min = old.min, max = old.max };
            if (old.type == "Float" || old.type == "Range")
                next.number = Numeric.Parse(buffer[0]);
            else next.vector = buffer.Select(Numeric.Parse).ToArray();
            // Validate even single-property writes before mutating the live material.
            MaterialBridge.Validate(selected, new MaterialSnapshot
            {
                shaderName = selected.shader.name, materialName = selected.name,
                properties = new[] { next }
            }, bridge);
            SaveUndo();
            bridge.Set(selected, next);
            message = "Applied " + old.name + " (saved via MaterialEditor repository).";
            editBuffers.Remove(old.name);
        }

        private string SafeFile(string filename)
        {
            // Only named JSON files inside our dedicated folder. No arbitrary file reads.
            if (string.IsNullOrWhiteSpace(filename) || filename != Path.GetFileName(filename) ||
                !filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Import only a .json file name inside " + fileRoot);
            return Path.Combine(fileRoot, filename);
        }

        private void Export()
        {
            var data = MaterialSnapshot.Capture(selected);
            var path = SafeFile("material-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json, new UTF8Encoding(false));
            message = "Exported: " + Path.GetFileName(path);
        }

        private void Import()
        {
            var path = SafeFile(importName);
            if (!File.Exists(path)) throw new FileNotFoundException("Place your JSON in: " + path);
            var file = new FileInfo(path);
            if (file.Length > 1024 * 1024) throw new InvalidOperationException("JSON limit is 1 MB. Texture data is intentionally excluded.");
            var incoming = JsonUtility.FromJson<MaterialSnapshot>(File.ReadAllText(path, Encoding.UTF8));
            MaterialBridge.Validate(selected, incoming, bridge);
            var before = MaterialSnapshot.Capture(selected);
            SaveUndo();
            try { bridge.ApplyValidated(selected, incoming); }
            catch
            {
                try { bridge.ApplyValidated(selected, before); }
                catch (Exception rollback) { Logger.LogError("Rollback failed: " + rollback); }
                throw;
            }
            editBuffers.Clear();
            message = "Imported " + incoming.properties.Length + " properties (texture metadata left untouched).";
        }

        private void Undo()
        {
            if (undo.Count == 0) { message = "No undo snapshot."; return; }
            var saved = undo.Peek();
            bridge.ApplyValidated(selected, saved);
            undo.Pop(); editBuffers.Clear(); message = "Undo applied.";
        }

        private void ExportTexture(string slot)
        {
            var texture = selected.GetTexture(slot);
            if (texture == null) throw new InvalidOperationException("Texture slot is empty.");
            if (texture.width > 8192 || texture.height > 8192)
                throw new InvalidOperationException("Texture too large for preview readback.");
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Texture2D cpu = null;
            try
            {
                Graphics.Blit(texture, rt);
                RenderTexture.active = rt;
                cpu = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                cpu.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                cpu.Apply();
                var raw = cpu.EncodeToPNG();
                var filename = "texture-preview-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + new string(slot.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray()) + ".png";
                File.WriteAllBytes(Path.Combine(fileRoot, filename), raw);
                message = "Saved GPU preview " + filename + " (not lossless original source).";
            }
            finally
            {
                RenderTexture.active = prev;
                if (cpu != null) Destroy(cpu);
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private void Safe(Action action)
        {
            try { action(); }
            catch (Exception ex) { message = ex.Message; Logger.LogError(ex); }
        }
    }
}
