using System;
using System.Windows.Input;
using System.Windows.Threading;
using billiard_management_system.Core;
using billiard_management_system.Helpers;

namespace billiard_management_system.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentViewModel;
        private string _currentTimeString;
        private readonly DispatcherTimer _clockTimer;

        public event Action LogoutRequested;

        public MainViewModel()
        {
            Title = "Billiard Management System";

            // Khởi tạo các ViewModel con
            TableManagementVM = new TableManagementViewModel();
            CurrentViewModel = TableManagementVM;

            // Khởi tạo Commands
            NavigateToTablesCommand = new RelayCommand(() => CurrentViewModel = TableManagementVM);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Đồng hồ thời gian thực
            UpdateTime();
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => UpdateTime();
            _clockTimer.Start();
        }

        public TableManagementViewModel TableManagementVM { get; }

        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string CurrentUserFullName => UserSession.CurrentUser?.FullName ?? "Quản trị viên";
        public string CurrentUserRole => UserSession.CurrentUser?.RoleName ?? "Admin";

        public string CurrentTimeString
        {
            get => _currentTimeString;
            set => SetProperty(ref _currentTimeString, value);
        }

        public ICommand NavigateToTablesCommand { get; }
        public ICommand LogoutCommand { get; }

        private void UpdateTime()
        {
            CurrentTimeString = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void ExecuteLogout()
        {
            UserSession.ClearSession();
            LogoutRequested?.Invoke();
        }
    }
}

