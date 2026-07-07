using System;
using System.Collections.Generic;
using UnityEngine;

<<<<<<<<
Assets / Scripts / Core / Ships / Snapshots / PixelatedRigidbody / Internals / PixelGridSnapshot.cs

namespace Core.Ships.Snapshots.PixelatedRigidbody.Internals

========

namespace Core.Ships
>>>>>>>>
Assets / Scripts / Core / Ships / PixelGridSnapshot.cs
{
    [Serializable]
    public class PixelGridSnapshot
{
public int width;
public int height;

/// <summary>
///     Flattened array of pixel colors. Empty pixels are transparent (alpha = 0).
///     Row-major order: index = y * width + x
/// </summary>
public Color32[] pixels;

public PixelGridSnapshot()
{
}

public PixelGridSnapshot(int width, int height)
{
    this.width = width;
    this.height = height;
    pixels = new Color32[width * height];
}

public Color32 GetPixel(int x, int y)
{
    if (x < 0 || x >= width || y < 0 || y >= height)
        return default;
    return pixels[y * width + x];
}

public void SetPixel(int x, int y, Color32 color)
{
    if (x < 0 || x >= width || y < 0 || y >= height)
        return;
    if (pixels == null || pixels.Length != width * height)
        return;
    pixels[y * width + x] = color;
}

public bool IsPixel(int x, int y)
{
    if (x < 0 || x >= width || y < 0 || y >= height)
        return false;
    if (pixels == null || pixels.Length != width * height)
        return false;
    return pixels[y * width + x].a > 0;
}

public void RemovePixel(int x, int y)
{
    SetPixel(x, y, new Color32(0, 0, 0, 0));
}

public List<Vector2Int> GetAllNonTransparentPixelPositions()
{
    var positions = new List<Vector2Int>();
    for (var y = 0; y < height; y++)
    for (var x = 0; x < width; x++)
        if (IsPixel(x, y))
            positions.Add(new Vector2Int(x, y));

    return positions;
}
}
}