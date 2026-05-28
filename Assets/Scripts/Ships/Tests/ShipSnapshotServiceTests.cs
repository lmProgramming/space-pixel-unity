using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.Services;
using Core.Ship;
using NUnit.Framework;
using Pixelation;
using Services;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using Resources = Core.Ship.Resources;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipSnapshotServiceTests : ShipTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _contentCatalog = new TestContentCatalog();
            _moduleCatalog = new TestModuleCatalog();
            _service = new ShipSnapshotService(Container, null, _moduleCatalog, _contentCatalog);
        }

        private sealed class TestContentCatalog : IGameContentCatalog
        {
            private readonly Dictionary<string, GameObject> _idToPrefab = new();
            private readonly Dictionary<string, Sprite> _idToSprite = new();
            private readonly Dictionary<GameObject, string> _prefabToId = new();
            private readonly Dictionary<Sprite, string> _spriteToId = new();

            public bool TryGetPrefab(string contentId, out GameObject prefab)
            {
                return _idToPrefab.TryGetValue(contentId, out prefab);
            }

            public bool TryGetContentId(GameObject prefab, out string contentId)
            {
                return _prefabToId.TryGetValue(prefab, out contentId);
            }

            public bool TryGetSprite(string contentId, out Sprite sprite)
            {
                return _idToSprite.TryGetValue(contentId, out sprite);
            }

            public bool TryGetSpriteContentId(Sprite sprite, out string contentId)
            {
                return _spriteToId.TryGetValue(sprite, out contentId);
            }

            public void AddPrefab(string id, GameObject prefab)
            {
                _idToPrefab[id] = prefab;
                _prefabToId[prefab] = id;
            }

            public void AddSprite(string id, Sprite sprite)
            {
                _idToSprite[id] = sprite;
                _spriteToId[sprite] = id;
            }
        }

        private sealed class TestModuleCatalog : IShipModuleCatalog
        {
            private readonly Dictionary<string, GameObject> _idToPrefab = new();

            public bool TryGetModulePrefab(string archetypeId, out GameObject prefab)
            {
                return _idToPrefab.TryGetValue(archetypeId, out prefab);
            }

            public void Add(string id, GameObject prefab)
            {
                _idToPrefab[id] = prefab;
            }
        }

        private TestContentCatalog _contentCatalog;
        private TestModuleCatalog _moduleCatalog;
        private ShipSnapshotService _service;

        [UnityTest]
        public IEnumerator PrefabOriginCannon_RoundTrip_PreservesProjectileAndSprite()
        {
            var projectilePrefab = new GameObject("ProjectilePrefab");
            var weaponSprite = CreateTestSprite();
            projectilePrefab.transform.SetParent(TestRoot.transform);
            _contentCatalog.AddPrefab("bullet_big", projectilePrefab);
            _contentCatalog.AddSprite("sprite_cannon", weaponSprite);

            var cannonPrefab = CreateCannonPrefab(projectilePrefab, weaponSprite);
            _moduleCatalog.Add("cannon_small_16", cannonPrefab);

            var ship = CreateShipWithCommandAndCannon(cannonPrefab, "cannon_small_16");
            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var json = _service.ToJson(snapshot);
            var fromJson = ShipSnapshotService.FromJson(json);
            _service.ApplySnapshot(ship, fromJson);
            ship.InitializeModules();

            var restoredCannon = ((Component)ship.AllModules[1]).GetComponent<Cannon>();
            Assert.IsNotNull(restoredCannon.GetSprite(), "Cannon sprite should be restored from content catalog.");
        }

        [UnityTest]
        public IEnumerator CustomScratchModule_RoundTrip_PreservesColorGrid()
        {
            var ship = CreateShipWithScratchEngine();
            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var moduleSnapshot = snapshot.modules[1];
            moduleSnapshot.origin = ModuleOrigin.Custom;
            moduleSnapshot.archetypeId = string.Empty;
            moduleSnapshot.colorGrid.RemovePixel(0, 0);

            _service.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();

            var restoredEngine = (Engine)ship.AllModules[1];
            var rb = restoredEngine.PixelatedRigidbody;
            Assert.IsFalse(rb.IsPixel(new Vector2Int(0, 0)));
            Assert.IsTrue(rb.IsPixel(new Vector2Int(1, 1)));
        }

        [UnityTest]
        public IEnumerator PostDamage_RoundTrip_KeepsDestroyedPixelsGone()
        {
            var ship = CreateShipWithScratchEngine();
            yield return null;

            var engine = (Engine)ship.AllModules[1];
            engine.PixelatedRigidbody.RemovePixelAt(new Vector2Int(2, 2));
            engine.PixelatedRigidbody.RemovePixelAt(new Vector2Int(3, 3));

            var snapshot = _service.CaptureSnapshot(ship);
            _service.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();

            var restored = (Engine)ship.AllModules[1];
            Assert.IsFalse(restored.PixelatedRigidbody.IsPixel(new Vector2Int(2, 2)));
            Assert.IsFalse(restored.PixelatedRigidbody.IsPixel(new Vector2Int(3, 3)));
        }

        private Ship CreateShipWithCommandAndCannon(GameObject cannonPrefab, string archetypeId)
        {
            var shipGo = ModuleFactory.CreateGameObject("Ship", CreatedObjects);
            shipGo.transform.SetParent(TestRoot.transform);
            shipGo.SetActive(false);

            CreateCommandModule(shipGo.transform);

            var cannonInstance = Object.Instantiate(cannonPrefab, shipGo.transform);
            cannonInstance.transform.localPosition = new Vector3(2f, 0f, 0f);
            cannonInstance.SetActive(true);
            var cannonPixelRb = cannonInstance.GetComponent<PixelatedRigidbody>();
            cannonPixelRb.SetTextureFromColors(ModuleFactory.CreateSolidPixelGrid(5, 5));
            var cannonIdentity = cannonInstance.GetComponent<ModuleInstanceIdentity>();
            if (cannonIdentity == null)
                cannonIdentity = cannonInstance.AddComponent<ModuleInstanceIdentity>();
            cannonIdentity.EnsureAssigned(ModuleOrigin.CatalogPrefab, archetypeId);

            var ship = ModuleFactory.WireShip<Ship>(shipGo, Container);
            ship.InitializeModules();
            return ship;
        }

        private Ship CreateShipWithScratchEngine()
        {
            var shipGo = ModuleFactory.CreateGameObject("Ship", CreatedObjects);
            shipGo.transform.SetParent(TestRoot.transform);
            shipGo.SetActive(false);

            CreateCommandModule(shipGo.transform);

            var engineGo = ModuleFactory.CreateModuleBase("Engine", shipGo.transform, new Vector2(2f, 0f), 0f,
                Container, CreatedObjects, 5, 5);
            var exhaustRoot = ModuleFactory.CreateGameObject("Exhaust", CreatedObjects);
            exhaustRoot.transform.SetParent(engineGo.transform, false);
            exhaustRoot.AddComponent<ParticleSystem>();
            var engine = engineGo.AddComponent<Engine>();
            engine.SetResources(new Resources(0, 2f, 0, 0, 0));
            var identity = engineGo.AddComponent<ModuleInstanceIdentity>();
            identity.EnsureAssigned(ModuleOrigin.Custom);

            var ship = ModuleFactory.WireShip<Ship>(shipGo, Container);
            ship.InitializeModules();
            return ship;
        }

        private void CreateCommandModule(Transform parent)
        {
            var commandGo = ModuleFactory.CreateModuleBase("Command", parent, Vector2.zero, 0f, Container,
                CreatedObjects, 5, 5);
            var command = commandGo.AddComponent<Command>();
            command.SetResources(new Resources(0, 0, 0, 0, 0));
            var identity = commandGo.AddComponent<ModuleInstanceIdentity>();
            identity.EnsureAssigned(ModuleOrigin.Custom);
        }

        private GameObject CreateCannonPrefab(GameObject projectilePrefab, Sprite weaponSprite)
        {
            var go = ModuleFactory.CreateModuleBase("CannonPrefab", TestRoot.transform, Vector2.zero, 0f,
                Container, CreatedObjects, 5, 5);
            go.SetActive(false);
            var cannon = go.AddComponent<Cannon>();
            cannon.SetResources(new Resources(0, 1, 0, 0, 0));
            SetPrivateField(cannon, "projectilePrefab", projectilePrefab);
            SetPrivateField(cannon, "sprite", weaponSprite);
            SetPrivateField(cannon, "reloadTime", 1.5f);
            SetPrivateField(cannon, "projectileSpeed", 20f);
            return go;
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(2, 2);
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new UnityException($"Missing field '{fieldName}' on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }
    }
}