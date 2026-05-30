using Events.UI;
using UnityEngine.UIElements;

namespace UI.Common
{
    public sealed class UiPointerTracker
    {
        private readonly PointerOverUiEventChannel _channel;

        public UiPointerTracker(PointerOverUiEventChannel channel)
        {
            _channel = channel;
        }

        public void Track(VisualElement element)
        {
            if (element == null || _channel == null)
                return;

            element.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            element.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            element.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void Release(VisualElement element)
        {
            if (element == null || _channel == null)
                return;

            _channel.Raise(new PointerOverUiData(element, false));
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            _channel.Raise(new PointerOverUiData(evt.currentTarget, true));
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            _channel.Raise(new PointerOverUiData(evt.currentTarget, false));
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            _channel.Raise(new PointerOverUiData(evt.currentTarget, false));
        }
    }
}
