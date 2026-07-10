namespace Events.UI
{
    public readonly struct TextInputFocusData
    {
        public readonly object Source;
        public readonly bool IsFocused;

        public TextInputFocusData(object source, bool isFocused)
        {
            Source = source;
            IsFocused = isFocused;
        }
    }
}