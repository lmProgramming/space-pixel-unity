using UnityEngine;

namespace EasyPool
{
    public abstract class Instantiator : MonoBehaviour
    {
        public abstract GameObject Instantiate(GameObject prefab, Transform parent, bool instantiateInWorldSpace);
    }
}