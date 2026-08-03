using Core.Constants;
using Core.Services;
using Events.Camera;
using Events.Game;
using Events.Game.BattleOver;
using Events.Gameplay.Collision;
using Events.Gameplay.Shooting;
using Events.UI;
using Services;
using Services.GameInput;
using ShipFactory.Models;
using UnityEngine;
using Zenject;

namespace Context
{
    public class GameProjectInstaller : MonoInstaller
    {
        [Header("Channels")]
        [SerializeField] private CollisionEventChannelSO physicsCollisionChannel;

        [SerializeField] private CameraResetRequestEventChannel cameraResetRequestEventChannel;
        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;
        [SerializeField] private TextInputFocusEventChannel textInputFocusChannel;
        [SerializeField] private PauseStateEventChannel pauseStateChannel;
        [SerializeField] private ShootingEventChannel shootingEventChannel;
        [SerializeField] private CameraModeEventChannel cameraModeEventChannel;

        [Header("SOs")]
        [SerializeField] private ShipModuleCatalog shipModuleCatalog;

        [SerializeField] private GameContentCatalog gameContentCatalog;
        [SerializeField] private SkirmishSnapshotCatalog skirmishSnapshotCatalog;
        [SerializeField] private GameplayConstants gameplayConstants;
        [SerializeField] private ProgressionConstants progressionConstants;

        [SerializeField]
        private BattleOverEventChannel battleOverEventChannel;

        [Header("UI Panels")]
        [SerializeField] private GameObject settingsPanelPrefab;

        [SerializeField] private GameObject pausePanelPrefab;
        [SerializeField] private GameObject optionsPopupPanelPrefab;
        [SerializeField] private GameObject newCampaignPanelPrefab;
        [SerializeField] private GameObject progressionSlotsPanelPrefab;
        [SerializeField] private GameObject freeModeSetupPanelPrefab;
        [SerializeField] private GameObject shipLibraryPanelPrefab;
        [SerializeField] private GameObject notificationHostPanelPrefab;
        [SerializeField] private GameObject missionResolutionPanelPrefab;
        [SerializeField] private GameObject progressionGameOverPanelPrefab;

        public override void InstallBindings()
        {
            if (pointerOverUiChannel == null)
                throw new UnityException("[GameProjectInstaller] Pointer Over UI event channel must be assigned.");

            if (textInputFocusChannel == null)
                throw new UnityException("[GameProjectInstaller] Text Input Focus event channel must be assigned.");

            if (pauseStateChannel == null)
                throw new UnityException("[GameProjectInstaller] Pause State event channel must be assigned.");

            if (shipModuleCatalog is not IShipModuleCatalog typedShipModuleCatalog)
                throw new UnityException(
                    "[GameProjectInstaller] Ship module catalog must implement IShipModuleCatalog.");

            if (gameContentCatalog is not IGameContentCatalog typedGameContentCatalog)
                throw new UnityException(
                    "[GameProjectInstaller] Game content catalog must implement IGameContentCatalog.");

            if (skirmishSnapshotCatalog is not ISkirmishSnapshotCatalog typedSkirmishSnapshotCatalog)
                throw new UnityException(
                    "[GameProjectInstaller] Skirmish snapshot catalog must implement ISkirmishSnapshotCatalog.");

            if (shootingEventChannel == null)
                throw new UnityException("[GameProjectInstaller] Shooting event channel must be assigned.");

            if (battleOverEventChannel == null)
                throw new UnityException("[GameProjectInstaller] Battle victory event channel must be assigned.");

            if (!cameraResetRequestEventChannel)
                throw new UnityException(
                    $"[GameProjectInstaller] {nameof(cameraResetRequestEventChannel)} must be assigned.");

            if (!physicsCollisionChannel)
                throw new UnityException($"[GameProjectInstaller] Missing {nameof(physicsCollisionChannel)}");

            if (!cameraModeEventChannel)
                throw new UnityException($"[GameProjectInstaller] Missing {nameof(cameraModeEventChannel)}");

            if (!gameplayConstants)
                throw new UnityException($"[GameProjectInstaller] Missing {nameof(gameplayConstants)}");

            if (!progressionConstants)
                throw new UnityException($"[GameProjectInstaller] Missing {nameof(progressionConstants)}");

            Container.Bind<IShipModuleCatalog>()
                .FromInstance(typedShipModuleCatalog)
                .AsSingle();

            Container.Bind<IGameContentCatalog>()
                .FromInstance(typedGameContentCatalog)
                .AsSingle();

            Container.Bind<ISkirmishSnapshotCatalog>()
                .FromInstance(typedSkirmishSnapshotCatalog)
                .AsSingle();

            Container.Bind<IShipSnapshotService>()
                .To<ShipSnapshotService>()
                .AsSingle();

            Container.Bind<IShipSnapshotRepository>()
                .To<ShipSnapshotRepository>()
                .AsSingle();

            Container.Bind<IProgressionRepository>()
                .To<ProgressionRepository>()
                .AsSingle();

            Container.Bind<INextBattleService>()
                .To<NextBattleService>()
                .AsSingle();

            Container.Bind<IGameInput>()
                .To<GameInput>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("GameInput")
                .AsSingle()
                .NonLazy();

            Container.Bind<GameplayConstants>()
                .FromInstance(gameplayConstants)
                .AsSingle();

            Container.Bind<ProgressionConstants>()
                .FromInstance(progressionConstants)
                .AsSingle();

            BindChannels();

            BindUiPanelPrefabs();
        }

        private void BindChannels()
        {
            Container.Bind<PointerOverUiEventChannel>()
                .FromInstance(pointerOverUiChannel)
                .AsSingle();

            Container.Bind<TextInputFocusEventChannel>()
                .FromInstance(textInputFocusChannel)
                .AsSingle();

            Container.Bind<PauseStateEventChannel>()
                .FromInstance(pauseStateChannel)
                .AsSingle();

            Container.Bind<ShootingEventChannel>()
                .FromInstance(shootingEventChannel)
                .AsSingle();

            Container.Bind<BattleOverEventChannel>()
                .FromInstance(battleOverEventChannel)
                .AsSingle();

            Container.Bind<CameraResetRequestEventChannel>()
                .FromInstance(cameraResetRequestEventChannel)
                .AsSingle();

            Container.Bind<CollisionEventChannelSO>()
                .FromInstance(physicsCollisionChannel)
                .AsSingle();

            Container.Bind<CameraModeEventChannel>()
                .FromInstance(cameraModeEventChannel)
                .AsSingle();
        }

        private void BindUiPanelPrefabs()
        {
            BindUiPanel(UIPanelPrefabConstants.Settings, settingsPanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.Pause, pausePanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.OptionsPopup, optionsPopupPanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.NewCampaign, newCampaignPanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.ProgressionSlots, progressionSlotsPanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.FreeModeSetup, freeModeSetupPanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.ShipLibrary, shipLibraryPanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.NotificationHost, notificationHostPanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.MissionResolution, missionResolutionPanelPrefab);
            BindUiPanel(UIPanelPrefabConstants.ProgressionGameOver, progressionGameOverPanelPrefab);
        }

        private void BindUiPanel(string panelId, GameObject prefab)
        {
            if (!prefab)
                throw new UnityException(
                    $"[GameProjectInstaller] UI panel prefab for '{panelId}' must be assigned.");

            Container.Bind<GameObject>()
                .WithId(panelId)
                .FromInstance(prefab)
                .AsCached();
        }
    }
}