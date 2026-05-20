using UnityEngine;

namespace ShipFactory
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
    }
}