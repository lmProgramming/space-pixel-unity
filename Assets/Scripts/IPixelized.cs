using UnityEngine;

public interface IPixelized
{
    public void ResolveCollision(IPixelized other, Collision2D collision);
}