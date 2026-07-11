using System;
using UI.MVCVM;
using UnityEngine.UIElements;

namespace ShipFactory.UI.Views.ShipLibrary
{
    public class ShipLibraryView
        : IView<ShipLibraryViewModel>
    {
        private readonly VisualElement root;

        public ShipLibraryView(
            VisualElement root)
        {
            this.root = root;
        }

        public void SetData(
            ShipLibraryViewModel viewModel)
        {
        }

        public event Action CloseClicked;
    }
}