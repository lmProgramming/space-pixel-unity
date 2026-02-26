namespace Ships.Serialization
{
    public interface IShipSnapshotService
    {
        ShipSnapshot CaptureSnapshot(Ship ship);

        void ApplySnapshot(Ship ship, ShipSnapshot snapshot);

        string ToJson(ShipSnapshot snapshot, bool prettyPrint = true);

        ShipSnapshot FromJson(string json);
    }
}