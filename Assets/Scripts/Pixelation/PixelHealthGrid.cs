using System.Collections.Generic;
using Core.Grid;
using UnityEngine;
using ZLinq;

namespace Pixelation
{
    public class PixelHealthGrid
    {
        private readonly float _defaultMaxHealth;
        private float[] _health;
        private int _height;
        private int _width;

        public PixelHealthGrid(int width, int height, float defaultMaxHealth)
        {
            _width = width;
            _height = height;
            _defaultMaxHealth = defaultMaxHealth;
            _health = new float[width * height];
        }

        public float TotalHealth { get; private set; }

        public void InitializeFromGrid(IPixelGrid grid)
        {
            _width = grid.Width;
            _height = grid.Height;
            _health = new float[_width * _height];
            TotalHealth = 0f;

            for (var y = 0; y < _height; y++)
            for (var x = 0; x < _width; x++)
            {
                var point = new Vector2Int(x, y);
                var h = grid.IsPixelAssumeInBounds(point) ? _defaultMaxHealth : 0f;
                _health[y * _width + x] = h;
                TotalHealth += h;
            }
        }

        public float GetHealth(Vector2Int point)
        {
            return _health[point.y * _width + point.x];
        }

        public void SetHealth(Vector2Int point, float health)
        {
            var index = point.y * _width + point.x;
            TotalHealth += health - _health[index];
            _health[index] = health;
        }

        public bool DamagePixel(Vector2Int point, float damage)
        {
            var index = point.y * _width + point.x;
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
            var index = point.y * _width + point.x;
            TotalHealth -= _health[index];
            _health[index] = 0f;
        }

        public bool IsAlive(Vector2Int point)
        {
            return _health[point.y * _width + point.x] > 0f;
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
                var index = y * _width + x;
                if (_health[index] <= 0f) continue;

                var before = _health[index];
                var brightness = armorPixels[y * armorWidth + x].r / 255f;
                _health[index] = Mathf.Lerp(_defaultMaxHealth, maxArmorHealth, brightness);
                TotalHealth += _health[index] - before;
            }
        }

        public PixelHealthGrid CreateSubGrid(Vector2Int bottomLeft, int width, int height,
            HashSet<Vector2Int> points)
        {
            var subGrid = new PixelHealthGrid(width, height, _defaultMaxHealth);

            foreach (var point in points)
            {
                var localPoint = new Vector2Int(point.x - bottomLeft.x, point.y - bottomLeft.y);
                subGrid.SetHealth(localPoint, GetHealth(point));
            }

            return subGrid;
        }
    }
}