using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private float scrollMultiplier = 1f;
    [SerializeField] private float minScroll;
    [SerializeField] private float maxScroll;
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        var mouseScroll = Input.GetAxis("Mouse ScrollWheel") * scrollMultiplier;

        var orthographicSize = _mainCamera.orthographicSize + mouseScroll;

        orthographicSize = Mathf.Clamp(orthographicSize, minScroll, maxScroll);

        _mainCamera.orthographicSize = orthographicSize;
    }
}