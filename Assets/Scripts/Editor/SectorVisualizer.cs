using Services;
using UnityEngine;

namespace Editor
{
    public class SectorVisualizer : MonoBehaviour
    {
        [SerializeField] private bool onlyShowInCameraView = true;
        [SerializeField] private bool showGrid = true;
        [SerializeField] private float gizmoZ;
        [SerializeField] private SectorService sectorService;

        private void OnDrawGizmos()
        {
            var sectorSize = sectorService.SectorSize;
            if (sectorSize <= 0) return;

            var cache = sectorService.Cache;
            var cacheDuration = sectorService.CacheDuration;
            var currentTime = Application.isPlaying ? Time.time : 0;

            var cam = Camera.main;
            var hasCam = cam != null;

            float minX = 0, maxX = 0, minY = 0, maxY = 0;
            if (hasCam)
            {
                var height = 2f * cam.orthographicSize;
                var width = height * cam.aspect;
                var camPos = cam.transform.position;

                minX = camPos.x - width / 2f;
                maxX = camPos.x + width / 2f;
                minY = camPos.y - height / 2f;
                maxY = camPos.y + height / 2f;
            }

            if (showGrid && hasCam) DrawVisibleGrid(minX, maxX, minY, maxY, sectorSize);

            if (cache == null) return;

            foreach (var kvp in cache)
            {
                var center = kvp.Key;
                var result = kvp.Value;

                if (onlyShowInCameraView && hasCam)
                {
                    var halfSize = sectorSize / 2f;
                    if (center.x + halfSize < minX || center.x - halfSize > maxX ||
                        center.y + halfSize < minY || center.y - halfSize > maxY)
                        continue;
                }

                var age = currentTime - result.GenerationTime;
                var ratio = Mathf.Clamp01(age / cacheDuration);

                var baseColor = result.Empty ? Color.green : Color.red;
                // Darken as it approaches expiration. If expired, it stays very dark.
                var darkness = age > cacheDuration ? 0.85f : ratio * 0.7f;
                var color = Color.Lerp(baseColor, Color.black, darkness);
                color.a = 0.5f;

                Gizmos.color = color;
                var gizmoCenter = new Vector3(center.x, center.y, gizmoZ);
                Gizmos.DrawCube(gizmoCenter, new Vector3(sectorSize * 0.95f, sectorSize * 0.95f, 0.01f));

                if (age > cacheDuration)
                {
                    Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    Gizmos.DrawWireCube(gizmoCenter, new Vector3(sectorSize, sectorSize, 0.01f));
                }
            }
        }

        private void DrawVisibleGrid(float minX, float maxX, float minY, float maxY, float sectorSize)
        {
            Gizmos.color = new Color(1, 1, 1, 0.05f);

            // Align grid to the sector centers
            var startX = Mathf.Round(minX / sectorSize) * sectorSize;
            var startY = Mathf.Round(minY / sectorSize) * sectorSize;

            for (var x = startX - sectorSize; x <= maxX + sectorSize; x += sectorSize)
                Gizmos.DrawLine(new Vector3(x - sectorSize / 2f, minY, gizmoZ),
                    new Vector3(x - sectorSize / 2f, maxY, gizmoZ));
            for (var y = startY - sectorSize; y <= maxY + sectorSize; y += sectorSize)
                Gizmos.DrawLine(new Vector3(minX, y - sectorSize / 2f, gizmoZ),
                    new Vector3(maxX, y - sectorSize / 2f, gizmoZ));
        }
    }
}