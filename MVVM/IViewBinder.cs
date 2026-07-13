namespace DevKit.UI.MVVM
{
    /// <summary>
    /// Encapsulates the logic of View/ViewModel creation/destroying and binding/unbinding.
    /// </summary>
    public interface IViewBinder<TViewModel> where TViewModel : IViewModel
    {
        public TViewModel Open();
        public void Close();
    }
}