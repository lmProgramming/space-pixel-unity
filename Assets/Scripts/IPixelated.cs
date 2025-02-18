using UnityEngine;

public interface IPixelated
{
    public void ResolveCollision(IPixelated other, Collision2D collision);
}