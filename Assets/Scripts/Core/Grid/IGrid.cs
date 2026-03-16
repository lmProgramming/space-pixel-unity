using UnityEngine;

namespace Core.Grid
{
    public interface IGrid<out T>
    {
        Vector2 Center { get; }
        int Width { get; }
        int Height { get; }
        T GetValue(Vector2Int point);
        bool InBounds(Vector2Int point);
        Vector2Int Dimensions();
    }
}