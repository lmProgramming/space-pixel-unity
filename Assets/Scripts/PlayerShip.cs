using UnityEngine;

public class PlayerShip : Ship
{
    [SerializeField] private float speedMultiplier;

    [SerializeField] private float rotationMultiplier;

    protected override void Move()
    {
        var acceleration = Input.GetAxis("Vertical") * speedMultiplier;

        rb.AddForce(transform.up * acceleration);

        var turn = Input.GetAxis("Horizontal") * rotationMultiplier;

        rb.AddTorque(turn);
    }

    protected override void HandleWeapons()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        foreach (var weapon in Weapons) weapon.Shoot();
    }
}