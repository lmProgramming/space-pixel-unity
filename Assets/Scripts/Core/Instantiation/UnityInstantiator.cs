using UnityEngine;

namespace Core.Instantiation
{
    public class UnityInstantiator : Instantiator
    {
        public override GameObject Instantiate(GameObject prefab, Transform parent, bool instantiateInWorldSpace)
        {
            return GameObject.Instantiate(prefab, parent, instantiateInWorldSpace);
        }

        public override GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GameObject.Instantiate(prefab, position, rotation);
        }

        public override GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent)
        {
            return GameObject.Instantiate(prefab, position, rotation, parent);
        }
    }
}