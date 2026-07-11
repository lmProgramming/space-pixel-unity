using Core.Ships;
using UnityEngine;

namespace ShipFactory
{
    public class ShipModuleSOContainer : MonoBehaviour, IHasModuleArchetypeId
    {
        [field: SerializeField] public ShipModuleSO Module { get; private set; }

        public string ModuleArchetypeId => Module != null ? Module.ArchetypeId : string.Empty;
    }
}
