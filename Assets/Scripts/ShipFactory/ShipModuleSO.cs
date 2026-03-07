using UnityEngine;

namespace ShipFactory
{
    [CreateAssetMenu(fileName = "ShipModuleSO", menuName = "Ship Factory/ShipModuleSO")]
    public class ShipModuleSO : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector2Int dimensions;
        [SerializeField] private string partName;
        [SerializeField] private string description;

        public GameObject Prefab => prefab;
        public Vector2Int Dimensions => dimensions;
        public string Name => partName;
        public string Description => description;
    }
}