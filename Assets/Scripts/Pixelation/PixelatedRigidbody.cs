using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Core.Grid;
using Core.Pixelation;
using Core.Services;
using Cysharp.Threading.Tasks;
using Events.Collision;
using Grid;
using LMPro;
using UnityEngine;
using Zenject;
using ZLinq;

[assembly: InternalsVisibleTo("Game.Ships.Tests")]
[assembly: InternalsVisibleTo("Game.Pixelation.Tests")]

namespace Pixelation
{
    [RequireComponent(typeof(PolygonCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelatedRigidbody : MonoBehaviour, IPixelatedRigidbody
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
        [SerializeField] private Sprite armorMap;

        [Tooltip("Health value that a fully white (255) pixel in the armor map represents.")]
        [SerializeField] private float maxArmorHealth = 10f;

        [Inject] private CollisionEventChannelSO _collisionEventChannelSO;
        [Inject] private IDebrisSpawner _debrisSpawner;

        private bool _isSetup;
        private HealthGrid HealthGrid { get; set; }

        private bool HasArmorMap => armorMap != null && armorMap.ToString() != "null";

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
        }

        public virtual void Start()
        {
            Setup();
        }

        private void Update()
        {
            if (CollisionHandler == null) return;

            CollisionHandler.SetCollided(false);

            Rigidbody.collisionDetectionMode =
                Rigidbody.linearVelocity.sqrMagnitude >= SpeedLimitForDiscreteCollisionDetectionSquared
                    ? CollisionDetectionMode2D.Continuous
                    : CollisionDetectionMode2D.Discrete;
        }

        private void OnDestroy()
        {
            _isSetup = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CollisionHandler?.OnCollision(collision);
        }

        public Vector2 WeightedCenter { get; private set; }

        public IPixelGrid PixelGrid { get; set; }

        [field: SerializeField]
        public float MassMultiplier { get; private set; } = 1;

        public int CurrentPixelCount => PixelGrid.PixelCount;
        public int StartPixelCount { get; private set; }

        public bool HasSprite => sprite != null && sprite.ToString() != "null";
        public IPixelCollisionHandler CollisionHandler { get; private set; }

        public Rigidbody2D Rigidbody { get; private set; }
        public SpriteRenderer SpriteRenderer { get; set; }

        public void ApplyPixels()
        {
            PixelGrid.ApplyPixels();
        }

        public Color32 GetColor(Vector2Int point)
        {
            return PixelGrid.GetValue(point);
        }

        public bool IsPixel(Vector2Int point)
        {
            return InBounds(point) && IsPixelAssumeInBounds(point);
        }

        public bool IsPixelAssumeInBounds(Vector2Int point)
        {
            return PixelGrid.IsPixelAssumeInBounds(point);
        }

        public bool InBounds(Vector2Int point)
        {
            return PixelGrid.InBounds(point);
        }

        public Vector2Int Dimensions()
        {
            return PixelGrid.Dimensions();
        }

        public void SetPixel(Vector2Int point, Color32 color)
        {
            PixelGrid.SetPixel(point, color);
        }

        public void SetTextureFromColors(Color32[,] colors)
        {
            Setup(colors, true);
        }

        public void SetPixelNoApply(Vector2Int point, Color32 color)
        {
            PixelGrid.SetPixelNoApply(point, color);
        }

        public void RemovePixels(IEnumerable<Vector2Int> points, bool simulateCollision = false)
        {
            var pointsArray = points as Vector2Int[] ?? points.AsValueEnumerable().ToArray();

            if (!pointsArray.AsValueEnumerable().Any()) return;

            HealthGrid?.RemovePixels(pointsArray);

            var countBefore = PixelGrid.PixelCount;
            PixelGrid.RemovePixels(pointsArray);
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

            return new Vector2(position.x + (float)PixelGrid.Width / 2, position.y + (float)PixelGrid.Height / 2);
        }

        public Vector2Int WorldToLocalPixel(Vector2 worldPosition)
        {
            var position = WorldToLocalPoint(worldPosition);

            return new Vector2Int((int)position.x, (int)position.y);
        }

        public Vector2 LocalToWorldPoint(Vector2Int localPosition)
        {
            Vector2 position = transform.TransformPoint(new Vector2(localPosition.x - (float)PixelGrid.Width / 2,
                localPosition.y - (float)PixelGrid.Height / 2));

            return position;
        }

        public Vector2 LocalToWorldPoint(Vector2 localPosition)
        {
            Vector2 position = transform.TransformPoint(new Vector2(localPosition.x - (float)PixelGrid.Width / 2,
                localPosition.y - (float)PixelGrid.Height / 2));

            return position;
        }

        public event Action<IPixelated> OnNoPixelsLeft;

        public event Action<List<Vector2Int>, PixelLoseReason> OnPixelsLost;

#if UNITY_INCLUDE_TESTS
        internal void SetSpriteForTesting(Sprite testSprite)
        {
            sprite = testSprite;
        }
#endif

        public void Setup(Color32[,] colors = null, bool forceSetup = false)
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

            PixelGrid = new PixelGrid(SpriteRenderer);

            if (_collisionEventChannelSO != null && _debrisSpawner != null)
                CollisionHandler = new PixelCollisionHandler(PixelGrid, this, GetComponent<PolygonCollider2D>(),
                    _collisionEventChannelSO, _debrisSpawner);

            if (colors is not null)
            {
                PixelGrid.SetTextureFromColors(colors);
            }
            else if (HasSprite)
            {
                var (colorsArray, width, height) =
                    (sprite.texture.GetPixels32(), sprite.texture.width, sprite.texture.height);
                colorsArray = EasyImage.ReorientTexture(colorsArray, width, height, flipX, flipY);
                (colorsArray, width, height) = EasyImage.RotateTexture(colorsArray, width, height, rotation);
                PixelGrid.SetTextureFromColors(colorsArray, width, height);
            }

            PixelGrid.Setup();

            HealthGrid = new HealthGrid(PixelGrid.Width, PixelGrid.Height, defaultPixelHealth);
            HealthGrid.InitializeFromGrid(PixelGrid);

            if (HasArmorMap)
                ApplyArmorMap();

            StartPixelCount = PixelGrid.PixelCount;

            WeightedCenter = CalculateWeightedCenter();

            OnPixelsLost?.Invoke(new List<Vector2Int>(), PixelLoseReason.Other);
        }

        private void ApplyArmorMap()
        {
            var armorPixels = armorMap.texture.GetPixels32();
            var armorWidth = armorMap.texture.width;
            var armorHeight = armorMap.texture.height;

            armorPixels = EasyImage.ReorientTexture(armorPixels, armorWidth, armorHeight, flipX, flipY);
            (armorPixels, armorWidth, armorHeight) =
                EasyImage.RotateTexture(armorPixels, armorWidth, armorHeight, rotation);

            if (armorWidth != PixelGrid.Width || armorHeight != PixelGrid.Height)
            {
                Debug.LogError(
                    $"[PixelatedRigidbody] Armor map size ({armorWidth}x{armorHeight}) doesn't match " +
                    $"sprite size ({PixelGrid.Width}x{PixelGrid.Height}) on '{name}'. Armor map ignored.");
                return;
            }

            HealthGrid.ApplyArmorMap(armorPixels, armorWidth, armorHeight, maxArmorHealth);
        }

        private Vector2 CalculateWeightedCenter()
        {
            var sum = Vector2.zero;
            var dims = PixelGrid.Dimensions();

            for (var x = 0; x < dims.x; x++)
            for (var y = 0; y < dims.y; y++)
                if (PixelGrid.IsPixelAssumeInBounds(new Vector2Int(x, y)))
                    sum += new Vector2(x, y);

            return PixelGrid.PixelCount > 0 ? sum / PixelGrid.PixelCount : PixelGrid.Center;
        }

        private void NudgeWeightedCenter(IEnumerable<Vector2Int> removedPixels, int countBefore)
        {
            if (PixelGrid.PixelCount <= 0)
            {
                WeightedCenter = PixelGrid.Center;
                return;
            }

            var removedSum = removedPixels.AsValueEnumerable()
                .Aggregate(Vector2.zero, (current, p) => current + new Vector2(p.x, p.y));

            WeightedCenter = (WeightedCenter * countBefore - removedSum) / PixelGrid.PixelCount;
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

            var countBefore = PixelGrid.PixelCount + region.Count;
            NudgeWeightedCenter(region, countBefore);
            OnPixelsLost?.Invoke(region.AsValueEnumerable().ToList(), PixelLoseReason.Division);
        }
    }
}