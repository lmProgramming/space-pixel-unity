using System;
using System.Collections.Generic;
using System.Linq;
using ContourTracer;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PixelatedRigidbody : MonoBehaviour, IPixelated
{
    [SerializeField] private Sprite sprite;

    [SerializeField] private Outliner outliner;

    [SerializeField] private float pixelsPerUnit = 100f;

    [SerializeField] private Vector2 centerPivot = new(0.5f, 0.5f);

    [SerializeField] private float lineSimplificationTolerance;

    private bool _didCollide;

    private Sprite _internalSprite;

    private Pixel[,] _pixels;

    private PolygonCollider2D _polygonCollider2D;

    private SpriteRenderer _spriteRenderer;

    private Texture2D _texture;

    public Rigidbody2D Rigidbody { get; private set; }

    private float PixelUnitSize => 1 / pixelsPerUnit;

    private float UnitWidth => _texture.width / pixelsPerUnit;

    private float UnitHeight => _texture.height / pixelsPerUnit;

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    public void Start()
    {
        Setup();
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

    public void ResolveCollision(IPixelated other, Collision2D collision)
    {
        _didCollide = true;
        DamageAt(collision.contacts[0].point, collision);
    }

    public void SetupFromColors(Color[,] colors)
    {
        _texture = new Texture2D(colors.GetLength(0), colors.GetLength(1), TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };

        var colorsArray = colors.OfType<Color>().ToArray();

        _texture.SetPixels(colorsArray);
        sprite = Sprite.Create(_texture, new Rect(0, 0, _texture.width, _texture.height), new Vector2(0.5f, 0.5f));

        Setup();
    }

    public void Setup()
    {
        GetComponents();

        SetupRendering();

        CalculatePixels();

        RecalculateColliders();
    }

    protected void CalculatePixels()
    {
        _pixels = new Pixel[_texture.width, _texture.height];

        for (var x = 0; x < _texture.width; x++)
        for (var y = 0; y < _texture.height; y++)
        {
            var color = _texture.GetPixel(x, y);
            _pixels[x, y] = new Pixel(color, 100);
        }
    }

    public event Action<IPixelated> OnNoPixelsLeft;

    private void GetComponents()
    {
        _polygonCollider2D = GetComponent<PolygonCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    private void SetupRendering()
    {
        _texture = new Texture2D(sprite.texture.width, sprite.texture.height)
        {
            filterMode = FilterMode.Point
        };
        _texture.SetPixels(sprite.texture.GetPixels());
        _texture.Apply();

        _internalSprite = Sprite.Create(_texture, new Rect(0, 0, _texture.width, _texture.height),
            new Vector2(0.5f, 0.5f));
        _spriteRenderer.sprite = _internalSprite;
    }

    private void RecalculateColliders()
    {
        var gridContourTracer = new GridContourTracer();
        var polygon = gridContourTracer.GenerateCollider(_texture, centerPivot, pixelsPerUnit);
        if (polygon is null)
        {
            NoPixelsLeft();
            return;
        }

        var points = new List<Vector2>();

        LineUtility.Simplify(polygon.ToList(), lineSimplificationTolerance, points);

        _polygonCollider2D.pathCount = 1;
        _polygonCollider2D.SetPath(0, points);

        // var boundaryTracer = new ContourTracer.ContourTracer();
        //
        // boundaryTracer.Trace(_internalSprite.texture, centerPivot, pixelsPerUnit, outliner.gapLength, outliner.product);
        //
        // var points = new List<Vector2>();
        //
        // _polygonCollider2D.pathCount = boundaryTracer.ContourCount;
        //
        // var paths = new List<List<Vector2>>();
        // for (var i = 0; i < _polygonCollider2D.pathCount; i++)
        // {
        //     var path = boundaryTracer.GetContour(i);
        //     LineUtility.Simplify(path.ToList(), lineSimplificationTolerance, points);
        //
        //     if (points.Count < 3)
        //     {
        //         _polygonCollider2D.pathCount--;
        //         i--;
        //         continue;
        //     }
        //
        //     paths.Add(points);
        // }
        //
        // if (_polygonCollider2D.pathCount == 0)
        // {
        //     NoPixelsLeft();
        //     return;
        // }
        //
        // for (var i = 0; i < _polygonCollider2D.pathCount; i++)
        // {
        //     points = paths[i];
        //
        //     _polygonCollider2D.SetPath(i, points);
        // }
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
        RemovePixelAt(pos);
    }

    private void RemovePixelAt(Vector2Int point)
    {
        _internalSprite.texture.SetPixel(point.x, point.y, Color.clear);
        _internalSprite.texture.Apply();

        var regions = FloodFindCohesiveRegions(point);

        if (regions.Count > 1) HandleDivision(regions);

        RecalculateColliders();
    }

    private void HandleDivision(List<HashSet<Vector2Int>> regions)
    {
        var colors = new[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow
        };

        regions = regions.OrderBy(r => r.Count).ToList();

        for (var index = 0; index < regions.Count - 1; index++)
        {
            var region = regions[index];

            Debug.Log(region.Count);

            foreach (var point in region)
                SetColorNoApply(point, colors[index]);

            CreateNewJunk(region);
        }

        ApplyColors();
        RecalculateColliders();
    }

    private void CreateNewJunk(HashSet<Vector2Int> points)
    {
        var rightTopPoint = new Vector2Int(points.Max(p => p.x), points.Max(p => p.y));
        var leftBottomPoint = new Vector2Int(points.Min(p => p.x), points.Min(p => p.y));

        var width = rightTopPoint.x - leftBottomPoint.x + 1;
        var height = rightTopPoint.y - leftBottomPoint.y + 1;

        var newColorsGrid = new Color[width, height];

        foreach (var point in points)
        {
            newColorsGrid[point.x - leftBottomPoint.x, point.y - leftBottomPoint.y] = GetColor(point);
            SetColorNoApply(point, Color.clear);
        }

        var globalPosition = transform.TransformPoint((Vector2)leftBottomPoint);

        JunkSpawner.Instance.SpawnJunk(globalPosition, transform.rotation, newColorsGrid);
    }

    private List<HashSet<Vector2Int>> FloodFindCohesiveRegions(Vector2Int searchStartPoint)
    {
        var visited = new HashSet<Vector2Int>
        {
            searchStartPoint
        };

        var regions = new List<HashSet<Vector2Int>>();

        SetupFlooding(searchStartPoint + new Vector2Int(1, 0));
        SetupFlooding(searchStartPoint + new Vector2Int(-1, 0));
        SetupFlooding(searchStartPoint + new Vector2Int(0, 1));
        SetupFlooding(searchStartPoint + new Vector2Int(0, -1));

        return regions;

        void SetupFlooding(Vector2Int searchStart)
        {
            if (!InBounds(searchStart)) return;

            regions.Add(new HashSet<Vector2Int> { searchStart });

            FloodFind(searchStart, regions.Count - 1);
        }

        void FloodFind(Vector2Int position, int regionIndex)
        {
            if (regionIndex == regions.Count) return;

            if (!InBounds(position)) return;

            if (!visited.Add(position))
            {
                FindRegionToMerge(position, regionIndex);
                return;
            }

            if (!IsPixel(position)) return;

            regions[regionIndex].Add(position);

            FloodFind(position + new Vector2Int(1, 0), regionIndex);
            FloodFind(position + new Vector2Int(-1, 0), regionIndex);
            FloodFind(position + new Vector2Int(0, 1), regionIndex);
            FloodFind(position + new Vector2Int(0, -1), regionIndex);
        }

        void FindRegionToMerge(Vector2Int position, int regionIndex)
        {
            for (var index = 0; index < regions.Count; index++)
            {
                var region = regions[index];

                if (!region.Contains(position)) continue;

                if (index == regionIndex) break;

                MergeRegions(index, regionIndex);
            }
        }

        void MergeRegions(int indexToMergeWith, int indexMerged)
        {
            regions[indexToMergeWith].UnionWith(regions[indexMerged]);

            regions.RemoveAt(indexMerged);
        }
    }

    private void SetColorNoApply(Vector2Int point, Color color)
    {
        _texture.SetPixel(point.x, point.y, color);
    }

    private void ApplyColors()
    {
        _texture.Apply();
    }

    private Color GetColor(Vector2Int point)
    {
        return _texture.GetPixel(point.x, point.y);
    }

    private bool IsPixel(Vector2Int point)
    {
        return _texture.GetPixel(point.x, point.y).a != 0;
    }

    private bool InBounds(Vector2Int point)
    {
        return point.x >= 0 && point.x < _texture.width && point.y >= 0 && point.y < _texture.height;
    }

    private Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
    {
        var pointsTraversed = GridMarcher.March(new Vector2Int(_texture.width, _texture.height), startPosition,
            direction);

        if (getLast) pointsTraversed.Reverse();

        foreach (var point in pointsTraversed.Where(point => _texture.GetPixel(point.x, point.y).a != 0))
            return new Vector2Int(point.x, point.y);

        return null;
    }

    protected virtual void NoPixelsLeft()
    {
        OnNoPixelsLeft?.Invoke(this);
        Destroy(gameObject);
    }
}