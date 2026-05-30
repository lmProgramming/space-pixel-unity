using Core.Services;
using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public sealed class TestContentCatalog : IGameContentCatalog
    {
        public bool TryGetPrefab(string contentId, out GameObject prefab)
        {
            prefab = null;
            return false;
        }

        public bool TryGetContentId(GameObject prefab, out string contentId)
        {
            contentId = null;
            return false;
        }

        public bool TryGetSprite(string contentId, out Sprite sprite)
        {
            sprite = null;
            return false;
        }

        public bool TryGetSpriteContentId(Sprite sprite, out string contentId)
        {
            contentId = null;
            return false;
        }
    }
}