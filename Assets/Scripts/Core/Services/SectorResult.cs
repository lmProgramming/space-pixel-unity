using System.Collections.Generic;
using Core.Ships;

namespace Core.Services
{
    public class SectorResult
    {
        public static readonly SectorResult Empty = new(false, false, float.MaxValue);

        public SectorResult(bool hasObstacles, bool hasDebris, float generationTime,
            IReadOnlyCollection<IShip> shipsInSector = null)
        {
            HasObstacles = hasObstacles;
            HasDebris = hasDebris;
            GenerationTime = generationTime;
            ShipsInSector = shipsInSector ?? new List<IShip>();
        }

        public float GenerationTime { get; }
        public bool HasObstacles { get; }
        public bool HasDebris { get; }
        public bool IsEmpty => !HasObstacles && !HasDebris && ShipsInSector.Count == 0;
        public IReadOnlyCollection<IShip> ShipsInSector { get; }
    }
}