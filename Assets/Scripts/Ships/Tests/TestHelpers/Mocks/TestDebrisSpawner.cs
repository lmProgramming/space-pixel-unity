using System.Collections.Generic;
using Core.Pixelation;
using Core.Services;
using UnityEngine;

namespace Ships.Tests.TestHelpers.Mocks
{
    public class TestDebrisSpawner : IDebrisSpawner
    {
        private List<DebrisSpawnRecord> SpawnedDebris { get; } = new();

        public void SpawnDebris(Vector2 position, Quaternion rotation, Color32[,] colors,
            IPixelatedRigidbody parentBody)
        {
            SpawnedDebris.Add(new DebrisSpawnRecord(position, rotation, colors, parentBody));
        }

        public void Clear()
        {
            SpawnedDebris.Clear();
        }

        public List<DebrisSpawnRecord> GetDebris()
        {
            return SpawnedDebris;
        }

        public readonly struct DebrisSpawnRecord
        {
            public readonly Vector2 Position;
            public readonly Quaternion Rotation;
            public readonly Color32[,] Colors;
            public readonly IPixelatedRigidbody ParentBody;

            public DebrisSpawnRecord(Vector2 position, Quaternion rotation, Color32[,] colors,
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