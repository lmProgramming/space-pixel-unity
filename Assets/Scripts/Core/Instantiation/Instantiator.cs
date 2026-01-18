using UnityEngine;

namespace Core.Instantiation
{
    public abstract class Instantiator : MonoBehaviour
    {
        public abstract GameObject Instantiate(GameObject prefab, Transform parent, bool instantiateInWorldSpace);
        public abstract GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation);

        public abstract GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent);
    }
}