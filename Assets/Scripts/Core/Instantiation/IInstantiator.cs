using UnityEngine;

namespace Core.Instantiation
{
    public interface IInstantiator
    {
        GameObject Instantiate(GameObject prefab, Transform parent, bool instantiateInWorldSpace);
        GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation);

        GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent);
    }
}