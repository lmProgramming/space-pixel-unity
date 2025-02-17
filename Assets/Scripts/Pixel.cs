using UnityEngine;

public class Pixel
{
    public Color Color { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }

    public Pixel(Color color, float maxHealth)
    {
        Color = color;
        MaxHealth = maxHealth;
    }

    public void RepairToMaxHealth()
    {
        Health = MaxHealth;
    }
}
