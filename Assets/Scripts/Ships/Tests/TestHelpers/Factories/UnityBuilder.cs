using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers.Factories
{
    public class UnityBuilder
    {
        public static GameObject CreateGameObject(string name, ICollection<GameObject> createdObjects,
            DiContainer container)
        {
            var go = new GameObject(name);
            createdObjects.Add(go);
            container.Inject(go);
            return go;
        }
    }
}