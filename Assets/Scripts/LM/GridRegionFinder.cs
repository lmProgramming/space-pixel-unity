using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LM.Grid;
using Pixelation;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace LM
{
    public static class GridRegionFinder
    {
        public static List<HashSet<Vector2Int>> FloodFindCohesiveRegions(PixelGrid grid)
        {
            var texture = grid.Texture;
            if (!texture) return new List<HashSet<Vector2Int>>();

            var pixelData = texture.GetPixels32();
            var dimensions = new Vector2Int(texture.width, texture.height);

            var analyzer = new FastGridAnalyzer(pixelData, dimensions);
            return analyzer.FindAllRegions();
        }

        public static List<HashSet<Vector2Int>> FloodFindCohesiveRegions(Vector2Int lostPixel, PixelGrid grid)
        {
            var texture = grid.Texture;
            if (!texture) return new List<HashSet<Vector2Int>>();

            var pixelData = texture.GetPixels32();
            var dimensions = new Vector2Int(texture.width, texture.height);

            var analyzer = new FastGridAnalyzer(pixelData, dimensions);
            return analyzer.FindRegionsFromNeighbors(lostPixel);
        }

        public static List<HashSet<Vector2Int>> FloodFindCohesiveRegionsWithJobs(PixelGrid grid)
        {
            var texture = grid.Texture;
            if (texture == null) return new List<HashSet<Vector2Int>>();

            var width = texture.width;
            var height = texture.height;
            var pixelCount = width * height;

            var pixelData = texture.GetPixels32();
            var nativePixels = new NativeArray<Color32>(pixelData, Allocator.TempJob);
            var nativeVisited = new NativeArray<bool>(pixelCount, Allocator.TempJob);

            var nativeRegionPixels = new NativeList<Vector2Int>(pixelCount, Allocator.TempJob);
            var nativeRegionMetadata = new NativeList<RegionMetadata>(16, Allocator.TempJob);

            var job = new FindAllRegionsJob
            {
                Width = width,
                Height = height,
                Pixels = nativePixels,
                Visited = nativeVisited,
                AllRegionPixels = nativeRegionPixels,
                Regions = nativeRegionMetadata
            };

            var handle = job.Schedule();

            handle.Complete();

            var finalRegions = new List<HashSet<Vector2Int>>();
            foreach (var metadata in nativeRegionMetadata)
            {
                var region = new HashSet<Vector2Int>(metadata.PixelCount);
                for (var j = 0; j < metadata.PixelCount; j++) region.Add(nativeRegionPixels[metadata.StartIndex + j]);
                finalRegions.Add(region);
            }

            nativePixels.Dispose();
            nativeVisited.Dispose();
            nativeRegionPixels.Dispose();
            nativeRegionMetadata.Dispose();

            return finalRegions;
        }

        private class FastGridAnalyzer
        {
            private readonly int _height;
            private readonly Color32[] _pixels;

            private readonly Vector2Int[] _queue;

            private readonly bool[] _visited;
            private readonly int _width;
            private int _queueHead;
            private int _queueTail;

            public FastGridAnalyzer(Color32[] pixels, Vector2Int dimensions)
            {
                _pixels = pixels;
                _width = dimensions.x;
                _height = dimensions.y;

                var pixelCount = _width * _height;
                _visited = new bool[pixelCount];
                _queue = new Vector2Int[pixelCount];
            }

            public List<HashSet<Vector2Int>> FindAllRegions()
            {
                var regions = new List<HashSet<Vector2Int>>();

                for (var y = 0; y < _height; y++)
                for (var x = 0; x < _width; x++)
                {
                    var index = y * _width + x;

                    if (_visited[index] || _pixels[index].a <= 0) continue;

                    var startPoint = new Vector2Int(x, y);

                    _visited[index] = true;

                    regions.Add(FloodFillFrom(startPoint));
                }

                return regions;
            }

            public List<HashSet<Vector2Int>> FindRegionsFromNeighbors(Vector2Int point)
            {
                var regions = new List<HashSet<Vector2Int>>();

                var initialIndex = point.y * _width + point.x;
                if (!IsOutOfBounds(point)) _visited[initialIndex] = true;

                CheckNeighbor(new Vector2Int(point.x, point.y + 1), regions);
                CheckNeighbor(new Vector2Int(point.x, point.y - 1), regions);
                CheckNeighbor(new Vector2Int(point.x - 1, point.y), regions);
                CheckNeighbor(new Vector2Int(point.x + 1, point.y), regions);

                return regions;
            }

            private void CheckNeighbor(Vector2Int neighbor, List<HashSet<Vector2Int>> regions)
            {
                if (IsOutOfBounds(neighbor)) return;

                var index = neighbor.y * _width + neighbor.x;

                if (_visited[index] || _pixels[index].a <= 0) return;

                _visited[index] = true;
                regions.Add(FloodFillFrom(neighbor));
            }

            private HashSet<Vector2Int> FloodFillFrom(Vector2Int startPoint)
            {
                var region = new HashSet<Vector2Int>();

                _queueHead = 0;
                _queueTail = 0;

                _queue[_queueTail++] = startPoint;

                while (_queueHead < _queueTail)
                {
                    var current = _queue[_queueHead++];
                    region.Add(current);

                    EnqueueIfValid(new Vector2Int(current.x, current.y + 1));
                    EnqueueIfValid(new Vector2Int(current.x, current.y - 1));
                    EnqueueIfValid(new Vector2Int(current.x - 1, current.y));
                    EnqueueIfValid(new Vector2Int(current.x + 1, current.y));
                }

                return region;
            }

            private void EnqueueIfValid(Vector2Int point)
            {
                if (IsOutOfBounds(point)) return;

                var index = point.y * _width + point.x;

                if (_visited[index] || _pixels[index].a <= 0) return;

                _visited[index] = true;
                _queue[_queueTail++] = point;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool IsOutOfBounds(Vector2Int point)
            {
                return point.x < 0 || point.x >= _width || point.y < 0 || point.y >= _height;
            }
        }
    }
}