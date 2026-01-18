namespace Ships.Modules
{
    public struct ResourceDraw
    {
        public float Energy;
        public int Crew;

        public ResourceDraw(float energy, int crew)
        {
            Energy = energy;
            Crew = crew;
        }
    }
}