using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ShipFactory.Tests")]

namespace Core.ShipFactory
{
    [CreateAssetMenu(fileName = "ShipModuleSO", menuName = "Ship Factory/ShipModuleSO")]
    public class ShipModuleSO : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private string archetypeId;
        [SerializeField] private Vector2Int dimensions;
        [SerializeField] private string partName;
        [SerializeField] private string description;

        public GameObject Prefab => prefab;
        public string ArchetypeId => archetypeId;
        public Vector2Int Dimensions => dimensions;
        public string Name => partName;
        public string Description => description;

#if UNITY_INCLUDE_TESTS
        internal void ConfigureForTesting(string newPartName, string moduleDescription, Vector2Int moduleDimensions,
            GameObject modulePrefab)
        {
            partName = newPartName;
            description = moduleDescription;
            dimensions = moduleDimensions;
            prefab = modulePrefab;
        }
#endif
    }
}