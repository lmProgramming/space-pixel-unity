using System.Collections.Generic;
using Core.Ship;
using UnityEngine;

namespace Core.Services
{
    public interface INavigationService
    {
        SectorResult GetSectorResult(Vector3 position);
        List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize);
        List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize, IShip callerShip, IShip targetShip);
    }
}