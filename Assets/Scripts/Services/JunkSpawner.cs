using Core;
using Core.Instantiation;
using Pixelation;
using UnityEngine;

namespace Services
{
    public class JunkSpawner : MonoBehaviour, IJunkSpawner
    {
        [SerializeField] private GameObject junkPrefab;

        [SerializeField] private Transform parent;

        [SerializeField] private Instantiator instantiator;

        public void SpawnJunk(Vector2 position, Quaternion rotation, Color32[,] colors, IPixelatedRigidbody parentBody)
        {
            var newJunk = instantiator.Instantiate(junkPrefab, position, rotation, parent);

            var pixelatedJunk = newJunk.GetComponent<PixelatedJunk>();

            pixelatedJunk.Setup(colors);
            pixelatedJunk.CopyVelocity(parentBody);
        }
    }
}