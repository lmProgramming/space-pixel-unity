using UnityEngine;

namespace ShipFactory
{
    public class ModuleOverlay : MonoBehaviour
    {
        public static readonly Color NormalColor = new(0f, 0f, 0f, 0f);
        public static readonly Color HoverColor = new(0f, 0.898f, 1f, 0.15f);
        public static readonly Color SelectedColor = new(1f, 0.706f, 0f, 0.2f);
        public static readonly Color InsideOtherColor = new(1f, 0.2f, 0.2f, 0.3f);
        public static readonly Color OutsideShipColor = new(1f, 0.392f, 0.196f, 0.2f);

        private static Sprite _sharedSprite;
        private SpriteRenderer _renderer;

        public static ModuleOverlay Create(ShipModuleSOInstanceBundle bundle, Transform parent, int sortingOrder)
        {
            var go = new GameObject($"Overlay_{bundle.ModuleSO.Name}");
            go.transform.SetParent(parent, false);
            SyncTransformFromBundle(go.transform, bundle);

            var overlay = go.AddComponent<ModuleOverlay>();
            overlay._renderer = go.AddComponent<SpriteRenderer>();
            overlay._renderer.sprite = GetOrCreateSprite();
            overlay._renderer.sortingOrder = sortingOrder;
            overlay._renderer.color = NormalColor;

            var dims = bundle.ModuleSO.Dimensions;
            go.transform.localScale = new Vector3(dims.x, dims.y, 1f);

            return overlay;
        }

        public void SetColor(Color color)
        {
            _renderer.color = color;
        }

        public static void SyncTransformFromBundle(Transform overlayTransform, ShipModuleSOInstanceBundle bundle)
        {
            overlayTransform.position = bundle.Instance.transform.position;
            overlayTransform.rotation = bundle.Instance.transform.rotation;
        }

        private static Sprite GetOrCreateSprite()
        {
            if (_sharedSprite != null) return _sharedSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _sharedSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _sharedSprite;
        }
    }
}