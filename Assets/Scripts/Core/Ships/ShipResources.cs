using System;
using UnityEngine;

namespace Core.Ships
{
    [Serializable]
    public struct ShipResources
    {
        public float energyCapacity;
        public float energyDraw;
        public float energyProduction;

        [Range(0, 30)]
        public int crewNeeded;

        public int crewQuarters;

        public ShipResources(float energyCapacity, float energyDraw, int crewNeeded, float energyProduction,
            int crewQuarters)
        {
            this.energyCapacity = energyCapacity;
            this.energyDraw = energyDraw;
            this.energyProduction = energyProduction;

            this.crewNeeded = crewNeeded;
            this.crewQuarters = crewQuarters;
        }
    }
}