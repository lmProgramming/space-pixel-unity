using UI.Common;
using UnityEngine.UIElements;

namespace UI.MVCVM
{
    public abstract class View<TViewModel> : IPanelRenderable
    {
        public virtual void BindUI(
            VisualElement root)
        {
        }

        public virtual void UnbindUI()
        {
        }

        public abstract void SetData(TViewModel viewModel);
    }
}