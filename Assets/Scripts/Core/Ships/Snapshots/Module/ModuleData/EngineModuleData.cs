using System;
using Core.Ships.Snapshots.PixelatedRigidbody;

<<<<<<<<
Assets / Scripts / Core / Ships / Snapshots / Module / ModuleData / EngineModuleData.cs

namespace Core.Ships.Snapshots.Module.ModuleData

========

namespace Core.Ships.ModuleSnapshotPayloads
>>>>>>>>
Assets / Scripts / Core / Ships / ModuleSnapshotPayloads / EngineModuleData.cs
{
    [Serializable]
    public class EngineModuleData
{
public float maxThrust;
public float maxGimbalAngle;
public float gimbalSpeed;
public PixelatedRigidbodySnapshot[] nozzles;
}
}