using UnityEngine;

namespace Core.Services
{
    public interface IGameContentCatalog
    {
        bool TryGetPrefab(string contentId, out GameObject prefab);
        bool TryGetContentId(GameObject prefab, out string contentId);
        bool TryGetSprite(string contentId, out Sprite sprite);
        bool TryGetSpriteContentId(Sprite sprite, out string contentId);
    }
}
