using System;
using System.Threading.Tasks;
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

        public ICommand LoginCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }

        private void ExecuteTogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
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
