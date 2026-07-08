namespace Core.Ships
{
    public enum ModuleType
    {
        Command,
        Resources,
        Weapon,
        Engine,
        Structural
    }

    public enum ConcreteModuleType
    {
        Command,
        Basic,
        Cannon,
        Laser,
        Engine
    }
}