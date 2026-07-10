namespace Ships
{
    public interface ISas
    {
        bool IsSasOn { get; }
        void ToggleSas();
    }
}