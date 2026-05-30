namespace Events.UI
{
    public readonly struct PointerOverUiData
    {
        public readonly object Element;
        public readonly bool IsOver;

        public PointerOverUiData(object element, bool isOver)
        {
            Element = element;
            IsOver = isOver;
        }
    }
}
