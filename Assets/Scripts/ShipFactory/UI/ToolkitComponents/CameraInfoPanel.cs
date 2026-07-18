using System;
using Events.Camera;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShipFactory.UI.ToolkitComponents
{
    public class CameraInfoPanel
    {
        private const string CameraButtonName = "camera-reset-button";
        private const string CameraPositionLabelName = "camera-info-position";
        private readonly Label _cameraPositionLabel;
        private readonly Button _cameraResetButton;
        private readonly CameraResetRequestEventChannel _cameraResetRequestEventChannel;

        public CameraInfoPanel(VisualElement root, CameraResetRequestEventChannel cameraResetRequestEventChannel)
        {
            _cameraResetRequestEventChannel = cameraResetRequestEventChannel
                                              ?? throw new ArgumentNullException(
                                                  nameof(cameraResetRequestEventChannel));

            _cameraResetButton = root.Q<Button>(CameraButtonName);
            _cameraPositionLabel = root.Q<Label>(CameraPositionLabelName);

            if (_cameraResetButton == null || _cameraPositionLabel == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCameraInfo] Required camera info elements are missing in UXML!");

            _cameraResetButton.clicked += RequestReset;
        }

        public void Update(Camera camera)
        {
            _cameraPositionLabel.text = $"({camera.transform.position.x:0.0}, {camera.transform.position.y:0.0})";
        }

        public void RequestReset()
        {
            _cameraResetRequestEventChannel.Raise();
        }
    }
}