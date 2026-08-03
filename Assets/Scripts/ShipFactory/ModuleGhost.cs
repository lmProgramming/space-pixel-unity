using System;
using Core.ShipFactory;
using Core.Ships.Blueprints;
using Pixelation;
using ShipFactory.Helpers;
using UnityEngine;

namespace ShipFactory
{
    public class ModuleGhost : MonoBehaviour
    {
        public static readonly Color GhostColor = new(0.55f, 0.75f, 1f, 0.35f);

        private SpriteRenderer _renderer;

        public ModuleBlueprint Blueprint { get; private set; }
        public ShipModuleSO ModuleSO { get; private set; }

        public static ModuleGhost Create(ModuleBlueprint blueprint, ShipModuleSO moduleSO, Transform parent,
            int sortingOrder)
        {
            if (blueprint == null) throw new ArgumentNullException(nameof(blueprint));
            if (!moduleSO) throw new ArgumentNullException(nameof(moduleSO));

            var go = new GameObject($"Ghost_{moduleSO.Name}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = blueprint.localPosition;
            go.transform.localRotation = blueprint.localRotation;

            var ghost = go.AddComponent<ModuleGhost>();
            ghost.Blueprint = blueprint;
            ghost.ModuleSO = moduleSO;
            ghost._renderer = go.AddComponent<SpriteRenderer>();

            var body = moduleSO.Prefab ? moduleSO.Prefab.GetComponent<PixelatedRigidbody>() : null;
            if (body != null && body.GetSprite())
            {
                ghost._renderer.sprite = body.GetSprite();
            }
            else
            {
                ghost._renderer.sprite = CreateFallbackSprite();
                var dims = moduleSO.Dimensions;
                go.transform.localScale = new Vector3(dims.x, dims.y, 1f);
            }

            ghost._renderer.color = GhostColor;
            ghost._renderer.sortingOrder = sortingOrder;
            return ghost;
        }

        public (Vector2 min, Vector2 max) GetAxisAlignedBounds()
        {
            return ModuleRotationUtility.GetFootprintBoundsInParentSpace(
                transform.position,
                ModuleSO.Dimensions,
                transform.rotation);
        }

        public bool ContainsWorldPoint(Vector2 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            var half = (Vector2)ModuleSO.Dimensions * 0.5f;
            const float edgeEpsilon = 0.001f;
            return Mathf.Abs(local.x) <= half.x + edgeEpsilon && Mathf.Abs(local.y) <= half.y + edgeEpsilon;
        }

        public void SetSelected(bool selected)
        {
            _renderer.color = selected
                ? new Color(1f, 0.85f, 0.3f, 0.45f)
                : GhostColor;
        }

        private static Sprite CreateFallbackSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}