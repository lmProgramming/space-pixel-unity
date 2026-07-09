using System.Collections.Generic;
using Core.Services;
using Services;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers.Factories
{
    public static class SnapshotRestoreServicesFactory
    {
        public static void Bind(DiContainer container, ICollection<GameObject> createdObjects)
        {
            Install(container, createdObjects, false);
        }

        public static void Rebind(DiContainer container, ICollection<GameObject> createdObjects)
        {
            Install(container, createdObjects, true);
        }

        private static void Install(DiContainer container, ICollection<GameObject> createdObjects, bool rebind)
        {
            var shellPrefab = CreatePixelatedObjectShellPrefab(createdObjects);

            var servicesGo = new GameObject("SnapshotRestoreServices");
            createdObjects.Add(servicesGo);

            var pixelatedRigidbodyFactory = servicesGo.AddComponent<PixelatedRigidbodyFactory>();
            pixelatedRigidbodyFactory.ConfigureForTesting(shellPrefab);

            var moduleRestoreFactory = servicesGo.AddComponent<ModuleRestoreFactory>();

            if (rebind)
            {
                container.Rebind<IPixelatedRigidbodyFactory>()
                    .FromInstance(pixelatedRigidbodyFactory)
                    .AsCached();
                container.Rebind<IModuleRestoreFactory>()
                    .FromInstance(moduleRestoreFactory)
                    .AsCached();
            }
            else
            {
                container.Bind<IPixelatedRigidbodyFactory>()
                    .FromInstance(pixelatedRigidbodyFactory)
                    .AsSingle();
                container.Bind<IModuleRestoreFactory>()
                    .FromInstance(moduleRestoreFactory)
                    .AsSingle();
            }

            container.Inject(pixelatedRigidbodyFactory);
            container.Inject(moduleRestoreFactory);
        }

        private static GameObject CreatePixelatedObjectShellPrefab(ICollection<GameObject> createdObjects)
        {
            var shellGo = new GameObject("PixelatedObjectShell");
            createdObjects.Add(shellGo);

            shellGo.AddComponent<SpriteRenderer>();

            var rigidbody = shellGo.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;

            shellGo.AddComponent<PolygonCollider2D>();

            return shellGo;
        }
    }
}