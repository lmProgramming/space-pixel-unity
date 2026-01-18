using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Grid
{
    public struct RegionMetadata
    {
        public int StartIndex;
        public int PixelCount;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct FindAllRegionsJob : IJob
    {
        [ReadOnly] public Vector2Int StartPoint;
        [ReadOnly] public int Width;
        [ReadOnly] public int Height;
        [ReadOnly] public NativeArray<Color32> Pixels;

        public NativeArray<bool> Visited;

        [WriteOnly] public NativeList<Vector2Int> AllRegionPixels;
        [WriteOnly] public NativeList<RegionMetadata> Regions;

        public void Execute()
        {
            var queue = new NativeList<Vector2Int>(Width * Height, Allocator.Temp);

            if (IsInBounds(StartPoint)) Visited[StartPoint.y * Width + StartPoint.x] = true;

            CheckNeighborAndFloodFill(new Vector2Int(StartPoint.x, StartPoint.y + 1), ref queue);
            CheckNeighborAndFloodFill(new Vector2Int(StartPoint.x, StartPoint.y - 1), ref queue);
            CheckNeighborAndFloodFill(new Vector2Int(StartPoint.x - 1, StartPoint.y), ref queue);
            CheckNeighborAndFloodFill(new Vector2Int(StartPoint.x + 1, StartPoint.y), ref queue);

            queue.Dispose();
        }

        private void CheckNeighborAndFloodFill(Vector2Int point, ref NativeList<Vector2Int> queue)
        {
            if (!IsInBounds(point)) return;

            var index = point.y * Width + point.x;
            if (Visited[index] || Pixels[index].a <= 0) return;

            queue.Clear();
            var regionStartIndex = AllRegionPixels.Length;

            Visited[index] = true;
            queue.Add(point);
            var head = 0;

            while (head < queue.Length)
            {
                var current = queue[head++];
                AllRegionPixels.Add(current);

                EnqueueIfValid(new Vector2Int(current.x, current.y + 1), ref queue);
                EnqueueIfValid(new Vector2Int(current.x, current.y - 1), ref queue);
                EnqueueIfValid(new Vector2Int(current.x - 1, current.y), ref queue);
                EnqueueIfValid(new Vector2Int(current.x + 1, current.y), ref queue);
            }

            Regions.Add(new RegionMetadata
            {
                StartIndex = regionStartIndex,
                PixelCount = AllRegionPixels.Length - regionStartIndex
            });
        }

        private void EnqueueIfValid(Vector2Int point, ref NativeList<Vector2Int> queue)
        {
            if (!IsInBounds(point)) return;

            var index = point.y * Width + point.x;
            if (Visited[index] || Pixels[index].a <= 0) return;

            Visited[index] = true;
            queue.Add(point);
        }

        private bool IsInBounds(Vector2Int point)
        {
            return point.x >= 0 && point.x < Width && point.y >= 0 && point.y < Height;
        }
    }
}