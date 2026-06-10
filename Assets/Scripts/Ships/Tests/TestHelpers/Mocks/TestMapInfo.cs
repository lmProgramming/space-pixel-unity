using Core.Services;
using UnityEngine;

namespace Ships.Tests.TestHelpers.Mocks
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