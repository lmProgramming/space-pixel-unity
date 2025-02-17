using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private Camera _mainCamera;

    [SerializeField] private float scrollMultiplier = 1f;
    [SerializeField] private float minScroll;
    [SerializeField] private float maxScroll;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        var mouseScroll = Input.GetAxis("Mouse ScrollWheel") * scrollMultiplier;
        
        var orthographicSize = _mainCamera.orthographicSize + mouseScroll;
        
        orthographicSize = Mathf.Clamp(orthographicSize, minScroll, maxScroll);
        
        _mainCamera.orthographicSize = orthographicSize;
    }
}
