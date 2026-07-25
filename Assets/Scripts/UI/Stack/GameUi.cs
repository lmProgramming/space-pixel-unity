using System;
using System.Collections.Generic;
using Core.Constants;
using Core.UI;
using UI.Common;
using UI.Components.Notification;
using UI.Components.OptionsPopup;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UI.Stack
{
    public class GameUi : MonoBehaviour, IGameUi
    {
        private const int DefaultBaseSortingOrder = 100;
        private const int DefaultToastSortingOrder = 1000;

        [SerializeField] private Transform stackParent;
        [SerializeField] private Transform toastParent;
        [SerializeField] private int baseSortingOrder = DefaultBaseSortingOrder;
        [SerializeField] private int toastSortingOrder = DefaultToastSortingOrder;

        private readonly List<Component> _stack = new();
        private OptionsPopupController _activeOptionsPopup;

        [Inject] private DiContainer _container;

        private NotificationView _notificationHost;
        private Action<string> _pendingOptionsHandler;

        private void Awake()
        {
            if (!stackParent)
                throw new InvalidOperationException("[GameUi] Stack root is required.");

            if (!toastParent)
                throw new InvalidOperationException("[GameUi] Toast root is required.");

            if (_container == null)
                throw new InvalidOperationException("[GameUi] DiContainer is not injected.");
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            HandleEscape();
        }

        private void OnDestroy()
        {
            ClearOverlays();
            DestroyNotificationHost();
        }

        public int Depth => _stack.Count;

        public event Action DepthChanged;

        public void SetRoot(Component root)
        {
            if (!root)
                throw new ArgumentNullException(nameof(root));

            if (_stack.Count > 0)
                throw new InvalidOperationException("[GameUi] Root is already set.");

            _stack.Add(root);
            ShowComponent(root);
            DepthChanged?.Invoke();
        }

        public T PushById<T>(string panelId)
        {
            if (string.IsNullOrWhiteSpace(panelId))
                throw new ArgumentException("Panel id is required.", nameof(panelId));

            var prefab = _container.ResolveId<GameObject>(panelId);
            if (!prefab)
                throw new InvalidOperationException($"[GameUi] Prefab for id '{panelId}' resolved to null.");

            return Push<T>(prefab);
        }

        public T Push<T>(GameObject prefab)
        {
            if (!prefab)
                throw new ArgumentNullException(nameof(prefab));

            if (_stack.Count == 0)
                throw new InvalidOperationException("[GameUi] SetRoot must be called before Push.");

            var instance = _container.InstantiatePrefab(prefab, stackParent);
            var controller = instance.GetComponent<T>();
            if (controller is not Component component)
            {
                Destroy(instance);
                throw new InvalidOperationException("[GameUi] Prefab for id '" + prefab.name +
                                                    "' is not a component or is null.");
            }

            ApplySortingOrder(instance, baseSortingOrder + _stack.Count);
            ShowComponent(component);

            _stack.Add(component);
            DepthChanged?.Invoke();
            return controller;
        }

        public void Pop()
        {
            if (!TryPop())
                throw new InvalidOperationException("[GameUi] Cannot pop the unkillable root.");
        }

        public bool TryPop()
        {
            if (_stack.Count <= 1)
                return false;

            var top = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);

            Destroy(top.gameObject);

            DepthChanged?.Invoke();

            ShowComponent(_stack[^1]);

            return true;
        }

        public void ShowOptions(
            string title,
            string description,
            Action<string> optionSelected,
            params OptionsPopupOption[] options)
        {
            ClearPendingOptionsHandler();

            _activeOptionsPopup = PushById<OptionsPopupController>(UIPanelPrefabConstants.OptionsPopup);
            _pendingOptionsHandler = optionSelected;
            _activeOptionsPopup.OptionSelected += OnOptionsPopupOptionSelected;
            _activeOptionsPopup.Closed += OnOptionsPopupClosed;
            _activeOptionsPopup.Show(title, description, options);
        }

        public void Notify(string message, PopupLevel level = PopupLevel.Info)
        {
            EnsureNotificationHost().Show(message, level);
        }

        private void HandleEscape()
        {
            if (_stack.Count == 0)
                return;

            var top = _stack[^1];
            switch (GetActionOnEscape(top))
            {
                case ActionOnEscape.Pop:
                    if (_stack.Count > 1)
                        Pop();
                    break;
                case ActionOnEscape.PushPause:
                    PushById<PauseOverlayController>(UIPanelPrefabConstants.Pause);
                    break;
                case ActionOnEscape.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static ActionOnEscape GetActionOnEscape(Component panelComponent)
        {
            var panel = panelComponent as PanelRendererBase ?? panelComponent.GetComponent<PanelRendererBase>();
            return panel ? panel.ActionOnEscape : ActionOnEscape.Pop;
        }

        private static void ShowComponent(Component component)
        {
            if (component is IPanelRenderable renderable)
            {
                renderable.Show();
                return;
            }

            var panel = component.GetComponent<IPanelRenderable>();

            if (panel == null) throw new InvalidOperationException("[GameUI] component can't be shown");

            panel.Show();
        }

        private void OnOptionsPopupOptionSelected(string optionId)
        {
            _pendingOptionsHandler?.Invoke(optionId);
        }

        private void OnOptionsPopupClosed()
        {
            ClearPendingOptionsHandler();
        }

        private void ClearPendingOptionsHandler()
        {
            if (_activeOptionsPopup)
            {
                _activeOptionsPopup.OptionSelected -= OnOptionsPopupOptionSelected;
                _activeOptionsPopup.Closed -= OnOptionsPopupClosed;
                _activeOptionsPopup = null;
            }

            _pendingOptionsHandler = null;
        }

        private NotificationView EnsureNotificationHost()
        {
            if (_notificationHost)
                return _notificationHost;

            if (_container == null)
                throw new InvalidOperationException("[GameUi] DiContainer is not injected.");

            var prefab = _container.ResolveId<GameObject>(UIPanelPrefabConstants.NotificationHost);
            if (!prefab)
                throw new InvalidOperationException("[GameUi] NotificationHost prefab is missing.");

            var instance = _container.InstantiatePrefab(prefab, toastParent);
            ApplySortingOrder(instance, toastSortingOrder);

            _notificationHost = instance.GetComponent<NotificationView>();
            if (!_notificationHost)
            {
                Destroy(instance);
                throw new InvalidOperationException("[GameUi] NotificationHost prefab is missing NotificationView.");
            }

            _notificationHost.Show();
            return _notificationHost;
        }

        private void ClearOverlays()
        {
            while (_stack.Count > 1)
            {
                var top = _stack[^1];
                _stack.RemoveAt(_stack.Count - 1);
                if (!top)
                    continue;

                if (Application.isPlaying)
                    Destroy(top.gameObject);
                else
                    DestroyImmediate(top.gameObject);
            }
        }

        private void DestroyNotificationHost()
        {
            if (!_notificationHost)
                return;

            if (Application.isPlaying)
                Destroy(_notificationHost.gameObject);
            else
                DestroyImmediate(_notificationHost.gameObject);

            _notificationHost = null;
        }

        private static void ApplySortingOrder(GameObject instance, int sortingOrder)
        {
            var panelRenderer = instance.GetComponent<PanelRenderer>();
            if (panelRenderer)
                panelRenderer.sortingOrder = sortingOrder;
        }

#if UNITY_INCLUDE_TESTS
        internal void PushExistingForTesting(Component component)
        {
            if (!component)
                throw new ArgumentNullException(nameof(component));

            if (_stack.Count == 0)
                throw new InvalidOperationException("[GameUi] SetRoot must be called before Push.");

            _stack.Add(component);
            DepthChanged?.Invoke();
        }

        internal Component PeekForTesting()
        {
            return _stack.Count == 0 ? null : _stack[^1];
        }

        internal Component RootForTesting()
        {
            return _stack.Count == 0 ? null : _stack[0];
        }

        internal void HandleEscapeForTesting()
        {
            HandleEscape();
        }

        internal void SetRootParentsForTesting(Transform root)
        {
            stackParent = root;
            toastParent = root;
        }
#endif
    }
}