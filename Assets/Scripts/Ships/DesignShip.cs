using System;
using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using Core.Services;
using Core.Ships;
using Core.Ships.Blueprints;
using Core.Ships.Module;
using LMPro.DataStructures;
using LMPro.DataStructures.Graph;
using LMPro.External.IsAlive;
using Ships.ModuleConnection;
using Ships.Modules;
using Ships.Systems.Resources;
using UnityEngine;
using Zenject;
using ZLinq;

namespace Ships
{
    [RequireComponent(typeof(ResourceManager))]
    [DisallowMultipleComponent]
    public class DesignShip : MonoBehaviour, IShip
    {
        private readonly List<IModule> _allModulesCache = new();

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly DefaultDictionary<ModuleType, List<IModule>> _modulesDictionary =
            new(() => new List<IModule>());

        private BiCohesionGraph<IModule> _biCohesionGraph;

        [Inject] private DiContainer _container;

        private IModuleConnectionFactory _moduleConnectionFactory;

        [Inject] private IModuleRestoreFactory _moduleRestoreFactory;

        public int CrewMissingCount =>
            ModuleGraph.GetAllNodes().AsValueEnumerable().Sum(module => module.CrewMissingCount);

        private void Awake()
        {
            ResourceManager = GetComponent<ResourceManager>();
            _moduleConnectionFactory = GetComponent<IModuleConnectionFactory>();

            if (ResourceManager == null)
                throw new UnityException("[Ship] ResourceManager is required.");
            if (_moduleConnectionFactory == null)
                throw new UnityException("[Ship] IModuleConnectionFactory is required.");
        }

        public ITeam Team => throw new NotSupportedException();
        public IModule CommandModule { get; private set; }
        public Collider2D[] OwnColliders => throw new NotSupportedException();
        public float GeneralEfficiency => 1f;

        public IResourceManager ResourceManager { get; private set; }

        public List<IWeapon> Weapons =>
            _modulesDictionary[ModuleType.Weapon].AsValueEnumerable().Cast<IWeapon>().Where(e => e.IsAliveEnabled())
                .ToList();

        public List<IEngine> Engines =>
            _modulesDictionary[ModuleType.Engine].AsValueEnumerable().Cast<IEngine>().Where(e => e.IsAliveEnabled())
                .ToList();

        public float CaptainMultiplier => throw new NotSupportedException();
        public string Name => transform.name;
        public IReadOnlyList<IModule> AllModules => _allModulesCache;
        public bool IsSASOn => false;
        public bool IsDesignMode => true;
        public ShipBlueprint Blueprint { get; private set; } = new();

        public Graph<IModule> ModuleGraph => _biCohesionGraph;
        public Vector2 AttackTargetPosition => throw new NotSupportedException();

        public Vector2 GetPosition()
        {
            throw new NotSupportedException();
        }

        public void OnModuleConnectionLost(IModule module)
        {
            if (module == null) return;

            Debug.Log($"[Ship] Module destroyed: {module.Transform?.name}", module.Transform);

            HandleModuleChange();
        }

        public void ManualAddModule(IModule module)
        {
            if (module == null || !module.Transform) throw new ArgumentNullException(nameof(module));
            module.Transform.SetParent(transform);
            InitializeModules();
        }

        public void ManualRemoveModule(IModule module)
        {
            switch (module)
            {
                case null:
                    throw new ArgumentNullException(nameof(module));
                case Module concreteModule:
                    concreteModule.DetachAllConnections();
                    break;
            }

            if (!module.Transform)
            {
                Debug.LogError($"[Ship] ManualRemoveModule: module.Transform is null for module {module}", this);
                return;
            }

            module.Transform.SetParent(null);
            InitializeModules();
        }

        public void DestroyAllModules()
        {
            var existingModules = GetComponentsInChildren<Module>();

            foreach (var module in existingModules)
                if (module)
                    module.DestroyModule();
        }

        public void InitializeModules()
        {
            CommandModule = GetComponentInChildren<Command>();

            if (CommandModule == null)
            {
                _biCohesionGraph = null;
                _modulesDictionary.Clear();
                _allModulesCache.Clear();
                ResourceManager.Recalculate(_allModulesCache);
                return;
            }

            _biCohesionGraph = new BiCohesionGraph<IModule>(CommandModule);

            _moduleConnectionFactory.ConnectModules(this, transform);

            HandleModuleChange();
        }

        public void SetTeam(ITeam newTeam)
        {
            throw new NotSupportedException();
        }

        public void DestroyAllModulesSilently()
        {
            var existingModules = GetComponentsInChildren<Module>();

            foreach (var module in existingModules)
            {
                module.SetShip(null);
                module.transform.SetParent(null, true);
                Destroy(module.gameObject);
            }
        }

        public void SetBlueprint(ShipBlueprint blueprint)
        {
            Blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
        }

        public void SyncBlueprintFromLiveModules()
        {
            Blueprint = MergeBlueprintFromLiveModules();
        }

