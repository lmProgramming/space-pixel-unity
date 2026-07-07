using System;
using UnityEngine;

<<<<<<<<
Assets / Scripts / Core / Ships / Snapshots / Module / ModuleData / CannonModuleData.cs

namespace Core.Ships.Snapshots.Module.ModuleData

========

namespace Core.Ships.ModuleSnapshotPayloads
>>>>>>>>
Assets / Scripts / Core / Ships / ModuleSnapshotPayloads / CannonModuleData.cs
{
    [Serializable]
    public class CannonModuleData
{
public float reloadTime;
public float projectileSpeed;
public string projectileContentId;
public string spriteContentId;
public Vector2[] projectileLocalSpawnPoints;
}
}