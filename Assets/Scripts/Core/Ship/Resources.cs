using System;
using UnityEngine;

namespace Core.Ship
{
    [Serializable]
    public struct Resources
    {
        public float energyCapacity;
        public float energyDraw;
        public float energyProduction;

        [Range(0, 30)]
        public int crewNeeded;

        public int crewQuarters;

        public Resources(float energyCapacity, float energyDraw, int crewNeeded, float energyProduction,
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