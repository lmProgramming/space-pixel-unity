using Core.Services.Repair;
using Services.Repair;
using ShipFactory.UI;
using ShipFactory.UI.Runtime;
using ShipFactory.UI.ToolkitComponents;
using ShipFactory.UI.Views.Repair;
using UnityEngine.UIElements;
using Zenject;

namespace ShipFactory
{
    public class ShipFactoryInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IShipRepairService>()
                .To<ShipRepairService>()
                .AsSingle();

            Container.Bind<IShipRepairRunner>()
                .To<ShipRepairRunner>()
                .AsSingle();

            Container.BindFactory<VisualElement, ModulePaletteController, ModulePaletteController.Factory>();

            Container.BindFactory<VisualElement, CameraInfoPanel, CameraInfoPanel.Factory>();

            Container.BindFactory<VisualElement, ShipFactoryFeedback, ShipFactoryCanvasController,
                ShipFactoryCanvasController.Factory>();

            Container.BindFactory<VisualElement, RepairPanelController, RepairPanelController.Factory>();
        }
    }
}