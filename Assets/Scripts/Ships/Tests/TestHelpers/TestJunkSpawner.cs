using System.Collections.Generic;
using Core.Pixelation;
using Core.Services;
using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public class TestJunkSpawner : IJunkSpawner
    {
        private List<JunkSpawnRecord> SpawnedJunk { get; } = new();

        public void SpawnJunk(Vector2 position, Quaternion rotation, Color32[,] colors, IPixelatedRigidbody parentBody)
        {
            SpawnedJunk.Add(new JunkSpawnRecord(position, rotation, colors, parentBody));
        }

        public void Clear()
        {
            SpawnedJunk.Clear();
        }

        public List<JunkSpawnRecord> GetJunk()
        {
            return SpawnedJunk;
        }

        public readonly struct JunkSpawnRecord
        {
            public readonly Vector2 Position;
            public readonly Quaternion Rotation;
            public readonly Color32[,] Colors;
            public readonly IPixelatedRigidbody ParentBody;

            public JunkSpawnRecord(Vector2 position, Quaternion rotation, Color32[,] colors,
                IPixelatedRigidbody parentBody)
            {
                Position = position;
                Rotation = rotation;
                Colors = colors;
                ParentBody = parentBody;
            }
        }
    }
}