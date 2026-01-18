using System;
using UnityEngine;

namespace Ships.Internal
{
    public class ResourceManager : MonoBehaviour
    {
        [SerializeField] private float energyEfficiency;
        [SerializeField] private int crew;
        [SerializeField] private int crewCapacity;
        [SerializeField] private float energy;
        [SerializeField] private float energyCapacity;
        [SerializeField] private float energyDraw;
        [SerializeField] private float energyProduction;
        public float EnergyEfficiency => energyEfficiency;

        public void UpdateEnergy()
        {
            var netEnergy = energyProduction - energyDraw;

            energy = Math.Clamp(energy + netEnergy, 0, energyCapacity);

            energyEfficiency = energy > 0 ? 1 : energyProduction / energyDraw;
        }
    }
}