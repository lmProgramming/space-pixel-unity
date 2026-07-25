using Core.UI;
using DesignSystem.Runtime;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UI.Common
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class PanelRendererBase : MonoBehaviour, IPanelRenderable
    {
        [SerializeField] private ActionOnEscape actionOnEscape = ActionOnEscape.Pop;

        [Inject] protected IGameUi GameUi;

        private PanelRendererLifecycle _lifecycle;
        private VisualElement Root => _lifecycle.Root;
        public bool IsOpen { get; private set; }

        public ActionOnEscape ActionOnEscape => actionOnEscape;

        protected PanelRenderer PanelRenderer { get; private set; }

        protected virtual void Awake()
        {
            PanelRenderer = GetComponent<PanelRenderer>();
            if (PanelRenderer == null)
                throw new UnityException($"[{GetType().Name}] {nameof(PanelRenderer)} is required.");

            _lifecycle = new PanelRendererLifecycle(PanelRenderer, this);
            IsOpen = false;
        }

        protected virtual void OnEnable()
        {
            _lifecycle.OnHostEnabled();
        }

        protected virtual void OnDisable()
        {
            _lifecycle.OnHostDisabled();
        }

        public void BindUI(
            VisualElement root)
        {
            ApplyVisibility();
            BeforeBindUi(root);
            BindUiCore(root);
            AfterBindUi(root);
        }

        public void UnbindUI()
        {
            BeforeUnbindUi();
            UnbindUiCore();
            AfterUnbindUi();
        }

        public virtual void Show()
        {
            IsOpen = true;
            DesignSystemRuntime.EnsureToggleKnobs(Root);
            ApplyVisibility();
        }

        public virtual void Hide()
        {
            IsOpen = false;
            ApplyVisibility();
        }

        public void Toggle()
        {
            if (IsOpen)
                Hide();
            else
                Show();
        }

        private void ApplyVisibility()
        {
            if (Root == null)
                return;

            Root.visible = IsOpen;
        }

        protected virtual void BeforeBindUi(
            VisualElement root)
        {
        }

        protected abstract void BindUiCore(
            VisualElement root);

        protected virtual void AfterBindUi(
            VisualElement root)
        {
            ApplyVisibility();
        }

        protected virtual void BeforeUnbindUi()
        {
        }

        protected virtual void UnbindUiCore()
        {
        }

        protected virtual void AfterUnbindUi()
        {
        }
    }
}