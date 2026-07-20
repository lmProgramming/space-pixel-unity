using Core.Services;
using Core.Ships;
using UnityEngine;

namespace Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ActivePlayerShipProvider : IActivePlayerShipProvider
    {
        public bool HasPlayerShip { get; private set; }

        public IShip ActiveShip { get; private set; }

        public void SetActiveShip(IShip ship)
        {
            ActiveShip = ship ?? throw new UnityException("[ActivePlayerShipProvider] Active ship cannot be null.");
            HasPlayerShip = true;
        }
    }
}