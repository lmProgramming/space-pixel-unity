using System.Collections.Generic;
using UnityEngine;

namespace Core.Services
{
    public interface ISectorService
    {
        SectorResult GetSectorResult(Vector3 position);
        List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize);
    }
}