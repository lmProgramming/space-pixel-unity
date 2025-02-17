using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Ship : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Outliner outliner;
    [SerializeField]
    private float pixelsPerUnit = 100f;
    [SerializeField] private Vector2 centerPivot;
    [SerializeField]
    private float tolerance = 0.1f;
    
    [FormerlySerializedAs("rigidbody2D")] [SerializeField]
    protected Rigidbody2D rb;
    
    void Start()
    {
        var array = new[,]
        {
            { Color.clear, Color.green, Color.clear,  Color.green,  Color.green,  Color.green,  Color.green },
            { Color.green, Color.red, Color.green,  Color.green,  Color.green,  Color.green,  Color.green },
            { Color.green, Color.red, Color.green,  Color.green,  Color.green,  Color.green,  Color.green },
            { Color.clear, Color.green, Color.clear,  Color.green,  Color.green,  Color.green,  Color.green },
        };
        var texture = ConvertToTexture(array);
        
        texture.filterMode = FilterMode.Point;

        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        spriteRenderer.sprite = sprite;
        
        GenerateColliders(texture);
    }

    private static Texture2D ConvertToTexture(Color[,] array)
    {
        var width = array.GetLength(0);
        var height = array.GetLength(1);

        int minX = width, maxX = 0, minY = height, maxY = 0;
        var hasVisiblePixels = false;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (!(array[x, y].a > 0)) continue;
                hasVisiblePixels = true;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (!hasVisiblePixels) return null;

        var newWidth = maxX - minX + 1;
        var newHeight = maxY - minY + 1;
        var texture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);

        for (var x = 0; x < newWidth; x++)
        {
            for (var y = 0; y < newHeight; y++)
            {
                texture.SetPixel(x, y, array[minX + x, minY + y]);
            }
        }

        texture.Apply();
        return texture;
    }

    private void Update()
    {
        Move();
    }

    protected virtual void Move()
    {
        
    }

    private void GenerateColliders(Texture2D texture)
    {
        Debug.Log(texture.width);
        var boundaryTracer = new ContourTracer();
        
        boundaryTracer.Trace(texture, centerPivot, pixelsPerUnit, outliner.gapLength, outliner.product);

        List<Vector2> path = new List<Vector2>();
        List<Vector2> points = new List<Vector2>();
        
        Debug.Log(texture.width);   

        var polygonCollider2D = gameObject.GetComponent<PolygonCollider2D>();
        if(!polygonCollider2D) polygonCollider2D = gameObject.AddComponent<PolygonCollider2D>();

        polygonCollider2D.pathCount = boundaryTracer.pathCount;
        for (int i = 0; i < polygonCollider2D.pathCount; i++)
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
}
