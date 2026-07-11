namespace UI.MVCVM
{
    public interface IView<in TViewModel>
    {
        void SetData(TViewModel viewModel);
    }
}