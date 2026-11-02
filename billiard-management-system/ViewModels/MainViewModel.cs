using System.Windows.Input;
using billiard_management_system.Commands;
using billiard_management_system.Services;

namespace billiard_management_system.ViewModels
{
    /// <summary>
    /// ViewModel chính của cửa sổ MainWindow, quản lý điều hướng và trạng thái thanh điều hướng
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IAuthenticationService _authService;
        private string _pageTitle = "Trang chủ";

        public INavigationService NavigationService => _navigationService;
        public IAuthenticationService AuthService => _authService;

        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateTablesCommand { get; }
        public ICommand NavigateInvoicesCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel(INavigationService navigationService, IAuthenticationService authService)
        {
            _navigationService = navigationService;
            _authService = authService;

            NavigateDashboardCommand = new RelayCommand(() => PageTitle = "Tổng quan");
            NavigateTablesCommand = new RelayCommand(() => PageTitle = "Sơ đồ bàn bi-a");
            NavigateInvoicesCommand = new RelayCommand(() => PageTitle = "Tra cứu hóa đơn");
            NavigateReportsCommand = new RelayCommand(() => PageTitle = "Báo cáo doanh thu");
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        private void ExecuteLogout()
        {
            _authService?.Logout();
        }
    }
}
