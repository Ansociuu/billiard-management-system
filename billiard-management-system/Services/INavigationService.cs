namespace billiard_management_system.Services
{
    /// <summary>
    /// Service điều hướng chuyển đổi ViewModel trong ứng dụng WPF
    /// </summary>
    public interface INavigationService
    {
        object CurrentViewModel { get; }
        void NavigateTo<TViewModel>() where TViewModel : ViewModels.BaseViewModel;
        void NavigateTo(ViewModels.BaseViewModel viewModel);
    }
}
