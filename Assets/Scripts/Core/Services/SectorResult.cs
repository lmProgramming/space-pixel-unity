namespace Core.Services
{
    public class SectorResult
    {
        public SectorResult(bool empty, float generationTime)
        {
            Empty = empty;
            GenerationTime = generationTime;
        }

        public float GenerationTime { get; }
        public bool Empty { get; private set; }
    }
}