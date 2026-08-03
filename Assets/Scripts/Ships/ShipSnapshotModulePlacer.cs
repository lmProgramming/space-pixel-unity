using System.Collections.Generic;
using Core.Services;
using Core.Ships;
using Core.Ships.Module;
using Core.Ships.Snapshots.Module;
using UnityEngine;
using ZLinq;

namespace Ships
{
    internal static class ShipSnapshotModulePlacer
    {
        public static void CreateModulesFromSnapshot(
            IShip ship,
            Transform shipTransform,
            ShipSnapshot snapshot,
            IGameContentCatalog contentCatalog,
            IModuleRestoreFactory moduleRestoreFactory)
        {
            var created = new List<CreatedModule>(snapshot.modules.Count);

            foreach (var moduleSnapshot in snapshot.modules)
            {
                var moduleGo = moduleRestoreFactory.CreateModuleShell(moduleSnapshot, shipTransform);
                moduleGo.SetActive(false);

                var identity = moduleGo.GetComponent<GameObjectInstanceIdentity>();
                if (!identity)
                    identity = moduleGo.AddComponent<GameObjectInstanceIdentity>();
                identity.RestoreFromSnapshot(moduleSnapshot.instanceId, moduleSnapshot.origin,
                    moduleSnapshot.archetypeId);

                var module = moduleGo.GetComponent<IModule>();
                if (module == null)
                    throw new UnityException(
                        $"[Ship] Failed to add a Module component for '{moduleSnapshot.moduleName}' (moduleType: {moduleSnapshot.concreteModuleType}).");

                module.SetShip(ship);
                module.RestoreFromSnapshot(moduleSnapshot, contentCatalog);

                var matchingBlueprint = ship.Blueprint?.modules?.AsValueEnumerable()
                    .FirstOrDefault(b => b != null && b.blueprintId == moduleSnapshot.instanceId);
                if (matchingBlueprint != null)
                    module.SetBlueprint(matchingBlueprint);

                created.Add(new CreatedModule(moduleGo, module, moduleSnapshot));
            }

            var commandModule = created.AsValueEnumerable()
                .FirstOrDefault(entry => entry.Module.Type == ModuleType.Command);
            if (commandModule.Module == null)
                throw new UnityException("[Ship] Snapshot has no command module.");

            ShipLayoutSpace.ApplyLayoutTransform(ship, commandModule.GameObject.transform, Vector3.zero,
                Quaternion.identity);

            foreach (var entry in created)
            {
                if (entry.Module == commandModule.Module) continue;

                ShipLayoutSpace.ApplyLayoutTransform(ship, entry.GameObject.transform, entry.Snapshot.localPosition,
                    entry.Snapshot.localRotation);
            }

            foreach (var entry in created)
            {
                entry.GameObject.SetActive(true);
                if (ship is Component shipComponent)
                    entry.GameObject.layer = shipComponent.gameObject.layer;
            }
        }

        private readonly struct CreatedModule
        {
            public CreatedModule(GameObject gameObject, IModule module, ModuleSnapshot snapshot)
            {
                GameObject = gameObject;
                Module = module;
                Snapshot = snapshot;
            }

            public GameObject GameObject { get; }
            public IModule Module { get; }
            public ModuleSnapshot Snapshot { get; }
        }
    }
}