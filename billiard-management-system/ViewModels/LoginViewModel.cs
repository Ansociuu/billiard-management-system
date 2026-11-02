using System;
using System.Threading.Tasks;
using System.Windows.Input;
using billiard_management_system.Core;
using billiard_management_system.Services;

namespace billiard_management_system.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private string _username = "admin";
        private string _password = string.Empty;
        private bool _rememberMe = true;

        public event Action LoginSucceeded;

        public LoginViewModel(IAuthService authService = null)
        {
            _authService = authService ?? new AuthService();
            Title = "Đăng nhập hệ thống Billiard";
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
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

        public ICommand LoginCommand { get; }

        private bool CanExecuteLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && !IsBusy;
        }

        private async Task ExecuteLoginAsync()
        {
            ClearError();
            IsBusy = true;

            try
            {
                var (success, message, user) = await _authService.LoginAsync(Username, Password);
                if (success)
                {
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

