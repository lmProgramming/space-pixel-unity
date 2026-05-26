using UnityEngine;

namespace Services
{
    public class SkirmishSpawnArea : MonoBehaviour
    {
        [SerializeField] private Vector2 min = new(-150f, -260f);
        [SerializeField] private Vector2 max = new(260f, -10f);
        [SerializeField] private float margin;

        public Rect GetSpawnRect()
        {
            var minX = Mathf.Min(min.x, max.x) + margin;
            var maxX = Mathf.Max(min.x, max.x) - margin;
            var minY = Mathf.Min(min.y, max.y) + margin;
            var maxY = Mathf.Max(min.y, max.y) - margin;
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}