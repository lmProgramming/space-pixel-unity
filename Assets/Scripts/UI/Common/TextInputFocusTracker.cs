using Events.UI;
using UnityEngine.UIElements;

namespace UI.Common
{
    public sealed class TextInputFocusTracker
    {
        private readonly TextInputFocusEventChannel _channel;

        public TextInputFocusTracker(TextInputFocusEventChannel channel)
        {
            _channel = channel;
        }

        public void Track(TextField textField)
        {
            if (textField == null || _channel == null)
                return;

            textField.RegisterCallback<FocusInEvent>(OnFocusIn);
            textField.RegisterCallback<FocusOutEvent>(OnFocusOut);
            textField.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void Release(TextField textField)
        {
            if (textField == null || _channel == null)
                return;

            textField.UnregisterCallback<FocusInEvent>(OnFocusIn);
            textField.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            textField.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            _channel.Raise(new TextInputFocusData(textField, false));
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            _channel.Raise(new TextInputFocusData(evt.currentTarget, true));
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            _channel.Raise(new TextInputFocusData(evt.currentTarget, false));
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            _channel.Raise(new TextInputFocusData(evt.currentTarget, false));
        }
    }
}