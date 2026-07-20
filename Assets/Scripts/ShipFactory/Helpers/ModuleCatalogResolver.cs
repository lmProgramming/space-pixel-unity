using System;
using Core.Services;
using Core.ShipFactory;
using Core.Ships;
using ShipFactory.Models;
using UnityEngine;

namespace ShipFactory.Helpers
{
    public static class ModuleCatalogResolver
    {
        public static ShipModuleSO ResolveModuleSO(GameObject moduleInstance, IShipModuleCatalog catalog)
        {
            if (!moduleInstance)
                throw new ArgumentNullException(nameof(moduleInstance));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            var archetypeId = GetRequiredArchetypeId(moduleInstance);

            if (!catalog.TryGetModuleSO(archetypeId, out var moduleSO) || !moduleSO)
                throw new InvalidOperationException(
                    $"[ShipFactory] Module '{moduleInstance.name}' archetype '{archetypeId}' was not found in {nameof(IShipModuleCatalog)}.");

            return moduleSO;
        }

        private static string GetRequiredArchetypeId(GameObject moduleInstance)
        {
            var identity = moduleInstance.GetComponent<GameObjectInstanceIdentity>();
            if (identity && !string.IsNullOrWhiteSpace(identity.ArchetypeId))
                return identity.ArchetypeId;

            var archetypeSource = moduleInstance.GetComponent<IHasModuleArchetypeId>();
            if (archetypeSource != null && !string.IsNullOrWhiteSpace(archetypeSource.ModuleArchetypeId))
                return archetypeSource.ModuleArchetypeId;

            throw new InvalidOperationException(
                $"[ShipFactory] Module '{moduleInstance.name}' is missing archetype metadata. " +
                $"Assign {nameof(ShipModuleSOContainer)} or set {nameof(GameObjectInstanceIdentity)}.{nameof(GameObjectInstanceIdentity.ArchetypeId)}.");
        }
    }
}