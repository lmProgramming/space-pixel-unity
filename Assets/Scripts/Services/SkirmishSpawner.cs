using System;
using System.Collections.Generic;
using Core.Services;
using Core.Ships;
using Gameplay.EasyTeam;
using Instantiation;
using Services.Camera;
using Ships;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Services
{
    public class SkirmishSpawner : MonoBehaviour, ISkirmishSpawner
    {
        [Header("References")]
        [SerializeField] private GameObject asteroidPrefab;

        [SerializeField] private GameObject playerShipShellPrefab;
        [SerializeField] private GameObject enemyShipShellPrefab;
        [SerializeField] private GameObject friendlyShipShellPrefab;
        [SerializeField] private Team enemyTeam;
        [SerializeField] private Team friendlyTeam;
        [SerializeField] private SkirmishSpawnArea spawnArea;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private CameraManager cameraManager;

        [Header("Placement")]
        [SerializeField] private float asteroidSeparationRadius = 25f;

        [SerializeField] private float shipSeparationRadius = 40f;
        [SerializeField] private int maxSpawnAttempts = 50;
        [SerializeField] private LayerMask blockingMask;

        [SerializeField] private Instantiator instantiator;
        [Inject] private IActivePlayerShipProvider _activePlayerShipProvider;
        [Inject] private ISkirmishSnapshotCatalog _snapshotCatalog;
        [Inject] private IShipSnapshotService _snapshotService;
        [Inject] private IBattleSpawnConfigurationProvider _spawnConfigurationProvider;

        public void SpawnFromSaveState()
        {
            if (spawnArea == null)
                throw new UnityException("[SkirmishSpawner] Spawn area is not assigned.");

            if (blockingMask == 0)
                blockingMask = LayerMask.GetMask("Obstacles", "Debris", "Friendly", "Enemy");

            var configuration = _spawnConfigurationProvider.GetConfiguration();
            ValidateSnapshotAvailability(configuration);

            var spawnRect = spawnArea.GetSpawnRect();
            var reservations = new List<SkirmishSpawnPlacement.SpawnReservation>();

            if (configuration.PlayerShipSnapshot != null)
                SpawnPlayer(configuration, spawnRect, reservations);

            SpawnAsteroids(configuration.AsteroidCount, spawnRect, reservations);
            SpawnSnapshotShips(
                configuration.AllySnapshots,
                friendlyShipShellPrefab,
                friendlyTeam,
                spawnRect,
                reservations);
            SpawnShips(
                configuration.RandomFriendlyCount,
                friendlyShipShellPrefab,
                _snapshotCatalog.GetRandomFriendlySnapshot,
                friendlyTeam,
                spawnRect,
                reservations);
            SpawnShips(
                configuration.EnemyCount,
                enemyShipShellPrefab,
                _snapshotCatalog.GetRandomEnemySnapshot,
                enemyTeam,
                spawnRect,
                reservations);
        }

        private void SpawnPlayer(
            IBattleSpawnConfiguration configuration,
            Rect spawnRect,
            List<SkirmishSpawnPlacement.SpawnReservation> reservations)
        {
            if (!playerShipShellPrefab)
                throw new UnityException("[SkirmishSpawner] Player ship shell prefab is not assigned.");

            if (!TryFindSpawnPosition(spawnRect, shipSeparationRadius, reservations, out var position))
                throw new UnityException("[SkirmishSpawner] Failed to place player ship.");

            var instance = instantiator.Instantiate(playerShipShellPrefab, position, RandomRotation(), spawnParent);
            var ship = instance.GetComponent<Ship>();
            if (ship == null)
                throw new UnityException("[SkirmishSpawner] Spawned player ship shell does not have a Ship component.");

            _snapshotService.ApplySnapshot(ship, configuration.PlayerShipSnapshot);

            ship.SetTeam(friendlyTeam);
            ship.InitializeModules();
            _activePlayerShipProvider.SetActiveShip(ship);
            cameraManager.StartFollowingObject((ship.CommandModule as MonoBehaviour)?.gameObject);
            reservations.Add(new SkirmishSpawnPlacement.SpawnReservation(position, shipSeparationRadius));
        }

        private void ValidateSnapshotAvailability(IBattleSpawnConfiguration configuration)
        {
            if (configuration.EnemyCount > 0 && !_snapshotCatalog.HasEnemySnapshots())
                throw new UnityException(
                    "[SkirmishSpawner] Enemy ship count is greater than zero but enemy snapshot catalog is empty.");

            if (configuration.RandomFriendlyCount > 0 && !_snapshotCatalog.HasFriendlySnapshots())
                throw new UnityException(
                    "[SkirmishSpawner] Friendly ship count is greater than zero but friendly snapshot catalog is empty.");
        }

        private void SpawnAsteroids(
            int count,
            Rect spawnRect,
            List<SkirmishSpawnPlacement.SpawnReservation> reservations)
        {
            if (count <= 0)
                return;

            if (!asteroidPrefab)
                throw new UnityException("[SkirmishSpawner] Asteroid prefab is not assigned.");

            for (var i = 0; i < count; i++)
            {
                if (!TryFindSpawnPosition(spawnRect, asteroidSeparationRadius, reservations, out var position))
                {
                    Debug.LogError($"[SkirmishSpawner] Failed to place asteroid #{i + 1} without overlap.");
                    continue;
                }

                instantiator.Instantiate(asteroidPrefab, position, RandomRotation(), spawnParent);
                reservations.Add(new SkirmishSpawnPlacement.SpawnReservation(position, asteroidSeparationRadius));
            }
        }

        private void SpawnSnapshotShips(
            IReadOnlyList<ShipSnapshot> snapshots,
            GameObject shipShellPrefab,
            Team team,
            Rect spawnRect,
            List<SkirmishSpawnPlacement.SpawnReservation> reservations)
        {
            if (snapshots == null || snapshots.Count == 0)
                return;

            if (!shipShellPrefab)
                throw new UnityException("[SkirmishSpawner] Ship shell prefab is not assigned.");

            for (var i = 0; i < snapshots.Count; i++)
            {
                if (!TryFindSpawnPosition(spawnRect, shipSeparationRadius, reservations, out var position))
                {
                    Debug.LogError($"[SkirmishSpawner] Failed to place ally ship #{i + 1} without overlap.");
                    continue;
                }

                var instance = instantiator.Instantiate(shipShellPrefab, position, RandomRotation(), spawnParent);
                var ship = instance.GetComponent<Ship>();
                if (ship == null)
                    throw new UnityException("[SkirmishSpawner] Spawned ship shell does not have a Ship component.");

                _snapshotService.ApplySnapshot(ship, snapshots[i]);
                ship.SetTeam(team);
                ship.InitializeModules();
                reservations.Add(new SkirmishSpawnPlacement.SpawnReservation(position, shipSeparationRadius));
            }
        }

        private void SpawnShips(
            int count,
            GameObject shipShellPrefab,
            Func<ShipSnapshot> getSnapshot,
            Team team,
            Rect spawnRect,
            List<SkirmishSpawnPlacement.SpawnReservation> reservations)
        {
            if (count <= 0)
                return;

            if (!shipShellPrefab)
                throw new UnityException("[SkirmishSpawner] Ship shell prefab is not assigned.");

            for (var i = 0; i < count; i++)
            {
                if (!TryFindSpawnPosition(spawnRect, shipSeparationRadius, reservations, out var position))
                {
                    Debug.LogError($"[SkirmishSpawner] Failed to place ship #{i + 1} without overlap.");
                    continue;
                }

                var instance = instantiator.Instantiate(shipShellPrefab, position, RandomRotation(), spawnParent);
                var ship = instance.GetComponent<Ship>();
                if (ship == null)
                    throw new UnityException("[SkirmishSpawner] Spawned ship shell does not have a Ship component.");

                _snapshotService.ApplySnapshot(ship, getSnapshot());
                ship.SetTeam(team);
                ship.InitializeModules();
                reservations.Add(new SkirmishSpawnPlacement.SpawnReservation(position, shipSeparationRadius));
            }
        }

        private bool TryFindSpawnPosition(
            Rect spawnRect,
            float radius,
            List<SkirmishSpawnPlacement.SpawnReservation> reservations,
            out Vector2 position)
        {
            return SkirmishSpawnPlacement.TryFindPosition(
                spawnRect,
                radius,
                reservations,
                maxSpawnAttempts,
                blockingMask,
                out position);
        }

        private static Quaternion RandomRotation()
        {
            return Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
    }
}