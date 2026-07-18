using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Services;
using Services;
using UnityEngine;

[assembly: InternalsVisibleTo("E2E")]

namespace Editor.Standalone
{
    public class SectorVisualizer : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private bool onlyShowInCameraView = true;
        [SerializeField] private bool showGrid = true;
        [SerializeField] private float gizmoZ;

        [SerializeField]
        private NavigationService navigationService;

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void OnDrawGizmos()
        {
            var sectorSize = navigationService.InternalSectorSize;
            if (sectorSize <= 0) return;

            var cache = navigationService.InternalCache;
            var cacheDuration = navigationService.InternalCacheDuration;

            if (showGrid && _camera) DrawGrid(cache, sectorSize, cacheDuration);
        }

        private void DrawGrid(IReadOnlyDictionary<Vector2, SectorResult> cache, float sectorSize, float cacheDuration)
        {
            if (cache == null) return;
            var currentTime = Application.isPlaying ? Time.time : 0;

            var height = 2f * _camera.orthographicSize;
            var width = height * _camera.aspect;
            var camPos = _camera.transform.position;

            var minX = camPos.x - width / 2f;
            var maxX = camPos.x + width / 2f;
            var minY = camPos.y - height / 2f;
            var maxY = camPos.y + height / 2f;

            foreach (var (center, result) in cache)
            {
                if (onlyShowInCameraView)
                {
                    var halfSize = sectorSize / 2f;
                    if (center.x + halfSize < minX || center.x - halfSize > maxX ||
                        center.y + halfSize < minY || center.y - halfSize > maxY)
                        continue;
                }

                var age = currentTime - result.GenerationTime;

                Gizmos.color = GetSectorColor(result.IsEmpty, age, cacheDuration);
                var gizmoCenter = new Vector3(center.x, center.y, gizmoZ);
                Gizmos.DrawCube(gizmoCenter, new Vector3(sectorSize * 0.95f, sectorSize * 0.95f, 0.01f));

                if (!(age > cacheDuration)) continue;

                Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                Gizmos.DrawWireCube(gizmoCenter, new Vector3(sectorSize, sectorSize, 0.01f));
            }
        }

        private static Color GetSectorColor(bool empty, float age, float cacheDuration)
        {
            var baseColor = empty ? Color.green : Color.red;
            // Darken as it approaches expiration. If expired, it stays very dark.
            var darkness = age > cacheDuration ? 0.85f : Mathf.Clamp01(age / cacheDuration) * 0.7f;
            var color = Color.Lerp(baseColor, Color.black, darkness);
            color.a = 0.5f;
            return color;
        }

        public void RecalculateSectorGrid()
        {
            var sectorSize = navigationService.InternalSectorSize;
            var camPos = _camera.transform.position;

            var originX = Mathf.Floor(camPos.x / sectorSize) * sectorSize;
            var originY = Mathf.Floor(camPos.y / sectorSize) * sectorSize;

            const int halfCount = 5;
            var keys = new List<Vector2>(100);
            for (var col = -halfCount; col < halfCount; col++)
            for (var row = -halfCount; row < halfCount; row++)
                keys.Add(new Vector2(originX + col * sectorSize, originY + row * sectorSize));

            navigationService.ClearCacheEntries(keys);

            foreach (var key in keys)
                navigationService.GetSectorResult(new Vector3(key.x + sectorSize * 0.5f, key.y + sectorSize * 0.5f));
        }

        internal INavigationService InternalNavigationService
        {
            get => navigationService;
            set => navigationService = value as NavigationService;
        }
#endif
    }
}