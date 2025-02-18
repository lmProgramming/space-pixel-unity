using System;
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

    [SerializeField] private float lineSimplificationTolerance;

    [SerializeField] private PolygonCollider2D polygonCollider2D;

    private bool _didCollide;

    private Sprite _internalSprite;

    private Pixel[,] _pixels;

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
        SetupRendering();

        CalculatePixels();

        GetPolygonCollider();

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
        DamageAt(collision.contacts[0].point, collision);
    }

    private void CalculatePixels()
    {
        _pixels = new Pixel[texture.width, texture.height];

        for (var x = 0; x < texture.width; x++)
        for (var y = 0; y < texture.height; y++)
        {
            var color = texture.GetPixel(x, y);
            _pixels[x, y] = new Pixel(color, 100);
        }
    }

    public event Action<IPixelized> OnNoPixelsLeft;

    private void GetPolygonCollider()
    {
        polygonCollider2D = GetComponent<PolygonCollider2D>() ?? gameObject.AddComponent<PolygonCollider2D>();
    }

    private void SetupRendering()
    {
        texture = new Texture2D(sprite.texture.width, sprite.texture.height)
        {
            filterMode = FilterMode.Point
        };
        texture.SetPixels(sprite.texture.GetPixels());
        texture.Apply();

        _internalSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
        spriteRenderer.sprite = _internalSprite;
    }

    private void RecalculateColliders()
    {
        GenerateColliders(_internalSprite.texture);
    }

    private void DamageAt(Vector2 point, Collision2D collision)
    {
        Vector2 position = transform.InverseTransformPoint(point);

        var hitPosition = new Vector2Int((int)((position.x + UnitWidth / 2) / PixelUnitSize),
            (int)((position.y + UnitHeight / 2) / PixelUnitSize));

        var pixelToDestroyPosition = GetPointAlongPath(hitPosition, -collision.rigidbody.linearVelocity, true) ??
                                     GetPointAlongPath(hitPosition, collision.rigidbody.linearVelocity, false);

        if (pixelToDestroyPosition == null) return;

        var pos = pixelToDestroyPosition.Value;
        _internalSprite.texture.SetPixel(pos.x, pos.y, Color.clear);
        _internalSprite.texture.Apply();

        RecalculateColliders();

        Debug.Log(position);
    }

    private Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
    {
        var pointsTraversed = GridMarcher.March(new Vector2Int(texture.width, texture.height), startPosition,
            direction);

        if (getLast) pointsTraversed.Reverse();

        foreach (var point in pointsTraversed.Where(point => texture.GetPixel(point.x, point.y).a != 0))
        {
            Debug.Log(point);
            return new Vector2Int(point.x, point.y);
        }

        return null;
    }

    private void GenerateColliders(Texture2D usedTexture)
    {
        var boundaryTracer = new ContourTracer.ContourTracer();

        boundaryTracer.Trace(usedTexture, centerPivot, pixelsPerUnit, outliner.gapLength, outliner.product);

        var points = new List<Vector2>();

        polygonCollider2D.pathCount = boundaryTracer.ContourCount;

        var paths = new List<List<Vector2>>();
        for (var i = 0; i < polygonCollider2D.pathCount; i++)
        {
            var path = boundaryTracer.GetContour(i);
            LineUtility.Simplify(path.ToList(), lineSimplificationTolerance, points);

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
            NoPixelsLeft();
            return;
        }

        for (var i = 0; i < polygonCollider2D.pathCount; i++)
        {
            points = paths[i];

            polygonCollider2D.SetPath(i, points);
        }
    }

    protected virtual void NoPixelsLeft()
    {
        OnNoPixelsLeft?.Invoke(this);
        Destroy(gameObject);
    }
}