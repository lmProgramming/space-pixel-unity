using UI.Common;

namespace UI.MVCVM
{
    public abstract class View<TViewModel> : PanelRendererBase
    {
        public abstract void SetData(TViewModel viewModel);
    }
}