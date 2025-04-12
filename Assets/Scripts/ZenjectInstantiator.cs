using EasyPool;
using UnityEngine;
using Zenject;

public class ZenjectInstantiator : Instantiator
{
    [Inject] private DiContainer _container;

    public override GameObject Instantiate(GameObject prefab, Transform parent, bool instantiateInWorldSpace)
    {
        return _container.InstantiatePrefab(prefab, parent);
    }
}