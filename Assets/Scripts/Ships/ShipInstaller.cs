using Events.Gameplay.Ship;
using UnityEngine;
using Zenject;

namespace Ships
{
    public class ShipInstaller : MonoInstaller
    {
        [SerializeField] private ShipInitializeModulesEventChannel shipInitializeModulesEventChannel;

        public override void InstallBindings()
        {
            Container.Bind<ShipInitializeModulesEventChannel>()
                .FromInstance(shipInitializeModulesEventChannel)
                .AsSingle();
        }
    }
}