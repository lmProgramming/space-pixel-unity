using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class PanelRendererBase : MonoBehaviour
    {
        private int _uiVersion = -1;

        protected bool IsUiBound { get; private set; }

        protected PanelRenderer PanelRenderer { get; private set; }

        protected virtual void Awake()
        {
            PanelRenderer = GetComponent<PanelRenderer>();
            if (PanelRenderer == null)
                throw new UnityException($"[{GetType().Name}] {nameof(PanelRenderer)} is required.");
        }

        protected virtual void OnEnable()
        {
            PanelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        protected virtual void OnDisable()
        {
            PanelRenderer.UnregisterUIReloadCallback(OnUIReload);
            UnbindUi();
        }

        protected virtual void OnUIReload(
            PanelRenderer renderer,
            VisualElement root,
            int version)
        {
            if (version == _uiVersion && IsUiBound)
                return;

            if (version != _uiVersion)
                UnbindUi();

            _uiVersion = version;
            BindUi(root);
        }

        private void BindUi(
            VisualElement root)
        {
            if (IsUiBound || root == null)
                return;

            BeforeBindUi(root);
            BindUiCore(root);
            AfterBindUi(root);
            IsUiBound = true;
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

        private void UnbindUi()
        {
            if (!IsUiBound)
                return;

            BeforeUnbindUi();
            UnbindUiCore();
            AfterUnbindUi();
            IsUiBound = false;
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