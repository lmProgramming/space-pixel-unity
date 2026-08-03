using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Core.Constants;
using Core.Grid;
using Core.Pixelation;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.PixelatedRigidbody;
using Core.Ships.Snapshots.PixelatedRigidbody.Internals;
using Cysharp.Threading.Tasks;
using Events.Gameplay.Collision;
using Grid;
using LMPro;
using UnityEngine;
using Zenject;
using ZLinq;

[assembly: InternalsVisibleTo("Ships.Tests")]
[assembly: InternalsVisibleTo("Pixelation.Tests")]

namespace Pixelation
{
    [RequireComponent(typeof(PolygonCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class PixelatedRigidbody : MonoBehaviour, IPixelatedRigidbody, IPixelatedSprite
    {
        private const float SpeedLimitForDiscreteCollisionDetectionSquared = 0;

        [SerializeField] private Sprite sprite;
        [SerializeField] private bool flipX;
        [SerializeField] private bool flipY;

        [Range(0, 3)] [SerializeField] private int rotation;

        [SerializeField] private float defaultPixelHealth = 1f;

        [Header("Armor Map (optional)")]
        [Tooltip("Grayscale sprite where brightness = armor strength. " +
                 "White (255) = maxArmorHealth, black (0) = defaultPixelHealth. Must match the color sprite dimensions.")]
        [SerializeField]
        private Sprite armorMap;

        [Tooltip("Health value that a fully white (255) pixel in the armor map represents.")] [SerializeField]
        private float maxArmorHealth = 10f;

        [Inject] protected GameplayConstants GameplayConstants;

        [Inject] private CollisionEventChannelSO _collisionEventChannelSO;
        [Inject] private IDebrisSpawner _debrisSpawner;

        private bool _isSetup;
        [Inject] private PixelCollisionHandler.Factory _pixelCollisionHandlerFactory;
        private HealthGrid HealthGrid { get; set; }

        private bool HasArmorMap => armorMap != null && armorMap.ToString() != "null";

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();

            if (!_isSetup)
                Setup();
        }

        private void Start()
        {
            if (TexturePixelGrid == null || TexturePixelGrid.PixelCount == 0)
                throw new InvalidDataException("TexturePixelGrid is null or has no pixels.");

            EnsureCollisionHandler();
        }

        private void FixedUpdate()
        {
            if (CollisionHandler == null) return;

            CollisionHandler.SetCollided(false);

            Rigidbody.collisionDetectionMode =
                Rigidbody.linearVelocity.sqrMagnitude >= SpeedLimitForDiscreteCollisionDetectionSquared
                    ? CollisionDetectionMode2D.Continuous
                    : CollisionDetectionMode2D.Discrete;
        }

        protected virtual void OnDestroy()
        {
            _isSetup = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CollisionHandler?.OnCollision(collision);
        }

        public Vector2 WeightedCenter { get; private set; }
        public Vector2 WorldWeightedCenter => LocalToWorldPoint(WeightedCenter);

        public ITexturePixelGrid TexturePixelGrid { get; set; }

        [field: SerializeField] public float MassMultiplier { get; private set; } = 1;

        public int CurrentPixelCount => TexturePixelGrid.PixelCount;
        public int StartPixelCount { get; private set; }

        public bool HasSprite => sprite != null && sprite.ToString() != "null";
        public IPixelCollisionHandler CollisionHandler { get; private set; }

        public Rigidbody2D Rigidbody { get; private set; }
        public SpriteRenderer SpriteRenderer { get; set; }

        public void ApplyPixels()
        {
            TexturePixelGrid.ApplyPixels();
        }

        public bool IsPixel(Vector2Int point)
        {
            return InBounds(point) && IsPixelAssumeInBounds(point);
        }

        public bool IsPixelAssumeInBounds(Vector2Int point)
        {
            return TexturePixelGrid.IsPixelAssumeInBounds(point);
        }

        public bool InBounds(Vector2Int point)
        {
            return TexturePixelGrid.InBounds(point);
        }

        public Vector2Int Dimensions()
        {
            return TexturePixelGrid.Dimensions();
        }

        public void SetPixel(Vector2Int point, Color32 color)
        {
            TexturePixelGrid.SetPixel(point, color);
        }

        public void SetTextureFromColors(Color32[,] colors)
        {
            Setup(colors, true);
        }

        public void SetPixelNoApply(Vector2Int point, Color32 color)
        {
            TexturePixelGrid.SetPixelNoApply(point, color);
        }

        public void RemovePixels(IEnumerable<Vector2Int> points, bool simulateCollision = false)
        {
            var pointsArray = points as Vector2Int[] ?? points.AsValueEnumerable().ToArray();

            if (!pointsArray.AsValueEnumerable().Any()) return;

            HealthGrid?.RemovePixels(pointsArray);

            var countBefore = TexturePixelGrid.PixelCount;
            TexturePixelGrid.RemovePixels(pointsArray);
            NudgeWeightedCenter(pointsArray, countBefore);

            OnPixelsLost?.Invoke(pointsArray.AsValueEnumerable().ToList(), PixelLoseReason.Destroyed);

            if (!simulateCollision) return;

            var contactPoint = LocalToWorldPoint(pointsArray.AsValueEnumerable().First());

            CollisionHandler.RaiseCollisionEvent(null, contactPoint, pointsArray);
        }

        public void RemovePixelAt(Vector2Int point, bool simulateCollision = false)
        {
            RemovePixels(new[] { point }, simulateCollision);
        }

        public bool DamagePixelAt(Vector2Int point, float damage, bool simulateCollision = false)
        {
            if (!IsPixel(point)) return false;

            var killed = HealthGrid.DamagePixel(point, damage);

            if (!killed) return false;

            RemovePixelAt(point, simulateCollision);
            return true;
        }

        public List<Vector2Int> DamagePixels(IEnumerable<Vector2Int> points, float damagePerPixel,
            bool simulateCollision = false)
        {
            var destroyed = HealthGrid.DamagePixels(points, damagePerPixel);

            if (destroyed.Count > 0)
                RemovePixels(destroyed, simulateCollision);

            return destroyed;
        }

        public Collider2D Collider2D { get; private set; }
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        public Vector2 WorldToLocalPoint(Vector2 worldPosition)
        {
            var position = transform.InverseTransformPoint(worldPosition);

            return new Vector2(position.x + (float)TexturePixelGrid.Width / 2,
                position.y + (float)TexturePixelGrid.Height / 2);
        }

        public Vector2Int WorldToLocalPixel(Vector2 worldPosition)
        {
            var position = WorldToLocalPoint(worldPosition);

            return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        }

        public Vector2 LocalToWorldPoint(Vector2Int localPosition)
        {
            Vector2 position = transform.TransformPoint(new Vector2(localPosition.x - (float)TexturePixelGrid.Width / 2,
                localPosition.y - (float)TexturePixelGrid.Height / 2));

            return position;
        }

        public Vector2 LocalToWorldPoint(Vector2 localPosition)
        {
            Vector2 position = transform.TransformPoint(new Vector2(localPosition.x - (float)TexturePixelGrid.Width / 2,
                localPosition.y - (float)TexturePixelGrid.Height / 2));

            return position;
        }

        public event Action<IPixelatedRigidbody> Destroyed;

        public event Action<List<Vector2Int>, PixelLoseReason> OnPixelsLost;

        public event Action<List<Vector2Int>> OnPixelsRestored;

        public float DefaultPixelHealthForSnapshot => defaultPixelHealth;
        public float MaxArmorHealthForSnapshot => maxArmorHealth;

        public virtual PixelatedRigidbodySnapshot CaptureSnapshot(IGameContentCatalog contentCatalog)
        {
            var snapshot = new PixelatedRigidbodySnapshot
            {
                name = transform.name,
                rigidbodyType = GetSnapshotRigidbodyType(),
                localPosition = transform.localPosition,
                localRotation = transform.localRotation,
                spriteRenderedOrderInLayer = SpriteRenderer.sortingOrder,
                spriteRenderedSortingLayerID = SpriteRenderer.sortingLayerID,
                defaultPixelHealth = defaultPixelHealth,
                maxArmorHealth = maxArmorHealth,
                startPixelCount = StartPixelCount,
                armorGrid = CaptureArmorGridSnapshot(),
                healthGrid = CaptureHealthGridSnapshot()
            };

            if (TexturePixelGrid == null)
            {
                Debug.LogWarning(
                    $"[PixelatedRigidbody] '{name}' has no TexturePixelGrid. Snapshot will not contain pixel data.");
                return snapshot;
            }

            var dimensions = TexturePixelGrid.Dimensions();
            snapshot.colorGrid = new PixelGridSnapshot(dimensions.x, dimensions.y);

            for (var y = 0; y < dimensions.y; y++)
            for (var x = 0; x < dimensions.x; x++)
            {
                var pos = new Vector2Int(x, y);
                if (TexturePixelGrid.IsPixel(pos))
                    snapshot.colorGrid.SetPixel(pos, TexturePixelGrid.GetValue(pos));
            }

            return snapshot;
        }

        public virtual void RestoreFromSnapshot(PixelatedRigidbodySnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            if (snapshot == null)
                throw new UnityException($"[PixelatedRigidbody] Cannot restore null snapshot on '{name}'.");

            ApplySnapshotHealthDefaults(snapshot.defaultPixelHealth, snapshot.maxArmorHealth);
            RestoreColorGridFromSnapshot(snapshot.colorGrid);
            ApplyArmorGridSnapshot(snapshot.armorGrid);
            ApplyHealthGridSnapshot(snapshot.healthGrid);
            ApplySpriteRenderedOptions(snapshot.spriteRenderedSortingLayerID, snapshot.spriteRenderedOrderInLayer);

            if (snapshot.startPixelCount > 0)
                SetStartPixelCount(snapshot.startPixelCount);
        }

        public void SetStartPixelCount(int startPixelCount)
        {
            if (startPixelCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(startPixelCount),
                    "[PixelatedRigidbody] StartPixelCount must be positive.");
            StartPixelCount = startPixelCount;
        }

        public void RestorePixels(IReadOnlyList<Pixel> pixels)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (pixels.Count == 0) return;
            if (TexturePixelGrid == null)
                throw new InvalidOperationException(
                    $"[PixelatedRigidbody] '{name}' has no TexturePixelGrid; cannot restore pixels.");
            if (HealthGrid == null)
                throw new InvalidOperationException(
                    $"[PixelatedRigidbody] '{name}' has no HealthGrid; cannot restore pixels.");

            var countBefore = TexturePixelGrid.PixelCount;
            var points = new List<Vector2Int>(pixels.Count);
            var addPayload = new List<(Vector2Int point, Color32 color)>(pixels.Count);

            foreach (var pixel in pixels)
            {
                if (pixel.Color.a == 0)
                    throw new UnityException(
                        $"[PixelatedRigidbody] '{name}' cannot restore transparent pixel at {pixel.Point}.");
                if (pixel.Health <= 0f)
                    throw new UnityException(
                        $"[PixelatedRigidbody] '{name}' cannot restore pixel at {pixel.Point} with non-positive health.");

                points.Add(pixel.Point);
                addPayload.Add((pixel.Point, pixel.Color));
            }

            TexturePixelGrid.AddPixels(addPayload);

            foreach (var pixel in pixels)
                HealthGrid.SetHealth(pixel.Point, pixel.Health);

            NudgeWeightedCenterForAdded(points, countBefore);
            OnPixelsRestored?.Invoke(points);
        }

        public void KeepOnlyPixels(IEnumerable<Vector2Int> pointsToKeep)
        {
            if (pointsToKeep == null) throw new ArgumentNullException(nameof(pointsToKeep));
            if (TexturePixelGrid == null)
                throw new InvalidOperationException(
                    $"[PixelatedRigidbody] '{name}' has no TexturePixelGrid; cannot keep pixels.");

            var keepSet = pointsToKeep.AsValueEnumerable().ToHashSet();
            if (keepSet.Count == 0)
                throw new UnityException(
                    $"[PixelatedRigidbody] '{name}' KeepOnlyPixels requires at least one pixel.");

            var dims = TexturePixelGrid.Dimensions();
            var removed = new List<Vector2Int>();

            for (var y = 0; y < dims.y; y++)
            for (var x = 0; x < dims.x; x++)
            {
                var point = new Vector2Int(x, y);
                if (!TexturePixelGrid.IsPixelAssumeInBounds(point) || keepSet.Contains(point))
                    continue;
                removed.Add(point);
            }

            if (removed.Count == 0)
            {
                WeightedCenter = CalculateWeightedCenter();
                CollisionHandler?.ForceRecalculateColliders();
                return;
            }

            var countBefore = TexturePixelGrid.PixelCount;
            HealthGrid?.RemovePixels(removed);
            TexturePixelGrid.RemovePixels(removed);
            NudgeWeightedCenter(removed, countBefore);
            CollisionHandler?.ForceRecalculateColliders();
        }

        public Color32[,] BuildPristineColors()
        {
            if (!HasSprite)
                throw new InvalidOperationException(
                    $"[PixelatedRigidbody] '{name}' has no sprite; cannot build pristine colors.");

            var (colorsArray, width, height) =
                (sprite.texture.GetPixels32(), sprite.texture.width, sprite.texture.height);
            colorsArray = EasyImage.ReorientTexture(colorsArray, width, height, flipX, flipY);
            (colorsArray, width, height) = EasyImage.RotateTexture(colorsArray, width, height, rotation);

            var colors = new Color32[width, height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                colors[x, y] = colorsArray[y * width + x];

            return colors;
        }

        public float[,] BuildPristineHealth(Color32[,] pristineColors)
        {
            if (pristineColors == null) throw new ArgumentNullException(nameof(pristineColors));

            var width = pristineColors.GetLength(0);
            var height = pristineColors.GetLength(1);
            var health = new float[width, height];

            byte[] armorBytes = null;
            var armorWidth = 0;

            if (HasArmorMap)
            {
                var armorPixels = armorMap.texture.GetPixels32();
                armorWidth = armorMap.texture.width;
                var armorHeight = armorMap.texture.height;
                armorPixels = EasyImage.ReorientTexture(armorPixels, armorWidth, armorHeight, flipX, flipY);
                (armorPixels, armorWidth, armorHeight) =
                    EasyImage.RotateTexture(armorPixels, armorWidth, armorHeight, rotation);

                if (armorWidth != width || armorHeight != height)
                    throw new UnityException(
                        $"[PixelatedRigidbody] Armor map size ({armorWidth}x{armorHeight}) doesn't match " +
                        $"sprite size ({width}x{height}) on '{name}'.");

                armorBytes = armorPixels.AsValueEnumerable().Select(c => c.r).ToArray();
            }

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (pristineColors[x, y].a == 0)
                {
                    health[x, y] = 0f;
                    continue;
                }

                if (armorBytes == null)
                {
                    health[x, y] = defaultPixelHealth;
                    continue;
                }

                var brightness = armorBytes[y * armorWidth + x] / 255f;
                health[x, y] = Mathf.Lerp(defaultPixelHealth, maxArmorHealth, brightness);
            }

            return health;
        }

        public virtual void NoPixelsLeft()
        {
            Destroyed?.Invoke(this);
            Destroy(gameObject);
        }

        public Color32 GetColor(Vector2Int point)
        {
            return TexturePixelGrid.GetValue(point);
        }

        public Sprite GetSprite()
        {
            return sprite;
        }

        public void SetSprite(Sprite newSprite)
        {
            sprite = newSprite;
            Setup(forceSetup: true, recalculateColliders: true);
        }

        private void NudgeWeightedCenterForAdded(IEnumerable<Vector2Int> addedPixels, int countBefore)
        {
            if (TexturePixelGrid.PixelCount <= 0)
            {
                WeightedCenter = TexturePixelGrid.Center;
                return;
            }

            if (countBefore <= 0)
            {
                WeightedCenter = CalculateWeightedCenter();
                return;
            }

            var addedSum = addedPixels.AsValueEnumerable()
                .Aggregate(Vector2.zero, (current, p) => current + new Vector2(p.x, p.y));

            WeightedCenter = (WeightedCenter * countBefore + addedSum) / TexturePixelGrid.PixelCount;
        }

        private void EnsureCollisionHandler()
        {
            if (CollisionHandler != null)
                return;

            if (!_collisionEventChannelSO || _debrisSpawner == null || TexturePixelGrid == null)
                return;

            if (_pixelCollisionHandlerFactory == null)
                throw new UnityException(
                    "[PixelatedRigidbody] PixelCollisionHandler.Factory is required.");

            CollisionHandler = _pixelCollisionHandlerFactory.Create(
                TexturePixelGrid, this, RequirePolygonCollider());
            CollisionHandler.ForceRecalculateColliders();
        }

        private void ApplySpriteRenderedOptions(int sortingLayerID, int orderInLayer)
        {
            SpriteRenderer.sortingLayerID = sortingLayerID;
            SpriteRenderer.sortingOrder = orderInLayer;
        }

        private ArmorGridSnapshot CaptureArmorGridSnapshot()
        {
            if (TexturePixelGrid == null)
                return null;

            var dims = TexturePixelGrid.Dimensions();
            if (!HasArmorMap)
                return null;

            var armorSnapshot = new ArmorGridSnapshot(dims.x, dims.y);
            var armorPixels = armorMap.texture.GetPixels32();
            var armorWidth = armorMap.texture.width;
            var armorHeight = armorMap.texture.height;

            armorPixels = EasyImage.ReorientTexture(armorPixels, armorWidth, armorHeight, flipX, flipY);
            (armorPixels, armorWidth, armorHeight) =
                EasyImage.RotateTexture(armorPixels, armorWidth, armorHeight, rotation);

            if (armorWidth != dims.x || armorHeight != dims.y)
                return null;

            for (var y = 0; y < armorHeight; y++)
            for (var x = 0; x < armorWidth; x++)
                armorSnapshot.SetValue(x, y, armorPixels[y * armorWidth + x].r);

            return armorSnapshot;
        }

        private HealthGridSnapshot CaptureHealthGridSnapshot()
        {
            if (TexturePixelGrid == null || HealthGrid == null)
                return null;

            var dims = TexturePixelGrid.Dimensions();
            var snapshot = new HealthGridSnapshot(dims.x, dims.y);
            for (var y = 0; y < dims.y; y++)
            for (var x = 0; x < dims.x; x++)
                snapshot.SetValue(x, y, HealthGrid.GetValue(new Vector2Int(x, y)));

            return snapshot;
        }

        private void ApplyArmorGridSnapshot(ArmorGridSnapshot snapshot)
        {
            if (snapshot == null || HealthGrid == null || TexturePixelGrid == null)
                return;

            var dims = TexturePixelGrid.Dimensions();
            if (snapshot.width != dims.x || snapshot.height != dims.y)
                throw new UnityException(
                    "[PixelatedRigidbody] Armor grid snapshot dimensions do not match pixel grid.");

            for (var y = 0; y < snapshot.height; y++)
            for (var x = 0; x < snapshot.width; x++)
            {
                var point = new Vector2Int(x, y);
                if (!TexturePixelGrid.IsPixelAssumeInBounds(point))
                    continue;
                var brightness = snapshot.GetValue(x, y) / 255f;
                var health = Mathf.Lerp(defaultPixelHealth, maxArmorHealth, brightness);
                HealthGrid.SetHealth(point, health);
            }
        }

        private void ApplyHealthGridSnapshot(HealthGridSnapshot snapshot)
        {
            if (snapshot == null || HealthGrid == null || TexturePixelGrid == null)
                return;

            var dims = TexturePixelGrid.Dimensions();
            if (snapshot.width != dims.x || snapshot.height != dims.y)
                throw new UnityException(
                    "[PixelatedRigidbody] Health grid snapshot dimensions do not match pixel grid.");

            for (var y = 0; y < snapshot.height; y++)
            for (var x = 0; x < snapshot.width; x++)
                HealthGrid.SetHealth(new Vector2Int(x, y), snapshot.GetValue(x, y));
        }

        protected virtual PixelatedRigidbodyType GetSnapshotRigidbodyType()
        {
            return PixelatedRigidbodyType.PixelatedRigidbody;
        }

        private void ApplySnapshotHealthDefaults(float defaultHealth, float maxArmor)
        {
            defaultPixelHealth = defaultHealth;
            maxArmorHealth = maxArmor;
        }

        private void RestoreColorGridFromSnapshot(PixelGridSnapshot colorGrid)
        {
            if (colorGrid == null || colorGrid.Width == 0 || colorGrid.Height == 0)
                throw new UnityException(
                    $"[PixelatedRigidbody] '{name}' has no pixel data in snapshot. " +
                    "Re-capture the snapshot — empty color grids are not supported.");

            var colors = new Color32[colorGrid.Width, colorGrid.Height];

            for (var y = 0; y < colorGrid.Height; y++)
            for (var x = 0; x < colorGrid.Width; x++)
                colors[x, y] = colorGrid.GetValue(new Vector2Int(x, y));

            SetTextureFromColors(colors);
        }

#if UNITY_INCLUDE_TESTS
        internal void SetSpriteForTesting(Sprite testSprite)
        {
            sprite = testSprite;
        }
#endif

        public void Setup(Color32[,] colors = null, bool forceSetup = false, bool recalculateColliders = false)
        {
            if (_isSetup && !forceSetup)
            {
                Debug.LogWarning($"[PixelatedRigidbody] Setup already called on '{name}'.");
                return;
            }

            if (!sprite && colors is null)
            {
                if (TexturePixelGrid == null || TexturePixelGrid.PixelCount == 0) return;

                colors = TexturePixelGrid.GetValues2D();

                if (colors is null || colors.Length == 0)
                    throw new InvalidOperationException(
                        $"[PixelatedRigidbody] Setup failed on '{name}': no sprite and no colors provided, and existing TexturePixelGrid has no pixels.");
            }

            CollisionHandler?.Unsubscribe();

            _isSetup = true;

            GetComponents();

            TexturePixelGrid = new TexturePixelGrid(SpriteRenderer);

            if ((_collisionEventChannelSO && _debrisSpawner != null) || recalculateColliders)
            {
                if (_pixelCollisionHandlerFactory == null)
                    throw new UnityException(
                        "[PixelatedRigidbody] PixelCollisionHandler.Factory is required.");

                CollisionHandler = _pixelCollisionHandlerFactory.Create(
                    TexturePixelGrid, this, RequirePolygonCollider());
            }

            if (colors is not null)
            {
                TexturePixelGrid.SetTextureFromColors(colors);
            }
            else if (HasSprite)
            {
                var (colorsArray, width, height) =
                    (sprite.texture.GetPixels32(), sprite.texture.width, sprite.texture.height);
                colorsArray = EasyImage.ReorientTexture(colorsArray, width, height, flipX, flipY);
                (colorsArray, width, height) = EasyImage.RotateTexture(colorsArray, width, height, rotation);
                TexturePixelGrid.SetTextureFromColors(colorsArray, width, height);
            }

            TexturePixelGrid.Setup();

            HealthGrid = new HealthGrid(TexturePixelGrid.Width, TexturePixelGrid.Height, defaultPixelHealth);
            HealthGrid.InitializeFromGrid(TexturePixelGrid);

            if (HasArmorMap)
                ApplyArmorMap();

            StartPixelCount = TexturePixelGrid.PixelCount;

            WeightedCenter = CalculateWeightedCenter();

            OnPixelsLost?.Invoke(new List<Vector2Int>(), PixelLoseReason.Other);

            if (recalculateColliders)
                CollisionHandler?.ForceRecalculateColliders();
        }

        private void ApplyArmorMap()
        {
            var armorPixels = armorMap.texture.GetPixels32();
            var armorWidth = armorMap.texture.width;
            var armorHeight = armorMap.texture.height;

            armorPixels = EasyImage.ReorientTexture(armorPixels, armorWidth, armorHeight, flipX, flipY);
            (armorPixels, armorWidth, armorHeight) =
                EasyImage.RotateTexture(armorPixels, armorWidth, armorHeight, rotation);

            if (armorWidth != TexturePixelGrid.Width || armorHeight != TexturePixelGrid.Height)
            {
                Debug.LogError(
                    $"[PixelatedRigidbody] Armor map size ({armorWidth}x{armorHeight}) doesn't match " +
                    $"sprite size ({TexturePixelGrid.Width}x{TexturePixelGrid.Height}) on '{name}'. Armor map ignored.");
                return;
            }

            HealthGrid.ApplyArmorMap(armorPixels, armorWidth, armorHeight, maxArmorHealth);
        }

        private Vector2 CalculateWeightedCenter()
        {
            var sum = Vector2.zero;
            var dims = TexturePixelGrid.Dimensions();

            for (var x = 0; x < dims.x; x++)
            for (var y = 0; y < dims.y; y++)
                if (TexturePixelGrid.IsPixelAssumeInBounds(new Vector2Int(x, y)))
                    sum += new Vector2(x, y);

            return TexturePixelGrid.PixelCount > 0 ? sum / TexturePixelGrid.PixelCount : TexturePixelGrid.Center;
        }

        private void NudgeWeightedCenter(IEnumerable<Vector2Int> removedPixels, int countBefore)
        {
            if (TexturePixelGrid.PixelCount <= 0)
            {
                WeightedCenter = TexturePixelGrid.Center;
                return;
            }

            var removedSum = removedPixels.AsValueEnumerable()
                .Aggregate(Vector2.zero, (current, p) => current + new Vector2(p.x, p.y));

            WeightedCenter = (WeightedCenter * countBefore - removedSum) / TexturePixelGrid.PixelCount;
        }

        private void GetComponents()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            SpriteRenderer = GetComponent<SpriteRenderer>();
            Collider2D = GetComponent<Collider2D>();
        }

        private PolygonCollider2D RequirePolygonCollider()
        {
            var polygonCollider = Collider2D as PolygonCollider2D ?? GetComponent<PolygonCollider2D>();
            if (!polygonCollider)
                throw new UnityException(
                    $"[PixelatedRigidbody] PolygonCollider2D is required on '{name}'.");

            return polygonCollider;
        }

        public void CopyVelocity(IPixelatedRigidbody parentBody)
        {
            Rigidbody.linearVelocity = parentBody.Rigidbody.linearVelocity;
        }

        protected async UniTask FadeOutAndDestroy(float duration)
        {
            var token = this.GetCancellationTokenOnDestroy();

            await FadeOut(duration, token);

            if (!token.IsCancellationRequested) Destroy(gameObject);
        }

        private async UniTask FadeOut(float duration, CancellationToken token)
        {
            var elapsed = 0f;

            if (SpriteRenderer == null) return;
            var startColor = SpriteRenderer.color;

            while (elapsed < duration)
            {
                if (token.IsCancellationRequested || SpriteRenderer == null) return;

                var alpha = 1f - elapsed / duration;
                SpriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (SpriteRenderer != null) SpriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        }

        public void PixelLostByDivision(HashSet<Vector2Int> region)
        {
            HealthGrid?.RemovePixels(region);

            var countBefore = TexturePixelGrid.PixelCount + region.Count;
            NudgeWeightedCenter(region, countBefore);
            OnPixelsLost?.Invoke(region.AsValueEnumerable().ToList(), PixelLoseReason.Division);
        }

        public void SetSprites(Sprite visualSprite, Sprite armorSprite)
        {
            sprite = visualSprite;
            armorMap = armorSprite;
        }
    }
}