using System;
using UnityEngine;

namespace Pixelation
{
    [Serializable]
    public class Pixel
    {
        public Pixel(Color color, float maxHealth)
        {
            Color = color;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        [field: SerializeField] public Color Color { get; private set; }
        [field: SerializeField] public float Health { get; private set; }
        [field: SerializeField] public float MaxHealth { get; private set; }

        public void RepairToMaxHealth()
        {
            Health = MaxHealth;
        }
    }
}