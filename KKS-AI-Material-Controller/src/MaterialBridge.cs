using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MaterialEditorAPI;
using UnityEngine;

namespace KKSAIMaterialController
{
    // Only the public MaterialEditor edit service writes to the live material.
    // Never silently fall back to Material.SetFloat/SetColor: those edits do not save to cards.
    internal sealed class MaterialBridge
    {
        private readonly MaterialEditorEditService service;
        private readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

        internal MaterialBridge(MaterialEditorEditService editService)
        {
            service = editService;
        }

        internal bool IsReady { get { return service != null; } }

        private MethodInfo Find(string methodName, Type thirdType)
        {
            var key = methodName + ":" + thirdType.FullName;
            MethodInfo result;
            if (methods.TryGetValue(key, out result)) return result;
            result = service.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m =>
            {
                if (m.Name != methodName) return false;
                var a = m.GetParameters();
                return a.Length == 3 && a[0].ParameterType == typeof(Material) &&
                       a[1].ParameterType == typeof(string) && a[2].ParameterType == thirdType;
            });
            methods[key] = result;
            return result;
        }

        internal bool CanWrite(string kind)
        {
            if (!IsReady) return false;
            if (kind == "Float" || kind == "Range") return Find("SetFloat", typeof(float)) != null;
            if (kind == "Color") return Find("SetColor", typeof(Color)) != null;
            if (kind == "Vector") return Find("SetVector", typeof(Vector4)) != null;
            return false;
        }

        internal void Set(Material material, PropertySnapshot p)
        {
            if (!IsReady || !CanWrite(p.type)) throw new InvalidOperationException("EditService has no setter for " + p.type);
            object argument;
            string method;
            Type type;
            switch (p.type)
            {
                case "Float": case "Range":
                    method = "SetFloat"; type = typeof(float); argument = p.number; break;
                case "Color":
                    method = "SetColor"; type = typeof(Color);
                    argument = new Color(p.vector[0], p.vector[1], p.vector[2], p.vector[3]); break;
                case "Vector":
                    method = "SetVector"; type = typeof(Vector4);
                    argument = new Vector4(p.vector[0], p.vector[1], p.vector[2], p.vector[3]); break;
                default: throw new NotSupportedException("Read-only property type: " + p.type);
            }
            try { Find(method, type).Invoke(service, new object[] { material, p.name, argument }); }
            catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
        }

        internal static void Validate(Material material, MaterialSnapshot snapshot, MaterialBridge bridge)
        {
            if (material == null || material.shader == null) throw new InvalidOperationException("No selected material.");
            if (snapshot == null || snapshot.schemaVersion != 1 || snapshot.properties == null)
                throw new FormatException("Unsupported JSON schema (expected version 1).");
            if (snapshot.shaderName != material.shader.name || snapshot.materialName != material.name)
                throw new InvalidOperationException("Material/Shader mismatch. Selected=" + material.name + "/" + material.shader.name);
            var live = MaterialSnapshot.Capture(material).properties.ToDictionary(p => p.name, StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in snapshot.properties)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.name) || !seen.Add(p.name))
                    throw new FormatException("Null/duplicate property name.");
                PropertySnapshot current;
                if (!live.TryGetValue(p.name, out current)) throw new InvalidOperationException("Unknown property " + p.name);
                if (p.type != current.type) throw new InvalidOperationException("Type mismatch: " + p.name);
                if (p.type == "Texture") continue; // Metadata only; never overwrite original image data.
                if (!bridge.CanWrite(p.type)) throw new InvalidOperationException("Setter not available: " + p.type + " / " + p.name);
                if (p.type == "Range" || p.type == "Float")
                {
                    if (!Numeric.Finite(p.number)) throw new FormatException("Bad value: " + p.name);
                    if (current.hasRange && (p.number < current.min || p.number > current.max))
                        throw new FormatException("Out of range: " + p.name + " [" + current.min + "," + current.max + "]");
                }
                else
                {
                    if (p.vector == null || p.vector.Length != 4 || p.vector.Any(x => !Numeric.Finite(x)))
                        throw new FormatException("Bad four-component value: " + p.name);
                }
            }
        }

        internal void ApplyValidated(Material material, MaterialSnapshot snapshot)
        {
            Validate(material, snapshot, this);
            foreach (var p in snapshot.properties)
                if (p.type != "Texture") Set(material, p);
        }
    }
}
