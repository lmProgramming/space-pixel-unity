namespace Ships.Serialization
{
    public interface IShipSnapshotService
    {
        ShipSnapshot CaptureSnapshot(Ship ship);

        string ToJson(ShipSnapshot snapshot, bool prettyPrint = true);

        ShipSnapshot FromJson(string json);
    }
}