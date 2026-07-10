using System.Collections.Generic;
using Core.Services;
using Events.Game;
using Events.UI;
using UnityEngine;
using Zenject;

namespace Services.GameInput
{
    public sealed class GameInput : MonoBehaviour, IGameInput
    {
        [SerializeField] private float maxTimeBetweenDoubleClicks = 0.5f;

        private readonly HashSet<object> _hoveredUiElements = new();
        private readonly HashSet<object> _focusedTextInputs = new();

        [Inject] private PointerOverUiEventChannel _pointerOverUiChannel;
        [Inject] private TextInputFocusEventChannel _textInputFocusChannel;
        [Inject] private PauseStateEventChannel _pauseStateChannel;

        private UnityEngine.Camera _mainCamera;

        private readonly float _simSpeed = 1f;
        private float _timeSinceLastLeftClick = 100f;
        private float _unscaledDeltaTime;

        private UnityEngine.Camera MainCamera
        {
            get
            {
                if (_mainCamera == null)
                    _mainCamera = UnityEngine.Camera.main;
                return _mainCamera;
            }
        }

        public float PressingTime { get; private set; }
        public bool LeftDoubleClick { get; private set; }
        public bool PressingAfterLeftDoubleClick { get; private set; }
        public float SimDeltaTime { get; private set; }
        public int HeldUiElementCount { get; private set; }

        public bool IsPointerOverUI => _hoveredUiElements.Count > 0;
        public bool IsTextInputFocused => _focusedTextInputs.Count > 0;
        public bool IsPaused { get; private set; }

        public bool CanControlShip => !IsPaused;
        public bool CanFireWeapons => !IsPaused && !IsPointerOverUI;

        private void OnEnable()
        {
            _pointerOverUiChannel.Register(OnPointerOverUiChanged);
            _textInputFocusChannel.Register(OnTextInputFocusChanged);
            _pauseStateChannel.Register(OnPauseStateChanged);
        }

        private void OnDisable()
        {
            _pointerOverUiChannel.Unregister(OnPointerOverUiChanged);
            _textInputFocusChannel.Unregister(OnTextInputFocusChanged);
            _pauseStateChannel.Unregister(OnPauseStateChanged);
            _hoveredUiElements.Clear();
            _focusedTextInputs.Clear();
        }

        private void Update()
        {
            SimDeltaTime = Time.deltaTime * _simSpeed;
            _unscaledDeltaTime = Time.unscaledDeltaTime;

            LeftDoubleClick = false;

            if (JustClicked)
            {
                if (_timeSinceLastLeftClick < maxTimeBetweenDoubleClicks)
                {
                    LeftDoubleClick = true;
                    PressingAfterLeftDoubleClick = true;

                    // after double click, we don't want our next click to also be a double click
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

        public void StartHoldingUIElement()
        {
            HeldUiElementCount++;
        }

        public void StopHoldingUIElement()
        {
            HeldUiElementCount--;
        }

        private void OnPauseStateChanged(bool paused)
        {
            IsPaused = paused;
        }

        private void OnPointerOverUiChanged(PointerOverUiData data)
        {
            if (data.Element == null)
                return;

            if (data.IsOver)
                _hoveredUiElements.Add(data.Element);
            else
                _hoveredUiElements.Remove(data.Element);
        }

        private void OnTextInputFocusChanged(TextInputFocusData data)
        {
            if (data.Source == null)
                return;

            if (data.IsFocused)
                _focusedTextInputs.Add(data.Source);
            else
                _focusedTextInputs.Remove(data.Source);
        }

        private static bool JustClicked => Input.GetMouseButtonDown(0) ||
                                           (Input.touchCount == 1 &&
                                            Input.GetTouch(0).phase == TouchPhase.Began);

        public bool JustClickedOutsideUI =>
            (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 &&
                                             Input.GetTouch(0).phase == TouchPhase.Began)) &&
            !IsPointerOverUI;

        private static bool JustStoppedClicking
        {
            get
            {
                var any = false;
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Ended) continue;
                    any = true;
                    break;
                }

                return any || Input.GetMouseButtonUp(0);
            }
        }

        public bool JustStoppedClickingOutsideUI
        {
            get
            {
                for (var index = 0; index < Input.touchCount; index++)
                {
                    var touch = Input.GetTouch(index);
                    if (touch.phase == TouchPhase.Ended && !IsPointerOverUI)
                        return true;
                }

                return Input.GetMouseButtonUp(0) && !IsPointerOverUI;
            }
        }

        private static bool Pressing => Input.GetMouseButton(0) ||
                                        (Input.touchCount == 1 &&
                                         Input.GetTouch(0).phase != TouchPhase.Ended) ||
                                        Input.touchCount > 1;

        public GameObject ObjectUnderPointer
        {
            get
            {
                var pointerPos = ViewportPointerPosition;

                var ray = MainCamera.ViewportPointToRay(pointerPos);

                var hit = Physics2D.Raycast(ray.origin, ray.direction);

                return hit ? hit.transform.gameObject : null;
            }
        }

        private Vector2 GetWorldPointerPosition(int pointerNumber = 0)
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

            pointerPos = MainCamera.ScreenToWorldPoint(pointerPos);

            return pointerPos;
        }

        // warning: returns positive infinity for no touches
        public Vector2 WorldPointerPosition => GetWorldPointerPosition();

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
        public Vector2 CenteredScreenPointerPosition =>
            ScreenPointerPosition - new Vector2(Screen.width, Screen.height);

#if UNITY_EDITOR
        public float TouchesAndPointersCount => Input.GetMouseButton(0) ? 1 : 0;
#else
        public float TouchesAndPointersCount => Input.touchCount;
#endif

        // warning: returns positive infinity for more touches than one or none
        private Vector2 ViewportPointerPosition
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

                pointerPos = MainCamera.ScreenToViewportPoint(pointerPos);

                return pointerPos;
            }
        }
    }
}