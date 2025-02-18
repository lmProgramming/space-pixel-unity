using System;
using System.Collections.Generic;
using System.Linq;
using ContourTracer;
using UnityEngine;

public class PixelatedRigidbody : MonoBehaviour, IPixelated
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
        texture = new Texture2D(colors.GetLength(0), colors.GetLength(1), TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };

        var colorsArray = colors.OfType<Color>().ToArray();

        texture.SetPixels(colorsArray);
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        Setup();
    }

    public void Setup()
    {
        SetupRendering();

        CalculatePixels();

        GetPolygonCollider();

        RecalculateColliders();
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

    public event Action<IPixelated> OnNoPixelsLeft;

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
        RemovePixelAt(pos);

        Debug.Log(position);
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
        var colors = new List<Color>
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan
        };

        var cInd = 0;

        regions = regions.OrderBy(r => r.Count).ToList();

        for (var index = 0; index < regions.Count - 1; index++)
        {
            var region = regions[index];
            CreateNewJunk(region);
        }
    }

    private void CreateNewJunk(HashSet<Vector2Int> points)
    {
        var rightTopPoint = new Vector2Int(points.Max(p => p.x), points.Max(p => p.y));
        var leftBottomPoint = new Vector2Int(points.Min(p => p.x), points.Min(p => p.y));

        var width = rightTopPoint.x - leftBottomPoint.x + 1;
        var height = rightTopPoint.y - leftBottomPoint.y + 1;

        var newColorsGrid = new Color[width, height];

        foreach (var point in points)
            newColorsGrid[point.x - leftBottomPoint.x, point.y - leftBottomPoint.y] = GetColor(point);

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

    private void SetColor(Vector2Int point, Color color)
    {
        texture.SetPixel(point.x, point.y, color);
    }

    private Color GetColor(Vector2Int point)
    {
        return texture.GetPixel(point.x, point.y);
    }

    private bool IsPixel(Vector2Int point)
    {
        return texture.GetPixel(point.x, point.y).a != 0;
    }

    private bool InBounds(Vector2Int point)
    {
        return point.x >= 0 && point.x < texture.width && point.y >= 0 && point.y < texture.height;
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