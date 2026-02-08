using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Grid;
using Core.Pixelation;
using Core.Services;
using Cysharp.Threading.Tasks;
using Events.Collision;
using Grid;
using LM;
using UnityEngine;
using Zenject;

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

        [Inject] private CollisionEventChannelSO _collisionEventChannelSO;

        private bool _isSetup;
        [Inject] private IJunkSpawner _junkSpawner;

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
            CollisionHandler.OnCollision(collision);
        }

        public IPixelGrid PixelGrid { get; set; }

        [field: SerializeField]
        public float MassMultiplier { get; private set; } = 1;

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
            return PixelGrid.GetColor(point);
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
            Setup(colors);
        }

        public void SetPixelNoApply(Vector2Int point, Color32 color)
        {
            PixelGrid.SetPixelNoApply(point, color);
        }

        public void RemovePixels(IEnumerable<Vector2Int> points, bool simulateCollision = false)
        {
            var pointsArray = points as Vector2Int[] ?? points.ToArray();

            if (!pointsArray.Any()) return;

            PixelGrid.RemovePixels(pointsArray);

            OnPixelsLost?.Invoke(pointsArray.ToList(), PixelLoseReason.Destroyed);

            if (!simulateCollision) return;

            var contactPoint = LocalToWorldPoint(pointsArray.First());

            CollisionHandler.RaiseCollisionEvent(null, contactPoint, pointsArray);
        }

        public void RemovePixelAt(Vector2Int point, bool simulateCollision = false)
        {
            RemovePixels(new[] { point }, simulateCollision);
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

        public event Action<IPixelated> OnNoPixelsLeft;

        public event Action<List<Vector2Int>, PixelLoseReason> OnPixelsLost;

        public void Setup(Color32[,] colors = null, bool forceSetup = false)
        {
            if (_isSetup && !forceSetup) return;

            if (!sprite && colors is null) throw new UnityException("Sprite is null");

            _isSetup = true;

            GetComponents();

            PixelGrid = new PixelGrid(SpriteRenderer);

            CollisionHandler = new PixelCollisionHandler(PixelGrid, this, GetComponent<PolygonCollider2D>(),
                _collisionEventChannelSO, _junkSpawner);

            if (colors is not null) PixelGrid.SetTextureFromColors(colors);

            if (HasSprite)
            {
                var (colorsArray, width, height) =
                    (sprite.texture.GetPixels32(), sprite.texture.width, sprite.texture.height);
                colorsArray = EasyImage.ReorientTexture(colorsArray, width, height, flipX, flipY);
                (colorsArray, width, height) = EasyImage.RotateTexture(colorsArray, width, height, rotation);
                PixelGrid.SetTextureFromColors(colorsArray, width, height);
            }

            PixelGrid.Setup();

            OnPixelsLost?.Invoke(new List<Vector2Int>(), PixelLoseReason.Other);
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
            OnPixelsLost?.Invoke(region.ToList(), PixelLoseReason.Division);
        }
    }
}