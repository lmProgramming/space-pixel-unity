using System;
using System.Collections.Generic;
using Core.Services;
using UnityEngine;
using ZLinq;

namespace Services
{
    [CreateAssetMenu(fileName = "GameContentCatalog", menuName = "Game/Content Catalog")]
    public class GameContentCatalog : ScriptableObject, IGameContentCatalog
    {
        [SerializeField] private List<PrefabEntry> entries = new();
        [SerializeField] private List<SpriteEntry> spriteEntries = new();

        public bool TryGetPrefab(string contentId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrWhiteSpace(contentId))
                return false;

            foreach (var entry in entries.AsValueEnumerable()
                         .Where(entry => string.Equals(entry.contentId, contentId, StringComparison.Ordinal)))
            {
                prefab = entry.prefab;
                return prefab;
            }

            return false;
        }

        public bool TryGetContentId(GameObject prefab, out string contentId)
        {
            contentId = null;
            if (!prefab)
                return false;

            foreach (var entry in entries.AsValueEnumerable().Where(entry => entry.prefab == prefab))
            {
                contentId = entry.contentId;
                return !string.IsNullOrWhiteSpace(contentId);
            }

            return false;
        }

        public bool TryGetSprite(string contentId, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(contentId))
                return false;

            foreach (var entry in spriteEntries.AsValueEnumerable()
                         .Where(entry => string.Equals(entry.contentId, contentId, StringComparison.Ordinal)))
            {
                sprite = entry.sprite;
                return sprite;
            }

            return false;
        }

        public bool TryGetSpriteContentId(Sprite sprite, out string contentId)
        {
            contentId = null;
            if (!sprite)
                return false;

            foreach (var entry in spriteEntries.AsValueEnumerable().Where(entry => entry.sprite == sprite))
            {
                contentId = entry.contentId;
                return !string.IsNullOrWhiteSpace(contentId);
            }

            return false;
        }

        [Serializable]
        private class PrefabEntry
        {
            public string contentId;
            public GameObject prefab;
        }

        [Serializable]
        private class SpriteEntry
        {
            public string contentId;
            public Sprite sprite;
        }
    }
}