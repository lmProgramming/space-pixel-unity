namespace UI.MVCVM
{
    public abstract class Controller<TModel, TView, TViewModel>
        where TModel : ObservableModel
        where TView : IView<TViewModel>
    {
        protected readonly TModel Model;
        protected readonly TView View;

        protected Controller(
            TModel model,
            TView view)
        {
            Model = model;
            View = view;

            Model.Changed += Refresh;

            Refresh();
        }

        private void Refresh()
        {
            var viewModel = CreateViewModel(Model);

            View.SetData(viewModel);
        }

        protected abstract TViewModel CreateViewModel(
            TModel model);

        public virtual void Dispose()
        {
            Model.Changed -= Refresh;
        }
    }
}