using System.Collections.Generic;
using Pixelation;
using UnityEngine;

namespace LM
{
    public static class GridRegionFinder
    {
        public static List<HashSet<Vector2Int>> FloodFindCohesiveRegions(IPixelated grid)
        {
            var visited = new HashSet<Vector2Int>();
            var regions = new List<HashSet<Vector2Int>>();

            var dimensions = grid.Dimensions();
            var gridSize = dimensions.x * dimensions.y;

            while (visited.Count < gridSize)
            {
                Vector2Int? searchStartPoint = null;

                for (var x = 0; x < dimensions.x; x++)
                for (var y = 0; y < dimensions.y; y++)
                {
                    var point = new Vector2Int(x, y);
                    if (!visited.Contains(point) && grid.IsPixelAssumeInBounds(point))
                        searchStartPoint = point;
                }

                if (searchStartPoint == null) return regions;

                SetupFlooding(searchStartPoint.Value);
            }

            return regions;

            void SetupFlooding(Vector2Int searchStart)
            {
                if (!grid.InBounds(searchStart)) return;

                regions.Add(new HashSet<Vector2Int> { searchStart });

                FloodFind(searchStart, regions.Count - 1);
            }

            void FloodFind(Vector2Int position, int regionIndex)
            {
                while (true)
                {
                    if (regionIndex == regions.Count) return;

                    if (!grid.IsPixel(position)) return;

                    if (!visited.Add(position))
                    {
                        FindRegionToMerge(position, regionIndex);
                        return;
                    }

                    regions[regionIndex].Add(position);

                    FloodFind(position + new Vector2Int(1, 0), regionIndex);
                    FloodFind(position + new Vector2Int(-1, 0), regionIndex);
                    FloodFind(position + new Vector2Int(0, 1), regionIndex);
                    // recursion changed to tail
                    position += new Vector2Int(0, -1);
                }
            }

            void FindRegionToMerge(Vector2Int position, int regionIndex)
            {
                for (var index = 0; index < regions.Count; index++)
                {
                    var region = regions[index];

                    if (!region.Contains(position)) continue;

                    if (index == regionIndex) break;

                    MergeRegions(index, regionIndex);
                }
            }

            void MergeRegions(int indexToMergeWith, int indexMerged)
            {
                regions[indexToMergeWith].UnionWith(regions[indexMerged]);

                regions.RemoveAt(indexMerged);
            }
        }

        public static List<HashSet<Vector2Int>> FloodFindCohesiveRegions(Vector2Int searchStartPoint,
            IPixelated grid)
        {
            var visited = new HashSet<Vector2Int>
            {
                searchStartPoint
            };

            var regions = new List<HashSet<Vector2Int>>();

            SetupFlooding(searchStartPoint + new Vector2Int(1, 0));
            SetupFlooding(searchStartPoint + new Vector2Int(-1, 0));
            SetupFlooding(searchStartPoint + new Vector2Int(0, 1));
            SetupFlooding(searchStartPoint + new Vector2Int(0, -1));

            return regions;

            void SetupFlooding(Vector2Int searchStart)
            {
                if (!grid.InBounds(searchStart)) return;

                regions.Add(new HashSet<Vector2Int> { searchStart });

                FloodFind(searchStart, regions.Count - 1);
            }

            void FloodFind(Vector2Int position, int regionIndex)
            {
                while (true)
                {
                    if (regionIndex == regions.Count) return;

                    if (!grid.IsPixel(position)) return;

                    if (!visited.Add(position))
                    {
                        FindRegionToMerge(position, regionIndex);
                        return;
                    }

                    regions[regionIndex].Add(position);

                    FloodFind(position + new Vector2Int(1, 0), regionIndex);
                    FloodFind(position + new Vector2Int(-1, 0), regionIndex);
                    FloodFind(position + new Vector2Int(0, 1), regionIndex);
                    // recursion changed to tail
                    position += new Vector2Int(0, -1);
                }
            }

            void FindRegionToMerge(Vector2Int position, int regionIndex)
            {
                for (var index = 0; index < regions.Count; index++)
                {
                    var region = regions[index];

                    if (!region.Contains(position)) continue;

                    if (index == regionIndex) break;

                    MergeRegions(index, regionIndex);
                }
            }

            void MergeRegions(int indexToMergeWith, int indexMerged)
            {
                regions[indexToMergeWith].UnionWith(regions[indexMerged]);

                regions.RemoveAt(indexMerged);
            }
        }
    }
}