using UnityEngine;

namespace LMPro
{
    public static class GameObjectExt
    {
        public static void SetLayerAllChildren(this Transform root, int layer)
        {
            var children = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
                child.gameObject.layer = layer;
        }
    }
}