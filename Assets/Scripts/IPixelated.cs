using UnityEngine;

public interface IPixelated
{
    public void ResolveCollision(IPixelated other, Collision2D collision);

    public void RemovePixelAt(Vector2Int point);

    public Vector2Int WorldToLocalPoint(Vector2 worldPosition);

    public void SetupFromColors(Color[,] colors);
}