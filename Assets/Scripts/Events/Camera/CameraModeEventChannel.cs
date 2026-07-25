using Core.Services;
using UnityEngine;

namespace Events.Camera
{
    [CreateAssetMenu(fileName = "CameraModeEventChannel", menuName = "Events/Camera/CameraModeEventChannel")]
    public class CameraModeEventChannel : EventChannelSO<CameraMode>
    {
    }
}