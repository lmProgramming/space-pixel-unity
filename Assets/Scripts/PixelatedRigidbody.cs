using System.Collections.Generic;
using System.Linq;
using ContourTracer;
using UnityEngine;

public class PixelatedRigidbody : MonoBehaviour, IPixelized
{
    [SerializeField] private Sprite sprite;

    [SerializeField] private Texture2D texture;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Outliner outliner;

    [SerializeField] private float pixelsPerUnit = 100f;

    [SerializeField] private Vector2 centerPivot = new(0.5f, 0.5f);

    [SerializeField] private float tolerance;

    [SerializeField] private PolygonCollider2D polygonCollider2D;

    private bool _didCollide;

    public Rigidbody2D Rigidbody { get; private set; }

    private float PixelUnitSize => 1 / pixelsPerUnit;

    private float UnitWidth => texture.width / pixelsPerUnit;

    private float UnitHeight => texture.height / pixelsPerUnit;

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    public void Start()
    {
        GetPolygonCollider();

        SetupRendering();

        RecalculateColliders();
    }

    private void Update()
    {
        _didCollide = false;
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_didCollide) return;

        var otherRb = collision.gameObject.GetComponent<PixelatedRigidbody>();

        if (otherRb is null) return;

        otherRb.ResolveCollision(this, collision);

        ResolveCollision(otherRb, collision);
    }

    public void ResolveCollision(IPixelized other, Collision2D collision)
    {
        _didCollide = true;
        DamageAt(collision.contacts[0].point);
    }

    public void GetPolygonCollider()
    {
        polygonCollider2D = GetComponent<PolygonCollider2D>();

        if (polygonCollider2D == null) polygonCollider2D = gameObject.AddComponent<PolygonCollider2D>();
    }

    private void SetupRendering()
    {
        texture = new Texture2D(sprite.texture.width, sprite.texture.height)
        {
            filterMode = FilterMode.Point
        };
        texture.SetPixels(sprite.texture.GetPixels());
        texture.Apply();

        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
        spriteRenderer.sprite = sprite;
    }

    private void RecalculateColliders()
    {
        GenerateColliders(sprite.texture);
    }

    private static Texture2D ConvertToTexture(Color[,] array)
    {
        var width = array.GetLength(0);
        var height = array.GetLength(1);

        int minX = width, maxX = 0, minY = height, maxY = 0;
        var hasVisiblePixels = false;

        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
        {
            if (!(array[x, y].a > 0)) continue;
            hasVisiblePixels = true;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        if (!hasVisiblePixels) return null;

        var newWidth = maxX - minX + 1;
        var newHeight = maxY - minY + 1;
        var texture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);

        for (var x = 0; x < newWidth; x++)
        for (var y = 0; y < newHeight; y++)
            texture.SetPixel(x, y, array[minX + x, minY + y]);

        texture.Apply();
        return texture;
    }


    public void DamageAt(Vector2 point)
    {
        Vector2 position = transform.InverseTransformPoint(point);

        var arrayPosition = new Vector2Int((int)((position.x + UnitWidth / 2) / PixelUnitSize),
            (int)((position.y + UnitHeight / 2) / PixelUnitSize));

        sprite.texture.SetPixel(arrayPosition.x, arrayPosition.y, Color.clear);
        sprite.texture.Apply();

        RecalculateColliders();

        Debug.Log(position);
    }

    private void GenerateColliders(Texture2D usedTexture)
    {
        var boundaryTracer = new ContourTracer.ContourTracer();

        boundaryTracer.Trace(usedTexture, centerPivot, pixelsPerUnit, outliner.gapLength, outliner.product);

        Vector2[] path;
        var points = new List<Vector2>();

        polygonCollider2D.pathCount = boundaryTracer.ContourCount;

        var paths = new List<List<Vector2>>();
        for (var i = 0; i < polygonCollider2D.pathCount; i++)
        {
            path = boundaryTracer.GetContour(i);
            LineUtility.Simplify(path.ToList(), tolerance, points);

            if (points.Count < 3)
            {
                polygonCollider2D.pathCount--;
                i--;
                continue;
            }

            paths.Add(points);
        }

        if (polygonCollider2D.pathCount == 0)
        {
            Debug.LogWarning("No path found");

            polygonCollider2D.pathCount = 1;

            paths.Add(FallbackPath());
        }

        for (var i = 0; i < polygonCollider2D.pathCount; i++)
        {
            points = paths[i];

            polygonCollider2D.SetPath(i, points);
        }
    }

    private List<Vector2> FallbackPath()
    {
        var halfRealWidth = UnitWidth / 2;
        var halfRealHeight = UnitHeight / 2;

        var points = new List<Vector2>
        {
            new(-halfRealWidth, halfRealHeight),
            new(-halfRealWidth, -halfRealHeight),
            new(halfRealWidth, -halfRealHeight),
            new(halfRealWidth, halfRealHeight)
        };

        return points;
    }
}