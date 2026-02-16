using UnityEngine;

namespace Core.Services
{
    public interface ISectorService
    {
        SectorResult GetSectorResult(Vector3 position);
    }
}