using System;
using System.Collections.Generic;
using System.Linq;
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

        [SerializeField] private float lineSimplificationTolerance;
        private bool _isSetup;

        private PixelGrid PixelGrid { get; set; }
        public PixelCollisionHandler CollisionHandler { get; private set; }

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
            CollisionHandler.SetCollided(false);

            Rigidbody.collisionDetectionMode =
                Rigidbody.linearVelocity.sqrMagnitude > SpeedLimitForDiscreteCollisionDetectionSquared
                    ? CollisionDetectionMode2D.Continuous
                    : CollisionDetectionMode2D.Discrete;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CollisionHandler.OnCollision(collision);
        }

        public void RemovePixelAt(Vector2Int point)
        {
            RemovePixels(new[] { point });
        }

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

        public void SetPixel(Vector2Int point, Color32 color)
        {
            PixelGrid.SetPixel(point, color);
        }

        public void SetSpriteFromColors(Color32[,] colors)
        {
            Setup(colors);
        }

        public void SetPixelNoApply(Vector2Int point, Color32 color)
        {
            PixelGrid.SetPixelNoApply(point, color);
        }

        public void RemovePixels(IEnumerable<Vector2Int> points)
        {
            var pointsArray = points as Vector2Int[] ?? points.ToArray();

            if (!pointsArray.Any()) return;

            PixelGrid.RemovePixels(pointsArray);

            OnPixelsDestroyed?.Invoke(pointsArray.ToList());
        }

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
            Vector2 position = transform.TransformPoint((Vector2)localPosition);

            return position;
        }

        public void Setup(Color32[,] colors = null)
        {
            if (_isSetup) return;
            _isSetup = true;

            PixelGrid = new PixelGrid(GetComponent<SpriteRenderer>());

            CollisionHandler = new PixelCollisionHandler(PixelGrid, this, GetComponent<PolygonCollider2D>());

            if (colors is not null) PixelGrid.SetSpriteFromColors(colors);

            if (sprite.ToString() != "null") PixelGrid.SetSprite(sprite);

            PixelGrid.Setup();

            GetComponents();

            // CalculatePixels();

            CollisionHandler.RecalculateColliders();
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
        public event Action<List<Vector2Int>> OnPixelsDestroyed;

        private void GetComponents()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
        }

        public virtual void NoPixelsLeft()
        {
            OnNoPixelsLeft?.Invoke(this);
            Destroy(gameObject);
        }

        public void CopyVelocity(PixelatedRigidbody parentBody)
        {
            Rigidbody.linearVelocity = parentBody.Rigidbody.linearVelocity;
        }
    }
}