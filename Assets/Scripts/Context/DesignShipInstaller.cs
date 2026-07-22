using Core.Constants;
using Ships;
using UnityEngine;
using Zenject;

namespace Context
{
    public class DesignShipInstaller : MonoInstaller
    {
        [SerializeField] private DesignShip designShip;

        public override void InstallBindings()
        {
            if (!designShip)
                throw new UnityException("[DesignShipInstaller] DesignShip is required.");

            Container.Bind<DesignShip>()
                .WithId(UIPanelPrefabConstants.NewCampaignPreviewShip)
                .FromInstance(designShip)
                .AsSingle();
        }
    }
}