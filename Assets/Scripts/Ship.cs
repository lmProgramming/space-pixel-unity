using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ship : MonoBehaviour
{
    [SerializeField] private Sprite shipSprite;

    [SerializeField] private Texture2D shipTexture;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Outliner outliner;

    [SerializeField] private float pixelsPerUnit = 100f;

    [SerializeField] private Vector2 centerPivot;

    [SerializeField] private float tolerance = 0.1f;

    [SerializeField] protected Rigidbody2D rb;

    [SerializeField] protected List<IWeapon> Weapons = new();

    private float pixelUnitSize => 1 / pixelsPerUnit;

    private float unitWidth => shipTexture.width / pixelsPerUnit;

    private float unitHeight => shipTexture.height / pixelsPerUnit;

    private void Start()
    {
        shipTexture = new Texture2D(shipSprite.texture.width, shipSprite.texture.height)
        {
            filterMode = FilterMode.Point
        };
        shipTexture.SetPixels(shipSprite.texture.GetPixels());
        shipTexture.Apply();

        shipSprite = Sprite.Create(shipTexture, new Rect(0, 0, shipTexture.width, shipTexture.height),
            new Vector2(0.5f, 0.5f));
        spriteRenderer.sprite = shipSprite;

        RecalculateColliders();

        Weapons = GetComponentsInChildren<IWeapon>().ToList();
    }

    private void Update()
    {
        Move();

        HandleWeapons();
    }

    public void RecalculateColliders()
    {
        var texture = shipSprite.texture;

        GenerateColliders(texture);
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

    protected virtual void Move()
    {
    }

    protected virtual void HandleWeapons()
    {
    }

    private void GenerateColliders(Texture2D texture)
    {
        var boundaryTracer = new ContourTracer();

        boundaryTracer.Trace(texture, centerPivot, pixelsPerUnit, outliner.gapLength, outliner.product);

        var path = new List<Vector2>();
        var points = new List<Vector2>();

        var polygonCollider2D = gameObject.GetComponent<PolygonCollider2D>();
        if (!polygonCollider2D) polygonCollider2D = gameObject.AddComponent<PolygonCollider2D>();

        polygonCollider2D.pathCount = boundaryTracer.pathCount;
        for (var i = 0; i < polygonCollider2D.pathCount; i++)
        {
            boundaryTracer.GetPath(i, ref path);
            LineUtility.Simplify(path, tolerance, points);
            if (points.Count < 3)
            {
                polygonCollider2D.pathCount--;
                i--;
            }
            else
            {
                polygonCollider2D.SetPath(i, points);
            }
        }
    }

    public void DamagePixelAt(Vector2 point)
    {
        Vector2 position = transform.InverseTransformPoint(point);

        Debug.DrawRay(position, Vector2.up * tolerance, Color.red);

        var arrayPosition = new Vector2Int((int)((position.x + unitWidth / 2) / pixelUnitSize),
            (int)((position.y + unitHeight / 2) / pixelUnitSize));

        shipSprite.texture.SetPixel(arrayPosition.x, arrayPosition.y, Color.clear);
        shipSprite.texture.Apply();

        RecalculateColliders();

        Debug.Log(position);
    }
}