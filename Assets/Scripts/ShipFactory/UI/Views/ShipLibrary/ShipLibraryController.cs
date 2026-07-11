using UI.MVCVM;

namespace ShipFactory.UI.Views.ShipLibrary
{
    public class ShipLibraryController
        : Controller<
            ShipLibraryModel,
            ShipLibraryView,
            ShipLibraryViewModel>
    {
        public ShipLibraryController(
            ShipLibraryModel model,
            ShipLibraryView view)
            : base(model, view)
        {
        }

        protected override ShipLibraryViewModel
            CreateViewModel(
                ShipLibraryModel model)
        {
            return new ShipLibraryViewModel();
        }
    }
}