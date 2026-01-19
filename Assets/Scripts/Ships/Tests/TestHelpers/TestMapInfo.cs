using Core.Services;
using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public class TestMapInfo : IMapInfo
    {
        public TestMapInfo(Transform mapTransform)
        {
            MapTransform = mapTransform;
        }

        public Transform MapTransform { get; }
    }
}