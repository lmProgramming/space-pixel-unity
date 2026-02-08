using UnityEngine;
using Zenject;

namespace Instantiation
{
    public class ZenjectInstantiator : Instantiator
    {
        [Inject] private DiContainer _container;

        public override GameObject Instantiate(GameObject prefab, Transform parent, bool instantiateInWorldSpace)
        {
            return _container.InstantiatePrefab(prefab, parent);
        }

        public override GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Instantiate(prefab, position, rotation, null);
        }

        public override GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent)
        {
            return _container.InstantiatePrefab(prefab, position, rotation, parent);
        }
    }
}