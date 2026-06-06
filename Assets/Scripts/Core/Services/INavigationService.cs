using System.Collections.Generic;
using Core.Pixelation;
using Core.Ship;
using UnityEngine;

namespace Core.Services
{
    public interface INavigationService
    {
        float SectorSize { get; }
        SectorResult GetSectorResult(Vector3 position);
        List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize);

        List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize, IShip callerShip,
            IPixelatedRigidbody targetShip);
    }
}