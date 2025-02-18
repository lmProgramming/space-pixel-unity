using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Other
{
    public sealed class GameInput : MonoBehaviour
    {
        public static GameInput Instance;

        private static Camera _mainCamera;

        public static float PressingTime;

        private static float _timeSinceLastLeftClick = 100f;

        public float maxTimeBetweenDoubleClicks = 0.5f;

        public static bool LeftDoubleClick;

        public static bool PressingAfterLeftDoubleClick;

        private static float _deltaTime;
        private static float _unscaledDeltaTime;
        public static float SimDeltaTime;
        private static float _simSpeed = 1;

        public static int AmountOfHeldUIElements;

        private void Awake()
        {
            Instance = this;
            _simSpeed = 1f;
        }

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        public static void StartHoldingUIElement()
        {
            AmountOfHeldUIElements++;
        }

        public static void StopHoldingUIElement()
        {
            AmountOfHeldUIElements--;
        }

        private void Update()
        {
            _deltaTime = Time.deltaTime;
            SimDeltaTime = _deltaTime * _simSpeed;
            _unscaledDeltaTime = Time.unscaledDeltaTime;

            LeftDoubleClick = false;

            if (JustClicked)
            {
                if (_timeSinceLastLeftClick < maxTimeBetweenDoubleClicks)
                {
                    LeftDoubleClick = true;
                    PressingAfterLeftDoubleClick = true;

                    // after double click, we don't want our next click to also be double click
                    _timeSinceLastLeftClick = maxTimeBetweenDoubleClicks;
                }
                else
                {
                    _timeSinceLastLeftClick = 0f;
                }
            }
            else
            {
                if (JustStoppedClicking) PressingAfterLeftDoubleClick = false;

                if (Pressing)
                    PressingTime += _unscaledDeltaTime;
                else
                    PressingTime = 0f;

                _timeSinceLastLeftClick += _unscaledDeltaTime;
            }
        }

        private static bool JustClicked => Input.GetMouseButtonDown(0) ||
                                           (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began);

        public static bool JustClickedOutsideUI =>
            (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)) &&
            !IsPointerOverUI;

        private static bool JustStoppedClicking
        {
            get { return Input.touches.Any(touch => touch.phase == TouchPhase.Ended) || Input.GetMouseButtonUp(0); }
        }

        public static bool JustStoppedClickingOutsideUI
        {
            get
            {
                foreach (var touch in Input.touches)
                    if (touch.phase == TouchPhase.Ended && !IsPointerOverUI)
                        return true;

                return Input.GetMouseButtonUp(0) && !IsPointerOverUI;
            }
        }

        private static bool Pressing => Input.GetMouseButton(0) ||
                                        (Input.touchCount == 1 && Input.GetTouch(0).phase != TouchPhase.Ended) ||
                                        Input.touchCount > 1;

        [CanBeNull]
        public static GameObject ObjectUnderPointer
        {
            get
            {
                var pointerPos = ViewportPointerPosition;

                var ray = _mainCamera.ViewportPointToRay(pointerPos);

                var hit = Physics2D.Raycast(ray.origin, ray.direction);

                return hit ? hit.transform.gameObject : null;
            }
        }

        private static Vector2 GetWorldPointerPosition(int pointerNumber = 0)
        {
            var pointerPos = Vector2.zero;
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touchCount >= 1) pointerPos = Input.GetTouch(pointerNumber).position;
            }
            else
            {
                pointerPos = Input.mousePosition;
            }

            pointerPos = _mainCamera.ScreenToWorldPoint(pointerPos);

            return pointerPos;
        }

        // warning: returns positive infinity for no touches 
        public static Vector2 WorldPointerPosition => GetWorldPointerPosition();

        // warning: returns positive infinity for no touches
        private static Vector2 ScreenPointerPosition
        {
            get
            {
                var pointerPos = Vector2.positiveInfinity;
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    if (Input.touchCount >= 1) pointerPos = Input.GetTouch(0).position;
                }
                else
                {
                    pointerPos = Input.mousePosition;
                }

                return pointerPos;
            }
        }

        // warning: returns positive infinity for no touches
        public static Vector2 CenteredScreenPointerPosition
        {
            get
            {
                var pointerPos = ScreenPointerPosition;

                return pointerPos - new Vector2(Screen.width, Screen.height);
            }
        }

#if UNITY_EDITOR
        public static float TouchesAndPointersCount => Input.GetMouseButton(0) ? 1 : 0;
#else
    public static float TouchesAndPointersCount
    {
        get
        {
            return Input.touchCount;
        }
    }
#endif

        // warning: returns positive infinity for more touches than one or none
        private static Vector2 ViewportPointerPosition
        {
            get
            {
                var pointerPos = Vector2.positiveInfinity;
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    if (Input.touchCount == 1) pointerPos = Input.GetTouch(0).position;
                }
                else
                {
                    pointerPos = Input.mousePosition;
                }

                pointerPos = _mainCamera.ScreenToViewportPoint(pointerPos);

                return pointerPos;
            }
        }

        private static bool IsPointerOverUI
        {
            get
            {
                if (EventSystem.current.IsPointerOverGameObject()) return true;

                if (Input.touchCount <= 0) return false;
                var id = Input.touches[0].fingerId;
                return EventSystem.current.IsPointerOverGameObject(id);
            }
        }
    }
}