        public ShipSnapshot CaptureSnapshot(IGameContentCatalog contentCatalog)
        {
            if (!this.IsAlive())
            {
                Debug.LogError("[Ship] Cannot capture snapshot: ship is null");
                return null;
            }

            if (contentCatalog == null) throw new ArgumentNullException(nameof(contentCatalog));

            var snapshot = new ShipSnapshot(Name);

            foreach (var module in AllModules)
            {
                var moduleSnapshot = module.CaptureSnapshot(contentCatalog);
                snapshot.modules.Add(moduleSnapshot);

                if (CommandModule != module) continue;

                if (snapshot.commandModuleInstanceId != null)
                    throw new InvalidOperationException(
                        "[Ship] Multiple command modules found. Only the first one will be recorded in the snapshot.");

                snapshot.commandModuleInstanceId = moduleSnapshot.instanceId;
            }

            snapshot.blueprint = MergeBlueprintFromLiveModules();

            Debug.Log($"[Ship] Captured snapshot of '{Name}' with {snapshot.modules.Count} modules");
            return snapshot;
        }

        public void RestoreFromSnapshot(ShipSnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (contentCatalog == null) throw new ArgumentNullException(nameof(contentCatalog));

            SetBlueprint(ResolveBlueprintFromSnapshot(snapshot));

            DestroyAllModulesSilently();

            CreateModulesFromSnapshot(snapshot, contentCatalog);

            InitializeModules();

            HandleModuleChange();

            Debug.Log(
                $"[Ship] Applied snapshot '{snapshot.shipName}' to '{Name}' ({snapshot.modules.Count} modules)");
        }

        private ShipBlueprint MergeBlueprintFromLiveModules()
        {
            var result = Blueprint ?? new ShipBlueprint();
            if (result.modules == null)
                result.modules = new List<ModuleBlueprint>();

            foreach (var entry in result.modules)
            {
                if (entry != null && string.IsNullOrWhiteSpace(entry.blueprintId))
                    entry.blueprintId = Guid.NewGuid().ToString("N");
            }

            if (result.modules.Count == 0)
            {
                foreach (var module in AllModules)
                {
                    if (module.Blueprint == null)
                        throw new UnityException(
                            $"[Ship] Module '{module.Transform?.name}' has no blueprint during capture.");
                    result.modules.Add(module.Blueprint);
                }

                return result;
            }

            var byId = result.modules.AsValueEnumerable()
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.blueprintId))
                .ToDictionary(entry => entry.blueprintId);

            foreach (var module in AllModules)
            {
                if (module.Blueprint == null)
                    throw new UnityException(
                        $"[Ship] Module '{module.Transform?.name}' has no blueprint during capture.");

                if (string.IsNullOrWhiteSpace(module.Blueprint.blueprintId))
                    throw new UnityException(
                        $"[Ship] Module '{module.Transform?.name}' has empty blueprint id during capture.");

                byId[module.Blueprint.blueprintId] = module.Blueprint;
            }

            result.modules = byId.Values.AsValueEnumerable().ToList();
            return result;
        }

        private static ShipBlueprint ResolveBlueprintFromSnapshot(ShipSnapshot snapshot)
        {
            if (snapshot.blueprint?.modules != null && snapshot.blueprint.modules.Count > 0)
                return snapshot.blueprint;

            var synthesized = new ShipBlueprint();
            foreach (var moduleSnapshot in snapshot.modules)
            {
                if (moduleSnapshot.blueprint != null)
                {
                    synthesized.modules.Add(moduleSnapshot.blueprint);
                    continue;
                }

                synthesized.modules.Add(new ModuleBlueprint(
                    moduleSnapshot.instanceId,
                    moduleSnapshot.archetypeId ?? string.Empty,
                    moduleSnapshot.localPosition,
                    moduleSnapshot.localRotation));
            }

            return synthesized;
        }

        private void CreateModulesFromSnapshot(ShipSnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            ShipSnapshotModulePlacer.CreateModulesFromSnapshot(this, transform, snapshot, contentCatalog,
                _moduleRestoreFactory);
        }

        private void HandleModuleChange()
        {
            _modulesDictionary.Clear();
            _allModulesCache.Clear();

            foreach (var module in ModuleGraph.GetAllNodes())
            {
                var mod = module as Module;
                _modulesDictionary[module.Type].Add(mod);
                _allModulesCache.Add(mod);
            }

            ResourceManager.Recalculate(_allModulesCache);

            ConfigureModulesForDesignMode();
        }

        private void ConfigureModulesForDesignMode()
        {
            foreach (var module in _allModulesCache)
            {
                var rigidbody = module.PixelatedRigidbody?.Rigidbody;
                if (rigidbody)
                    rigidbody.simulated = false;

                if (module is Engine engine)
                    engine.SuppressNozzleExhaustForDesignMode();
            }

            MarkEnginesActivity(false);
        }

        private void MarkEnginesActivity(bool active)
        {
            foreach (var engine in Engines)
                engine.SetActive(active);
        }
    }
}