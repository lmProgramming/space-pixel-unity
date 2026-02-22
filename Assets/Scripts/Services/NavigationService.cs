using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Services;
using Gameplay.Navigation;
using UnityEngine;

[assembly: InternalsVisibleTo("Game.Editor")]

namespace Services
{
    public class NavigationService : MonoBehaviour, INavigationService
    {
        [SerializeField] private float sectorSize = 10f;
        [SerializeField] private float cacheDuration = 1f;

        private readonly Collider2D[] _results = new Collider2D[32];
        private readonly Dictionary<Vector2, SectorResult> _sectorCache = new();
        private NavigationCalculator _calculator;

        private ContactFilter2D _filter;

        private Vector2 Sector => new(sectorSize, sectorSize);

        private void Awake()
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = ~LayerMask.GetMask("Enemy", "Friendly"),
                useTriggers = false
            };

            _calculator = new NavigationCalculator(sectorSize, QuerySectorByPosition);
        }

        public SectorResult GetSectorResult(Vector3 position)
        {
            var normalizedPosition = _calculator.NormalizePositionToSector(position);

            if (_sectorCache.TryGetValue(normalizedPosition, out var cachedResult) &&
                cachedResult.GenerationTime > Time.time - cacheDuration)
                return cachedResult;

            var count = Physics2D.OverlapBox(normalizedPosition, Sector, 0, _filter, _results);
            var result = new SectorResult(count == 0, Time.time);
            _sectorCache[normalizedPosition] = result;

            return result;
        }

        public List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize)
        {
            return _calculator.CalculatePath(start, end, shipSize);
        }

        public void ClearCacheEntries(IEnumerable<Vector2> keys)
        {
            foreach (var key in keys)
                _sectorCache.Remove(key);
        }

        private SectorResult QuerySectorByPosition(Vector2 sectorPosition)
        {
            return GetSectorResult(new Vector3(sectorPosition.x, sectorPosition.y));
        }

#if UNITY_EDITOR
        internal float InternalSectorSize => sectorSize;
        internal float InternalCacheDuration => cacheDuration;
        internal IReadOnlyDictionary<Vector2, SectorResult> InternalCache => _sectorCache;
#endif
    }
}