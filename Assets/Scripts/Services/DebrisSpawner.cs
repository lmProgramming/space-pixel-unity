using Core.Pixelation;
using Core.Services;
using Instantiation;
using Pixelation;
using UnityEngine;

namespace Services
{
    public class DebrisSpawner : MonoBehaviour, IDebrisSpawner
    {
        [SerializeField]
        private GameObject debrisPrefab;

        [SerializeField] private Transform parent;

        [SerializeField] private Instantiator instantiator;

        public void SpawnDebris(Vector2 position, Quaternion rotation, Color32[,] colors,
            IPixelatedRigidbody parentBody)
        {
            var newDebris = instantiator.Instantiate(debrisPrefab, position, rotation, parent);

            var pixelatedDebris = newDebris.GetComponent<PixelatedDebris>();

            pixelatedDebris.Setup(colors);
            pixelatedDebris.CopyVelocity(parentBody);
        }
    }
}