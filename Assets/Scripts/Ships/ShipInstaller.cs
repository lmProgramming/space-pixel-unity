using Events.Ship;
using UnityEngine;
using Zenject;

namespace Ships
{
    public class ShipInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<ShipInitializeModulesEventChannelSO>()
                .FromMethod(_ => ScriptableObject.CreateInstance<
                    ShipInitializeModulesEventChannelSO>())
                .AsSingle();
        }
    }
}