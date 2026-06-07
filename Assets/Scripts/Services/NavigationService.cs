using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Constants;
using Core.Pixelation;
using Core.Services;
using Core.Ship;
using Gameplay.Navigation;
using UnityEngine;
using Zenject;

[assembly: InternalsVisibleTo("Editor.Standalone")]
[assembly: InternalsVisibleTo("E2E")]
[assembly: InternalsVisibleTo("Services.Tests")]

namespace Services
{
    public class NavigationService : MonoBehaviour, INavigationService
    {
        [SerializeField] private float sectorSize = 10f;
        [SerializeField] private float cacheDuration = 1f;
        [SerializeField] private int maxSectorsDistance = 1000;

        private readonly Collider2D[] _results = new Collider2D[32];
        private readonly Dictionary<Vector2, SectorResult> _sectorCache = new();
        private ContactFilter2D _allBlockersFilter;
        private NavigationCalculator _calculator;
        private ContactFilter2D _obstaclesFilter;

        [Inject] private IShipService _shipService;

        private Vector2 Sector => new(sectorSize, sectorSize);

        private void Start()
        {
            _obstaclesFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = LayerMask.GetMask("Obstacles", "Debris"),
                useTriggers = false
            };

            _allBlockersFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = LayerMask.GetMask("Obstacles", "Debris", "Friendly", "Enemy"),
                useTriggers = false
            };

            _calculator = new NavigationCalculator(sectorSize, QuerySectorByPosition, QuerySectorByPositionForShips);
        }

        public float SectorSize => sectorSize;

        public SectorResult GetSectorResult(Vector3 position)
        {
            var normalizedPosition = _calculator.NormalizePositionToSector(position);

            if (_sectorCache.TryGetValue(normalizedPosition, out var cachedResult) &&
                cachedResult.GenerationTime > Time.time - cacheDuration)
                return cachedResult;

            var result = BuildSectorResult(normalizedPosition);
            _sectorCache[normalizedPosition] = result;
            return result;
        }

        public List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize)
        {
            return _calculator.CalculatePath(start, end, shipSize, maxSectorsDistance);
        }

        public List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize, IShip callerShip,
            IPixelatedRigidbody targetShip)
        {
            return _calculator.CalculatePath(start, end, shipSize, callerShip, targetShip, maxSectorsDistance);
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

        private Vector2 GetSectorOverlapCenter(Vector2 sectorCorner)
        {
            return sectorCorner + Sector * 0.5f;
        }

        private SectorResult QuerySectorByPositionForShips(Vector2 sectorPosition, IShip callerShip)
        {
            var count = Physics2D.OverlapBox(GetSectorOverlapCenter(sectorPosition), Sector, 0, _allBlockersFilter,
                _results);
            if (count == 0) return SectorResult.Empty;

            var shipsFound = new List<IShip>();
            var hasObstacles = false;
            var hasDebris = false;

            for (var i = 0; i < count; i++)
            {
                var layer = _results[i].gameObject.layer;
                if (layer == PhysicsLayers.Obstacles)
                {
                    hasObstacles = true;
                    continue;
                }

                if (layer == PhysicsLayers.Debris)
                {
                    hasDebris = true;
                    continue;
                }

                var ship = FindShipForCollider(_results[i]);
                if (ship != null)
                    shipsFound.Add(ship);
            }

            return new SectorResult(hasObstacles, hasDebris, Time.time, shipsFound);
        }

        private SectorResult BuildSectorResult(Vector2 sectorPosition)
        {
            var count = Physics2D.OverlapBox(GetSectorOverlapCenter(sectorPosition), Sector, 0, _obstaclesFilter,
                _results);
            var hasObstacles = false;
            var hasDebris = false;

            for (var i = 0; i < count; i++)
            {
                var layer = _results[i].gameObject.layer;
                if (layer == PhysicsLayers.Obstacles)
                    hasObstacles = true;
                else if (layer == PhysicsLayers.Debris)
                    hasDebris = true;
            }

            return new SectorResult(hasObstacles, hasDebris, Time.time);
        }

        private static IShip FindShipForCollider(Collider2D potentialShipCollider)
        {
            return !potentialShipCollider ? null : potentialShipCollider.GetComponentInParent<IShip>();
        }

#if UNITY_EDITOR
        internal float InternalSectorSize
        {
            get => sectorSize;
            set => sectorSize = value;
        }

        internal float InternalCacheDuration => cacheDuration;
        internal IReadOnlyDictionary<Vector2, SectorResult> InternalCache => _sectorCache;
#endif
    }
}