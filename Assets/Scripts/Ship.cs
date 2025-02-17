using UnityEngine;

public class Ship : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer spriteRenderer;
    
    void Start()
    {
        var array = new Color[3, 3]
        {
            { Color.clear, Color.green, Color.clear },
            { Color.green, Color.red, Color.green },
            { Color.clear, Color.green, Color.clear },
        };
        var texture = ConvertToTexture(array);
        
        texture.filterMode = FilterMode.Point;
        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        
        Debug.Log("hello");
        Debug.Log("hello2");
        Debug.Log("hello2");
    }

    void Update()
    {
    }
    
    public static Texture2D ConvertToTexture(Color[,] array)
    {
        var width = array.GetLength(0);
        var height = array.GetLength(1);

        // Find bounds of non-transparent pixels
        int minX = width, maxX = 0, minY = height, maxY = 0;
        var hasVisiblePixels = false;

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (array[x, y].a > 0) // Non-transparent
                {
                    hasVisiblePixels = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (!hasVisiblePixels) return null; // No visible pixels

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
}
