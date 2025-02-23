using System;
using System.Collections.Generic;
using System.Linq;
using ContourTracer;
using UnityEngine;

namespace Pixelation
{
    [RequireComponent(typeof(PolygonCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelatedRigidbody : MonoBehaviour, IPixelated
    {
        private const float SpeedLimitForDiscreteCollisionDetectionSquared = 1;

        [SerializeField] private Sprite sprite;

        [SerializeField] private Vector2 centerPivot = new(0.5f, 0.5f);

        [SerializeField] private float lineSimplificationTolerance;

        private bool _didCollide;

        private PolygonCollider2D _polygonCollider2D;

        public PixelatedTexture PixelatedTexture { get; private set; }

        public Rigidbody2D Rigidbody { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
        }

        public void Start()
        {
            Setup();
        }

        private void Update()
        {
            _didCollide = false;

            Rigidbody.collisionDetectionMode =
                Rigidbody.linearVelocity.sqrMagnitude > SpeedLimitForDiscreteCollisionDetectionSquared
                    ? CollisionDetectionMode2D.Continuous
                    : CollisionDetectionMode2D.Discrete;
        }


        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_didCollide) return;

            var otherRb = collision.gameObject.GetComponent<PixelatedRigidbody>();

            if (otherRb is null) return;

            otherRb.ResolveCollision(this, collision);

            ResolveCollision(otherRb, collision);
        }

        public void SetSpriteFromColors(Color[,] colors)
        {
            Setup(colors);
        }

        public void RemovePixelAt(Vector2Int point)
        {
            SetPixel(point, Color.clear);

            var regions = FloodFindCohesiveRegions(point);

            if (regions.Count > 1) HandleDivision(regions);

            RecalculateColliders();
        }

        public void SetPixelNoApply(Vector2Int point, Color color)
        {
            PixelatedTexture.SetPixelNoApply(point, color);
        }

        public void SetPixel(Vector2Int point, Color color)
        {
            PixelatedTexture.SetPixel(point, color);
        }

        public void ApplyChanges()
        {
            PixelatedTexture.ApplyChanges();
        }

        public Color GetColor(Vector2Int point)
        {
            return PixelatedTexture.GetColor(point);
        }

        public bool IsPixel(Vector2Int point)
        {
            return InBounds(point) && IsPixelAssumeInBounds(point);
        }

        public bool IsPixelAssumeInBounds(Vector2Int point)
        {
            return PixelatedTexture.IsPixelAssumeInBounds(point);
        }

        public bool InBounds(Vector2Int point)
        {
            return PixelatedTexture.InBounds(point);
        }

        public void ResolveCollision(IPixelated other, Collision2D collision)
        {
            _didCollide = true;
            DamageAt(collision.contacts[0].point, collision);
        }

        public Vector2Int WorldToLocalPoint(Vector2 worldPosition)
        {
            Vector2 position = transform.InverseTransformPoint(worldPosition);

            return new Vector2Int((int)(position.x + (float)PixelatedTexture.Width / 2),
                (int)(position.y + (float)PixelatedTexture.Height / 2));
        }

        public void Setup(Color[,] colors = null)
        {
            if (sprite.ToString() == "null" && colors is null) return;
            
            PixelatedTexture = new PixelatedTexture(GetComponent<SpriteRenderer>());
            
            if (colors is not null)
            {
                Debug.Log(colors.Length);
                PixelatedTexture.SetSpriteFromColors(colors);
            }

            if (sprite.ToString() != "null") PixelatedTexture.SetSprite(sprite);
            
            PixelatedTexture.Setup();

            GetComponents();

            // CalculatePixels();

            RecalculateColliders();
        }

        // private void CalculatePixels()
        // {
        //     _pixels = new Pixel[_texture.width, _texture.height];
        //
        //     for (var x = 0; x < _texture.width; x++)
        //     for (var y = 0; y < _texture.height; y++)
        //     {
        //         var color = _texture.GetPixel(x, y);
        //         _pixels[x, y] = new Pixel(color, 100);
        //     }
        // }

        public event Action<IPixelated> OnNoPixelsLeft;

        private void GetComponents()
        {
            _polygonCollider2D = GetComponent<PolygonCollider2D>();
            Rigidbody = GetComponent<Rigidbody2D>();
        }

        private void RecalculateColliders()
        {
            var gridContourTracer = new GridContourTracer();
            var polygon = gridContourTracer.GenerateCollider(PixelatedTexture.Texture, centerPivot, 1);
            if (polygon is null)
            {
                NoPixelsLeft();
                return;
            }

            var points = new List<Vector2>();

            LineUtility.Simplify(polygon.ToList(), lineSimplificationTolerance, points);

            _polygonCollider2D.pathCount = 1;
            _polygonCollider2D.SetPath(0, points);
        }

        private void DamageAt(Vector2 position, Collision2D collision)
        {
            var localPoint = WorldToLocalPoint(position);

            // var pixelToDestroyPosition = GetPointAlongPath(hitPosition, -collision.rigidbody.linearVelocity, true) ??
            //                              GetPointAlongPath(hitPosition, collision.rigidbody.linearVelocity, false);

            var pixelToDestroyPosition = GetClosestPixelPosition(localPoint);

            if (pixelToDestroyPosition == null) return;

            var pos = pixelToDestroyPosition.Value;
            RemovePixelAt(pos);
        }

        private void HandleDivision(List<HashSet<Vector2Int>> regions)
        {
            regions = regions.OrderBy(r => r.Count).ToList();

            for (var index = 0; index < regions.Count - 1; index++)
            {
                var region = regions[index];

                if (region.Count >= 5) CreateNewJunk(region);

                RemovePixels(region);
            }

            ApplyChanges();
            RecalculateColliders();
        }

        private void RemovePixels(HashSet<Vector2Int> points)
        {
            foreach (var point in points) SetPixelNoApply(point, Color.clear);

            ApplyChanges();
        }

        private void CreateNewJunk(HashSet<Vector2Int> points)
        {
            var rightTopPoint = new Vector2Int(points.Max(p => p.x), points.Max(p => p.y));
            var leftBottomPoint = new Vector2Int(points.Min(p => p.x), points.Min(p => p.y));
            var parentCenterPoint = PixelatedTexture.Center;

            var width = rightTopPoint.x - leftBottomPoint.x + 1;
            var height = rightTopPoint.y - leftBottomPoint.y + 1;

            var centrePoint = leftBottomPoint + new Vector2(width, height) / 2;

            var newColorsGrid = new Color[width, height];

            foreach (var point in points)
                newColorsGrid[point.x - leftBottomPoint.x, point.y - leftBottomPoint.y] = GetColor(point);

            var globalPosition = transform.TransformPoint(centrePoint - parentCenterPoint);

            JunkSpawner.Instance.SpawnJunk(globalPosition, transform.rotation, newColorsGrid);
        }

        private List<HashSet<Vector2Int>> FloodFindCohesiveRegions(Vector2Int searchStartPoint)
        {
            var visited = new HashSet<Vector2Int>
            {
                searchStartPoint
            };

            var regions = new List<HashSet<Vector2Int>>();

            SetupFlooding(searchStartPoint + new Vector2Int(1, 0));
            SetupFlooding(searchStartPoint + new Vector2Int(-1, 0));
            SetupFlooding(searchStartPoint + new Vector2Int(0, 1));
            SetupFlooding(searchStartPoint + new Vector2Int(0, -1));

            return regions;

            void SetupFlooding(Vector2Int searchStart)
            {
                if (!InBounds(searchStart)) return;

                regions.Add(new HashSet<Vector2Int> { searchStart });

                FloodFind(searchStart, regions.Count - 1);
            }

            void FloodFind(Vector2Int position, int regionIndex)
            {
                if (regionIndex == regions.Count) return;

                if (!InBounds(position)) return;

                if (!IsPixel(position)) return;

                if (!visited.Add(position))
                {
                    FindRegionToMerge(position, regionIndex);
                    return;
                }

                regions[regionIndex].Add(position);

                FloodFind(position + new Vector2Int(1, 0), regionIndex);
                FloodFind(position + new Vector2Int(-1, 0), regionIndex);
                FloodFind(position + new Vector2Int(0, 1), regionIndex);
                FloodFind(position + new Vector2Int(0, -1), regionIndex);
            }

            void FindRegionToMerge(Vector2Int position, int regionIndex)
            {
                for (var index = 0; index < regions.Count; index++)
                {
                    var region = regions[index];

                    if (!region.Contains(position)) continue;

                    if (index == regionIndex) break;

                    MergeRegions(index, regionIndex);
                }
            }

            void MergeRegions(int indexToMergeWith, int indexMerged)
            {
                regions[indexToMergeWith].UnionWith(regions[indexMerged]);

                regions.RemoveAt(indexMerged);
            }
        }

        private Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
        {
            var pointsTraversed = GridMarcher.March(new Vector2Int(PixelatedTexture.Width, PixelatedTexture.Height),
                startPosition,
                direction);

            if (getLast) pointsTraversed.Reverse();

            foreach (var point in pointsTraversed.Where(IsPixel))
                return new Vector2Int(point.x, point.y);

            return null;
        }

        private List<Vector2Int> GetClosestPixelPositions(Vector2 localPosition, int positionsMaxCount)
        {
            var localPositionInt = new Vector2Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y));

            var radiusChecked = 0;

            var maxRadiusChecked = Mathf.Max(PixelatedTexture.Width, PixelatedTexture.Height);

            var closestPointsAndDistances = new List<(Vector2Int Position, float Distance)>();

            while (radiusChecked < maxRadiusChecked)
            {
                for (var x = localPositionInt.x - radiusChecked; x <= localPositionInt.x + radiusChecked; x++)
                for (var y = localPositionInt.y - radiusChecked; y <= localPositionInt.y + radiusChecked; y++)
                {
                    var pixelPosition = new Vector2Int(x, y);

                    if (!IsPixel(pixelPosition)) continue;

                    var distance = (new Vector2(x, y) - localPosition).SqrMagnitude();

                    InsertPositionToSortedArray(pixelPosition, distance);
                }

                if (closestPointsAndDistances.Count >= positionsMaxCount) break;

                radiusChecked++;
            }

            return closestPointsAndDistances.Select(p => p.Position).ToList();

            void InsertPositionToSortedArray(Vector2Int position, float distance)
            {
                for (var index = 0; index < closestPointsAndDistances.Count; index++)
                {
                    var closestPointAndDistance = closestPointsAndDistances[index];

                    if (!(distance < closestPointAndDistance.Distance)) continue;
                    closestPointsAndDistances.Insert(index, (position, radiusChecked));
                    return;
                }

                closestPointsAndDistances.Add((position, radiusChecked));
            }
        }

        private Vector2Int? GetClosestPixelPosition(Vector2 localPosition)
        {
            var positions = GetClosestPixelPositions(localPosition, 1);

            if (positions.Count > 0) return positions[0];
            return null;
        }

        protected virtual void NoPixelsLeft()
        {
            OnNoPixelsLeft?.Invoke(this);
            Destroy(gameObject);
        }
    }
}