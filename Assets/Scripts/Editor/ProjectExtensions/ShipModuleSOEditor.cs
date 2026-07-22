using System;
using System.Reflection;
using Core.ShipFactory;
using Pixelation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.ProjectExtensions
{
    [CustomEditor(typeof(ShipModuleSO))]
    public class ShipModuleSOEditor : UnityEditor.Editor
    {
        private ShipModuleSO Item => target as ShipModuleSO;

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var sprite = Item.Prefab?.GetComponent<PixelatedRigidbody>()?.GetSprite();

            if (sprite == null) return base.RenderStaticPreview(assetPath, subAssets, width, height);

            var t = GetType("UnityEditor.SpriteUtility");

            if (t == null) return base.RenderStaticPreview(assetPath, subAssets, width, height);

            var method = t.GetMethod("RenderStaticPreview",
                new[] { typeof(Sprite), typeof(Color), typeof(int), typeof(int) });

            if (method == null) return base.RenderStaticPreview(assetPath, subAssets, width, height);

            var ret = method.Invoke("RenderStaticPreview",
                new object[]
                    { sprite, Color.white, width, height });

            if (ret is Texture2D ret2D)
                return ret2D;

            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies)
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly == null) continue;

                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}