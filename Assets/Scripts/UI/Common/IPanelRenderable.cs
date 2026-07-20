using UnityEngine.UIElements;

namespace UI.Common
{
    public interface IPanelRenderable
    {
        void BindUI(VisualElement root);

        void UnbindUI();
    }
}