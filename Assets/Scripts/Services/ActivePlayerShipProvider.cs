using Core.Services;
using Core.Ships;
using UnityEngine;

namespace Services
{
    public class ActivePlayerShipProvider : IActivePlayerShipProvider
    {
        public IShip ActiveShip { get; private set; }

        public void SetActiveShip(IShip ship)
        {
            if (ship == null)
                throw new UnityException("[ActivePlayerShipProvider] Active ship cannot be null.");

            ActiveShip = ship;
        }
    }
}