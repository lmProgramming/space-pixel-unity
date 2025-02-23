using UnityEngine;

namespace Pixelation
{
    public class Pixel
    {
        public Pixel(Color color, float maxHealth)
        {
            Color = color;
            MaxHealth = maxHealth;
        }

        public Color Color { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; }

        public void RepairToMaxHealth()
        {
            Health = MaxHealth;
        }
    }
}