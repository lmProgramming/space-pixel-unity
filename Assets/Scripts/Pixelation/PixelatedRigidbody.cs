using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Core.Grid;
using Core.Pixelation;
using Core.Services;
using Core.Ship;
using Cysharp.Threading.Tasks;
using Events.Collision;
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

        [Inject] private CollisionEventChannelSO _collisionEventChannelSO;
        [Inject] private IDebrisSpawner _debrisSpawner;

        private bool _isSetup;
        private HealthGrid HealthGrid { get; set; }

        private bool HasArmorMap => armorMap != null && armorMap.ToString() != "null";

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            Setup();
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

        public event Action<IPixelated> OnNoPixelsLeft;

        public event Action<List<Vector2Int>, PixelLoseReason> OnPixelsLost;

        public float DefaultPixelHealthForSnapshot => defaultPixelHealth;
        public float MaxArmorHealthForSnapshot => maxArmorHealth;

        public ArmorGridSnapshot CaptureArmorGridSnapshot()
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

        public HealthGridSnapshot CaptureHealthGridSnapshot()
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

        public void ApplyArmorGridSnapshot(ArmorGridSnapshot snapshot)
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

        public void ApplyHealthGridSnapshot(HealthGridSnapshot snapshot)
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

        public Sprite GetSprite()
        {
            return sprite;
        }

        public void SetSprite(Sprite newSprite)
        {
            sprite = newSprite;
            Setup(forceSetup: true, recalculateColliders: true);
        }

        public Color32 GetColor(Vector2Int point)
        {
            return TexturePixelGrid.GetValue(point);
        }

#if UNITY_INCLUDE_TESTS
        internal void SetSpriteForTesting(Sprite testSprite)
        {
            sprite = testSprite;
        }
#endif

        public void Setup(Color32[,] colors = null, bool forceSetup = false, bool recalculateColliders = false)
        {
            if (_isSetup && !forceSetup) return;

            if (!sprite && colors is null)
            {
                Debug.LogWarning($"[PixelatedRigidbody] Setup skipped on '{name}': no sprite and no colors provided. " +
                                 "Expecting SetTextureFromColors to be called later.");
                return;
            }

            CollisionHandler?.Unsubscribe();

            _isSetup = true;

            GetComponents();

            TexturePixelGrid = new TexturePixelGrid(SpriteRenderer);

            if ((_collisionEventChannelSO && _debrisSpawner != null) || recalculateColliders)
                CollisionHandler = new PixelCollisionHandler(TexturePixelGrid, this, GetComponent<PolygonCollider2D>(),
                    _collisionEventChannelSO, _debrisSpawner);

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
        }

        public virtual void NoPixelsLeft()
        {
            OnNoPixelsLeft?.Invoke(this);
            Destroy(gameObject);
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