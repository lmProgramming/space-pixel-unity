using System;

namespace Core.Ship
{
    [Serializable]
    public struct Resources
    {
        public float energyCapacity;
        public float energyDraw;
        public float energyProduction;

        public int crew;
        public int crewCapacity;

        public Resources(float energyCapacity, float energyDraw, int crew, float energyProduction, int crewCapacity)
        {
            this.energyCapacity = energyCapacity;
            this.energyDraw = energyDraw;
            this.energyProduction = energyProduction;

            this.crew = crew;
            this.crewCapacity = crewCapacity;
        }
    }
}