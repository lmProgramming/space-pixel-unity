using UnityEngine.UIElements;

namespace UI
{
    public interface IPanelRenderable
    {
        void BindUI(VisualElement root);

        void UnbindUI();
    }
}