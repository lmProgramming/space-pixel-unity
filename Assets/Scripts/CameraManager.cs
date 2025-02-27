using LM;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private GameObject objectToFollow;
    [SerializeField] private bool forcedToFollowCurrentObject;
    [SerializeField] private bool followRotation;

    [SerializeField] private float dragSpeed = 1f;
    [SerializeField] private float keySpeed = 1f;

    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private float mobileScrollSpeed = 1f;

    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float minZoom = 1f;

    public bool updateCamera = true;

    private Camera _mainCamera;
    private Vector2 _previousMousePosition = Vector2.zero;

    private static bool IsMobile => Application.platform == RuntimePlatform.Android ||
                                    Application.platform == RuntimePlatform.IPhonePlayer;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!updateCamera)
            return;

        if (objectToFollow)
            FollowObject();
        else
            ProcessDrag();

        ProcessZoom();

        if (!IsMobile) _previousMousePosition = Input.mousePosition;
    }

    private void FollowObject()
    {
        var targetPosition = objectToFollow.transform.position;
        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);

        if (!followRotation) return;
        var targetRotationZ = objectToFollow.transform.rotation.eulerAngles.z;
        transform.rotation = Quaternion.Euler(0, 0, targetRotationZ);
    }

    public void StartFollowingObject(GameObject newObjectToFollow)
    {
        objectToFollow = newObjectToFollow;
    }

    public void ForceStartFollowingObject(GameObject newObjectToFollow)
    {
        objectToFollow = newObjectToFollow;
        forcedToFollowCurrentObject = true;
    }

    public void StopFollowingObject()
    {
        if (forcedToFollowCurrentObject)
            return;

        objectToFollow = null;
    }

    public void ForceStopFollowingObject()
    {
        objectToFollow = null;
        forcedToFollowCurrentObject = false;
    }

    private void ProcessDrag()
    {
        if (!ShouldProcessDrag())
            return;

        var dragDelta = IsMobile ? GetMobileDragDelta() : GetDesktopDragDelta();

        // Translate the camera, scaled by drag speed and the current orthographic size.
        transform.Translate(dragDelta * (dragSpeed * _mainCamera.orthographicSize), Space.World);
    }

    private static bool ShouldProcessDrag()
    {
        if (IsMobile) return Input.touchCount > 0 && !GameInput.IsPointerOverUI;

        return (Input.GetMouseButton(0) && !GameInput.IsPointerOverUI) ||
               Input.GetAxis("Horizontal") != 0 ||
               Input.GetAxis("Vertical") != 0;
    }

    private static Vector2 GetMobileDragDelta()
    {
        if (Input.touchCount != 1) return Vector2.zero;

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Ended)
            return -touch.deltaPosition;

        return Vector2.zero;
    }

    private Vector2 GetDesktopDragDelta()
    {
        if (Input.GetMouseButton(0)) return _previousMousePosition - (Vector2)Input.mousePosition;

        var x = Input.GetAxis("Horizontal") * keySpeed;
        var y = Input.GetAxis("Vertical") * keySpeed;
        return new Vector2(x, y);
    }

    private void ProcessZoom()
    {
        if (GameInput.IsPointerOverUI)
            return;

        var zoomIncrement = IsMobile ? GetMobileZoomIncrement() : -Input.GetAxis("Mouse ScrollWheel");

        _mainCamera.orthographicSize = Mathf.Clamp(
            _mainCamera.orthographicSize + zoomIncrement * scrollSpeed,
            minZoom,
            maxZoom);
    }

    private float GetMobileZoomIncrement()
    {
        if (Input.touchCount != 2) return 0f;

        var touchZero = Input.GetTouch(0);
        var touchOne = Input.GetTouch(1);

        var touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        var touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        var prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        var currentMagnitude = (touchZero.position - touchOne.position).magnitude;

        var deltaMagnitudeDiff = prevMagnitude - currentMagnitude;
        return deltaMagnitudeDiff * mobileScrollSpeed;
    }
}