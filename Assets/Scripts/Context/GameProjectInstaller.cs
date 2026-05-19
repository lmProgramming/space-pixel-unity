using Core.Services;
using Services;
using Zenject;

namespace Context
{
    public class GameProjectInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IShipSnapshotService>()
                .To<ShipSnapshotService>()
                .AsSingle();
        }
    }
}