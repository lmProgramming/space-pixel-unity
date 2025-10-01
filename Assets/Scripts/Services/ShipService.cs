using System.Collections.Generic;
using Ships;
using UnityEngine;

namespace Services
{
    public class ShipService : MonoBehaviour
    {
        [field: SerializeField]
        public List<Ship> Ships { get; private set; } = new();

        private void Start()
        {
            Ships = new List<Ship>(FindObjectsByType<Ship>(FindObjectsSortMode.None));
        }
    }
}