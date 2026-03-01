using System;
using System.Collections.Generic;
using Ships.Modules;
using UnityEngine;

namespace Ships.Internal
{
    public class ResourceManager : MonoBehaviour
    {
        [SerializeField] private float energyEfficiency;
        [SerializeField] private float energy;
        [SerializeField] private float energyCapacity;
        [SerializeField] private float energyDraw;
        [SerializeField] private float energyProduction;

        [SerializeField] private int crewCapacity;
        [SerializeField] private int crew;

        public float EnergyEfficiency => energyEfficiency;
        public float Energy => energy;
        public float EnergyCapacity => energyCapacity;
        public float EnergyDraw => energyDraw;
        public float EnergyProduction => energyProduction;
        public int Crew => crew;
        public int CrewCapacity => crewCapacity;

        public void Recalculate(IReadOnlyList<Module> modules)
        {
            energyCapacity = 0;
            energyDraw = 0;
            energyProduction = 0;

            crewCapacity = 0;
            crew = 0;

            foreach (var module in modules)
            {
                energyCapacity += module.EnergyCapacity;
                energyDraw += module.GetEnergyDraw();
                energyProduction += module.GetEnergyProduction();

                crewCapacity += module.CrewNeededCount;
                crew += module.AliveCrewCount();
            }
        }

        public void UpdateEnergy()
        {
            var netEnergy = (energyProduction - energyDraw) * Time.deltaTime;

            energy = Math.Clamp(energy + netEnergy, 0, energyCapacity);

            if (energyProduction == 0) energyEfficiency = 0;
            else if (energyDraw == 0) energyEfficiency = 1;
            else energyEfficiency = energy > 0 ? 1 : energyProduction / energyDraw;
        }
    }
}