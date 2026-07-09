using System;
using System.Collections.Generic;
using Core.Constants;
using Core.Services;
using Core.Ships;
using Core.State;
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
        [Inject(Id = Constants.PlayerShipId)] private IShip _playerShip;
        [Inject] private ISkirmishSnapshotCatalog _snapshotCatalog;
        [Inject] private IShipSnapshotService _snapshotService;

        public void SpawnFromSaveState()
        {
            if (spawnArea == null)
                throw new UnityException("[SkirmishSpawner] Spawn area is not assigned.");

            if (blockingMask == 0)
                blockingMask = LayerMask.GetMask("Obstacles", "Debris", "Friendly", "Enemy");

            ValidateSnapshotAvailability();

            var spawnRect = spawnArea.GetSpawnRect();

            var reservations = new List<SkirmishSpawnPlacement.SpawnReservation>();

            SpawnAndSetupPlayer(reservations);
            SpawnAsteroids(SaveState.AsteroidCount, spawnRect, reservations);
            SpawnShips(
                SaveState.EnemyShipCount,
                enemyShipShellPrefab,
                _snapshotCatalog.GetRandomEnemySnapshot,
                shipSeparationRadius,
                enemyTeam,
                spawnRect,
                reservations);
            SpawnShips(
                SaveState.FriendlyShipCount,
                friendlyShipShellPrefab,
                _snapshotCatalog.GetRandomFriendlySnapshot,
                shipSeparationRadius,
                friendlyTeam,
                spawnRect,
                reservations);
        }

        private void SpawnAndSetupPlayer(List<SkirmishSpawnPlacement.SpawnReservation> reservations)
        {
            var playerShipSnapshotFile = SaveState.PlayerShipSnapshotFilePath;

            if (!string.IsNullOrWhiteSpace(playerShipSnapshotFile))
                _snapshotService.ApplySnapshot(_playerShip,
                    _snapshotService.LoadSnapshotFromFile(playerShipSnapshotFile));

            _playerShip.InitializeModules();

            cameraManager.StartFollowingObject((_playerShip.CommandModule as MonoBehaviour)?.gameObject);

            reservations.Add(
                new SkirmishSpawnPlacement.SpawnReservation(_playerShip.GetPosition(), shipSeparationRadius));
        }

        private void ValidateSnapshotAvailability()
        {
            if (SaveState.EnemyShipCount > 0 && !_snapshotCatalog.HasEnemySnapshots())
                throw new UnityException(
                    "[SkirmishSpawner] Enemy ship count is greater than zero but enemy snapshot catalog is empty.");

            if (SaveState.FriendlyShipCount > 0 && !_snapshotCatalog.HasFriendlySnapshots())
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

        private void SpawnShips(int count,
            GameObject shipShellPrefab,
            Func<ShipSnapshot> getSnapshot,
            float shipRadius,
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
                if (!TryFindSpawnPosition(spawnRect, shipRadius, reservations, out var position))
                {
                    Debug.LogError($"[SkirmishSpawner] Failed to place ship #{i + 1} without overlap.");
                    continue;
                }

                var instance = instantiator.Instantiate(shipShellPrefab, position, RandomRotation(), spawnParent);
                var ship = instance.GetComponent<Ship>();
                if (ship == null)
                    throw new UnityException("[SkirmishSpawner] Spawned ship shell does not have an IShip component.");

                _snapshotService.ApplySnapshot(ship, getSnapshot());
                ship.SetTeam(team);
                ship.InitializeModules();
                reservations.Add(new SkirmishSpawnPlacement.SpawnReservation(position, shipRadius));
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