using UnityEngine;

namespace Core.Pixelation
{
    public interface IPixelatedSprite
    {
        public Sprite GetSprite();
        public void SetSprite(Sprite sprite);
    }
}