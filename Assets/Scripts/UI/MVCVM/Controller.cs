using Core.MVCVM;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.MVCVM
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class Controller<TModel, TView, TViewModel> : MonoBehaviour
        where TModel : ObservableModel
        where TView : View<TViewModel>
    {
        private PanelRendererLifecycle _lifecycle;

        protected TModel Model { get; private set; }

        protected TView View { get; private set; }

        protected virtual void Awake()
        {
            var panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer == null)
                throw new UnityException($"[{GetType().Name}] {nameof(PanelRenderer)} is required.");

            Model = CreateModel();
            View = CreateView();
            _lifecycle = new PanelRendererLifecycle(panelRenderer, View, this);
        }

        protected virtual void OnEnable()
        {
            Model.Changed += Refresh;
            _lifecycle.OnHostEnabled();
            Refresh();
            Debug.Log($"[MVCVM] Opened {GetType().Name}", this);
        }

        protected virtual void OnDisable()
        {
            Debug.Log($"[MVCVM] Closed {GetType().Name}", this);
            Model.Changed -= Refresh;
            _lifecycle.OnHostDisabled();
        }

        protected virtual void OnDestroy()
        {
            _lifecycle.Unregister();
        }

        protected abstract TModel CreateModel();

        protected abstract TView CreateView();

        protected void Refresh()
        {
            View.SetData(CreateViewModel(Model));
        }

        protected abstract TViewModel CreateViewModel(
            TModel model);
    }
}