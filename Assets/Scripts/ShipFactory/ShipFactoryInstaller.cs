using ShipFactory.Models;
using ShipFactory.UI;
using ShipFactory.UI.Runtime;
using ShipFactory.UI.ToolkitComponents;
using UI.Components.Notification;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace ShipFactory
{
    public class ShipFactoryInstaller : MonoInstaller
    {
        [SerializeField] private ShipModuleCatalog shipModuleCatalog;

        public override void InstallBindings()
        {
            Container.BindFactory<VisualElement, ModulePaletteController, ModulePaletteController.Factory>();

            Container.BindFactory<VisualElement, CameraInfoPanel, CameraInfoPanel.Factory>();

            Container.BindFactory<VisualElement, NotificationView, ShipFactoryFeedback, ShipFactoryCanvasController,
                ShipFactoryCanvasController.Factory>();
        }
    }
}