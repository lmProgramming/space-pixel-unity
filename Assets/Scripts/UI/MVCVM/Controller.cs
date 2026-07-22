using Core.MVCVM;
using Core.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UI.MVCVM
{
    [RequireComponent(typeof(PanelRenderer))]
    public abstract class Controller<TModel, TView, TViewModel> : MonoBehaviour
        where TModel : ObservableModel
        where TView : View<TViewModel>
    {
        [Inject] protected IGameUi GameUi;
        protected TModel Model { get; private set; }

        protected TView View { get; private set; }

        protected PanelRenderer PanelRenderer { get; private set; }

        protected virtual void Awake()
        {
            PanelRenderer = GetComponent<PanelRenderer>();
            if (PanelRenderer == null)
                throw new UnityException($"[{GetType().Name}] {nameof(PanelRenderer)} is required.");

            View = GetComponent<TView>();
            if (View == null)
                throw new UnityException(
                    $"[{GetType().Name}] {typeof(TView).Name} is required on the same GameObject.");

            Model = CreateModel();
        }

        protected virtual void OnEnable()
        {
            Model.Changed += Refresh;
            Refresh();
            Debug.Log($"[MVCVM] Opened {GetType().Name}", this);
        }

        protected virtual void OnDisable()
        {
            Debug.Log($"[MVCVM] Closed {GetType().Name}", this);
            Model.Changed -= Refresh;
        }

        protected abstract TModel CreateModel();

        protected void Refresh()
        {
            View.SetData(CreateViewModel(Model));
        }

        protected abstract TViewModel CreateViewModel(
            TModel model);

        public void Show()
        {
            View.Show();
        }

        public void Hide()
        {
            View.Hide();
        }
    }
}