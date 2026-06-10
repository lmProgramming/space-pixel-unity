using System.Collections.Generic;
using Core.Services;
using UnityEngine;

namespace Ships.Tests.TestHelpers.Mocks
{
    public sealed class TestContentCatalog : IGameContentCatalog
    {
        private readonly Dictionary<string, GameObject> _idToPrefab = new();
        private readonly Dictionary<string, Sprite> _idToSprite = new();
        private readonly Dictionary<GameObject, string> _prefabToId = new();
        private readonly Dictionary<Sprite, string> _spriteToId = new();

        public bool TryGetPrefab(string contentId, out GameObject prefab)
        {
            return _idToPrefab.TryGetValue(contentId, out prefab);
        }

        public bool TryGetContentId(GameObject prefab, out string contentId)
        {
            return _prefabToId.TryGetValue(prefab, out contentId);
        }

        public bool TryGetSprite(string contentId, out Sprite sprite)
        {
            return _idToSprite.TryGetValue(contentId, out sprite);
        }

        public bool TryGetSpriteContentId(Sprite sprite, out string contentId)
        {
            return _spriteToId.TryGetValue(sprite, out contentId);
        }

        public void AddPrefab(string id, GameObject prefab)
        {
            _idToPrefab[id] = prefab;
            _prefabToId[prefab] = id;
        }

        public void AddSprite(string id, Sprite sprite)
        {
            _idToSprite[id] = sprite;
            _spriteToId[sprite] = id;
        }
    }
}