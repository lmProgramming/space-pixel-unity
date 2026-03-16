using UnityEngine;

namespace ShipFactory
{
    public class ShipModuleSOContainer : MonoBehaviour
    {
        [field: SerializeField] public ShipModuleSO Module { get; private set; }
    }
}