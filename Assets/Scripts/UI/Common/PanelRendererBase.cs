using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Common
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class PanelRendererBase : MonoBehaviour, IPanelRenderable
    {
        private PanelRendererLifecycle _lifecycle;

        protected bool IsUiBound => _lifecycle.IsBound;

        protected PanelRenderer PanelRenderer { get; private set; }

        protected virtual void Awake()
        {
            PanelRenderer = GetComponent<PanelRenderer>();
            if (PanelRenderer == null)
                throw new UnityException($"[{GetType().Name}] {nameof(PanelRenderer)} is required.");

            _lifecycle = new PanelRendererLifecycle(PanelRenderer, this, this);
        }

        protected virtual void OnEnable()
        {
            _lifecycle.OnHostEnabled();
        }

        protected virtual void OnDisable()
        {
            _lifecycle.OnHostDisabled();
        }

        protected virtual void OnDestroy()
        {
            _lifecycle.Unregister();
        }

        void IPanelRenderable.BindUI(
            VisualElement root)
        {
            BeforeBindUi(root);
            BindUiCore(root);
            AfterBindUi(root);
        }

        void IPanelRenderable.UnbindUI()
        {
            BeforeUnbindUi();
            UnbindUiCore();
            AfterUnbindUi();
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
        }

        protected virtual void BeforeUnbindUi()
        {
        }

        protected abstract void UnbindUiCore();

        protected virtual void AfterUnbindUi()
        {
        }
    }
}