using System.Runtime.CompilerServices;
using Core.Pixelation;
using Core.Services;
using UnityEngine;
using Zenject;

[assembly: InternalsVisibleTo("Ships.Tests")]
[assembly: InternalsVisibleTo("E2E")]

namespace Services
{
    public class PixelatedRigidbodyFactory : MonoBehaviour, IPixelatedRigidbodyFactory
    {
        [SerializeField] private GameObject pixelatedObjectShellPrefab;

        private DiContainer _container;
        private IInstantiator _instantiator;

        public IPixelatedRigidbodyShellBuilder CreatePixelatedRigidbodyShell(
            Transform parent,
            string newName,
            Vector3 localPosition,
            Quaternion localRotation,
            RigidbodyType2D bodyType)
        {
            if (!pixelatedObjectShellPrefab)
                throw new UnityException(
                    "[PixelatedRigidbodyFactory] Assign pixelatedObjectShellPrefab on the component.");

            var instance = _instantiator.InstantiatePrefab(pixelatedObjectShellPrefab, parent);
            instance.name = newName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;

            var rigidbody = instance.GetComponent<Rigidbody2D>();
            if (!rigidbody)
                throw new UnityException(
                    $"[PixelatedRigidbodyFactory] Shell prefab '{pixelatedObjectShellPrefab.name}' is missing Rigidbody2D.");

            rigidbody.bodyType = bodyType;
            rigidbody.gravityScale = 0f;

            return new PixelatedRigidbodyShellBuilder(instance, _container);
        }

        [Inject]
        private void Construct(IInstantiator instantiator, DiContainer container)
        {
            _instantiator = instantiator;
            _container = container;
        }

#if UNITY_INCLUDE_TESTS
        internal void ConfigureForTesting(GameObject shellPrefab)
        {
            pixelatedObjectShellPrefab = shellPrefab;
        }
#endif
    }

    public sealed class PixelatedRigidbodyShellBuilder : IPixelatedRigidbodyShellBuilder
    {
        private readonly DiContainer _container;

        public PixelatedRigidbodyShellBuilder(GameObject gameObject, DiContainer container)
        {
            GameObject = gameObject;
            _container = container;
        }

        public GameObject GameObject { get; }

        public IPixelatedRigidbody PixelatedRigidbody { get; private set; }

        public IPixelatedRigidbodyShellBuilder AsDisabledGameObject()
        {
            GameObject.SetActive(false);
            return this;
        }

        public IPixelatedRigidbodyShellBuilder WithPixelatedRigidbody<T>() where T : Component, IPixelatedRigidbody
        {
            PixelatedRigidbody = _container.InstantiateComponent<T>(GameObject);
            return this;
        }
    }
}