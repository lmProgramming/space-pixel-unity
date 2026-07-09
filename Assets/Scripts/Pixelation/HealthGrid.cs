using System.Collections.Generic;
using Core.Grid;
using LMPro.DataStructures;
using UnityEngine;
using ZLinq;

namespace Pixelation
{
    public class HealthGrid : IGrid<float>
    {
        private readonly float _defaultMaxHealth;
        private float[] _health;

        public HealthGrid(int width, int height, float defaultMaxHealth)
        {
            Width = width;
            Height = height;
            _defaultMaxHealth = defaultMaxHealth;
            _health = new float[width * height];
        }

        public float TotalHealth { get; private set; }

        public Vector2 Center => new(Width / 2f, Height / 2f);
        public int Width { get; private set; }
        public int Height { get; private set; }

        public float GetValue(Vector2Int point)
        {
            return _health[point.y * Width + point.x];
        }

        public float[,] GetValues2D()
        {
            return Helpers.Make2DArray(_health, Height, Width);
        }

        public bool InBounds(Vector2Int point)
        {
            return point.x >= 0 && point.x < Width && point.y >= 0 && point.y < Height;
        }

        public Vector2Int Dimensions()
        {
            return new Vector2Int(Width, Height);
        }

        public void InitializeFromGrid(ITexturePixelGrid grid)
        {
            Width = grid.Width;
            Height = grid.Height;
            _health = new float[Width * Height];
            TotalHealth = 0f;

            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                var point = new Vector2Int(x, y);
                var h = grid.IsPixelAssumeInBounds(point) ? _defaultMaxHealth : 0f;
                _health[y * Width + x] = h;
                TotalHealth += h;
            }
        }

        public void SetHealth(Vector2Int point, float health)
        {
            var index = point.y * Width + point.x;
            TotalHealth += health - _health[index];
            _health[index] = health;
        }

        public bool DamagePixel(Vector2Int point, float damage)
        {
            var index = point.y * Width + point.x;
            if (_health[index] <= 0f) return false;

            var before = _health[index];
            _health[index] = Mathf.Max(0f, _health[index] - damage);
            TotalHealth -= before - _health[index];
            return _health[index] <= 0f;
        }

        public List<Vector2Int> DamagePixels(IEnumerable<Vector2Int> points, float damagePerPixel)
        {
            return points.AsValueEnumerable().Where(point => DamagePixel(point, damagePerPixel)).ToList();
        }

        public void RemovePixel(Vector2Int point)
        {
            var index = point.y * Width + point.x;
            TotalHealth -= _health[index];
            _health[index] = 0f;
        }

        public void RemovePixels(IEnumerable<Vector2Int> points)
        {
            foreach (var point in points) RemovePixel(point);
        }

        public bool IsAlive(Vector2Int point)
        {
            return _health[point.y * Width + point.x] > 0f;
        }

        /// <summary>
        ///     Overrides health for active pixels using a grayscale armor map.
        ///     Brightness (0–255) is linearly mapped: black = <see cref="_defaultMaxHealth" />,
        ///     white = <paramref name="maxArmorHealth" />.
        ///     Only affects pixels that are already alive (non-transparent in the color sprite).
        /// </summary>
        public void ApplyArmorMap(Color32[] armorPixels, int armorWidth, int armorHeight, float maxArmorHealth)
        {
            for (var y = 0; y < armorHeight; y++)
            for (var x = 0; x < armorWidth; x++)
            {
                var index = y * Width + x;
                if (_health[index] <= 0f) continue;

                var before = _health[index];
                var brightness = armorPixels[y * armorWidth + x].r / 255f;
                _health[index] = Mathf.Lerp(_defaultMaxHealth, maxArmorHealth, brightness);
                TotalHealth += _health[index] - before;
            }
        }

        public HealthGrid CreateSubGrid(Vector2Int bottomLeft, int width, int height,
            HashSet<Vector2Int> points)
        {
            var subGrid = new HealthGrid(width, height, _defaultMaxHealth);

            foreach (var point in points)
            {
                var localPoint = new Vector2Int(point.x - bottomLeft.x, point.y - bottomLeft.y);
                subGrid.SetHealth(localPoint, GetValue(point));
            }

            return subGrid;
        }
    }
}