using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using billiard_management_system.Core;
using billiard_management_system.Helpers;
using billiard_management_system.Services;

namespace billiard_management_system.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private string _username = "admin";
        private string _password = string.Empty;
        private bool _rememberMe = true;
        private bool _isPasswordVisible = false;
        private bool _isCapsLockOn = false;

        public event Action LoginSucceeded;

        public LoginViewModel(IAuthService authService = null)
        {
            _authService = authService ?? new AuthService();
            Title = "Đăng nhập hệ thống Billiard";

            // Khôi phục tên đăng nhập đã ghi nhớ nếu có
            RememberMe = UserPreferences.RememberMe;
            if (RememberMe && !string.IsNullOrWhiteSpace(UserPreferences.SavedUsername))
            {
                Username = UserPreferences.SavedUsername;
            }

            LoginCommand = new RelayCommand<object>(ExecuteLoginAsync);
            TogglePasswordVisibilityCommand = new RelayCommand(ExecuteTogglePasswordVisibility);
            QuickFillAdminCommand = new RelayCommand(ExecuteQuickFillAdmin);
            QuickFillStaffCommand = new RelayCommand(ExecuteQuickFillStaff);
            ShowHelpCommand = new RelayCommand(ExecuteShowHelp);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                if (SetProperty(ref _isPasswordVisible, value))
                {
                    OnPropertyChanged(nameof(IsPasswordHidden));
                }
            }
        }

        public bool IsPasswordHidden => !_isPasswordVisible;

        public bool IsCapsLockOn
        {
            get => _isCapsLockOn;
            set => SetProperty(ref _isCapsLockOn, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }
        public ICommand QuickFillAdminCommand { get; }
        public ICommand QuickFillStaffCommand { get; }
        public ICommand ShowHelpCommand { get; }

        private void ExecuteTogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private void ExecuteQuickFillAdmin()
        {
            Username = "admin";
            Password = "Admin@123";
            ClearError();
        }

        private void ExecuteQuickFillStaff()
        {
            Username = "staff01";
            Password = "Admin@123";
            ClearError();
        }

        private void ExecuteShowHelp()
        {
            MessageBox.Show(
                "Để đảm bảo an toàn dữ liệu và doanh thu của quán, hệ thống không mở tính năng tự đặt lại mật khẩu trực tuyến.\n\n" +
                "Vui lòng liên hệ Chủ quán / Quản trị viên hệ thống hoặc Hotline kỹ thuật: 0901.234.567 để được hỗ trợ cấp lại mật khẩu.",
                "Trợ giúp Đăng nhập",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void ExecuteLoginAsync(object parameter)
        {
            if (IsBusy) return;

            string pwd = Password;
            if (parameter is PasswordBox pbox && !string.IsNullOrEmpty(pbox.Password))
            {
                pwd = pbox.Password;
            }

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(pwd))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return;
            }

            ClearError();
            IsBusy = true;

            try
            {
                var (success, message, user) = await _authService.LoginAsync(Username, pwd);
                if (success)
                {
                    // Lưu hoặc xóa thông tin ghi nhớ đăng nhập
                    UserPreferences.Save(Username.Trim(), RememberMe);

                    LoginSucceeded?.Invoke();
                }
                else
                {
                    ErrorMessage = message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi đăng nhập: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
