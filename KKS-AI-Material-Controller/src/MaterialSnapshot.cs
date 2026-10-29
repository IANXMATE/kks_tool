using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace KKSAIMaterialController
{
    [Serializable]
    public sealed class MaterialSnapshot
    {
        public int schemaVersion = 1;
        public string source = "KKS-AI-Material-Controller";
        public string materialName;
        public string shaderName;
        public PropertySnapshot[] properties;

        public static MaterialSnapshot Capture(Material material)
        {
            if (material == null || material.shader == null)
                throw new InvalidOperationException("No material / shader is selected.");
            var entries = new List<PropertySnapshot>();
            var shader = material.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                var name = shader.GetPropertyName(i);
                if (!material.HasProperty(name)) continue;
                var kind = shader.GetPropertyType(i);
                var p = new PropertySnapshot { name = name };
                switch (kind)
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        p.type = kind == ShaderPropertyType.Range ? "Range" : "Float";
                        p.number = material.GetFloat(name);
                        if (kind == ShaderPropertyType.Range)
                        {
                            var limits = shader.GetPropertyRangeLimits(i);
                            p.min = limits.x;
                            p.max = limits.y;
                            p.hasRange = true;
                        }
                        break;
                    case ShaderPropertyType.Color:
                        p.type = "Color";
                        p.vector = Encode(material.GetColor(name));
                        break;
                    case ShaderPropertyType.Vector:
                        p.type = "Vector";
                        p.vector = Encode(material.GetVector(name));
                        break;
                    case ShaderPropertyType.Texture:
                        p.type = "Texture";
                        var texture = material.GetTexture(name);
                        p.textureName = texture == null ? null : texture.name;
                        p.textureWidth = texture == null ? 0 : texture.width;
                        p.textureHeight = texture == null ? 0 : texture.height;
                        var offset = material.GetTextureOffset(name);
                        var scale = material.GetTextureScale(name);
                        p.offset = new[] { offset.x, offset.y };
                        p.scale = new[] { scale.x, scale.y };
                        break;
                    default: continue;
                }
                entries.Add(p);
            }
            return new MaterialSnapshot { materialName = material.name, shaderName = shader.name, properties = entries.ToArray() };
        }

        private static float[] Encode(Color c) { return new[] { c.r, c.g, c.b, c.a }; }
        private static float[] Encode(Vector4 c) { return new[] { c.x, c.y, c.z, c.w }; }
    }

    [Serializable]
    public sealed class PropertySnapshot
    {
        public string name;
        public string type;
        public float number;
        public float[] vector;
        public bool hasRange;
        public float min;
        public float max;
        public string textureName;
        public int textureWidth;
        public int textureHeight;
        public float[] offset;
        public float[] scale;
    }

    internal static class Numeric
    {
        internal static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        internal static string Text(float f) { return f.ToString("0.######", Culture); }
        internal static float Parse(string s)
        {
            float n;
            if (!float.TryParse(s, NumberStyles.Float, Culture, out n) || float.IsNaN(n) || float.IsInfinity(n) || Math.Abs(n) > 1000000f)
                throw new FormatException("Invalid float: " + s + " (use decimal point '.')");
            return n;
        }
        internal static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f) && Math.Abs(f) <= 1000000f; }
    }
}
