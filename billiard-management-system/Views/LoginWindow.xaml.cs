using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using billiard_management_system.ViewModels;

namespace billiard_management_system.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
            DataContextChanged += LoginWindow_DataContextChanged;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Tự động thích ứng kích thước cửa sổ nếu độ phân giải màn hình hoặc tỷ lệ DPI Display Scale thấp
            double workHeight = SystemParameters.WorkArea.Height;
            if (workHeight > 0 && workHeight < Height + 40)
            {
                Height = Math.Max(480, workHeight - 30);
            }

            UpdateCapsLockState();
            ApplySmartFocus();
        }

        private void LoginWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LoginViewModel oldVm)
            {
                oldVm.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is LoginViewModel newVm)
            {
                newVm.PropertyChanged += ViewModel_PropertyChanged;
                UpdateCapsLockState();
                ApplySmartFocus();
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Khi đăng nhập thất bại (báo lỗi) -> Tự động focus lại vào ô mật khẩu để người dùng nhập lại ngay
            if (e.PropertyName == nameof(LoginViewModel.HasError))
            {
                if (DataContext is LoginViewModel vm && vm.HasError)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FocusPassword();
                    }), DispatcherPriority.Input);
                }
            }
        }

        /// <summary>
        /// Cho phép kéo thả cửa sổ khi click chuột trái vào thanh tiêu đề hoặc vùng nhận diện
        /// </summary>
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Nút Thu nhỏ cửa sổ xuống Taskbar
        /// </summary>
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Nút Đóng cửa sổ ứng dụng
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Bắt sự kiện phím gõ để cập nhật trạng thái phím Caps Lock theo thời gian thực
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            UpdateCapsLockState();
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            UpdateCapsLockState();
        }

        private void TxtInput_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateCapsLockState();
        }

        private void UpdateCapsLockState()
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.IsCapsLockOn = Keyboard.IsKeyToggled(Key.CapsLock);
            }
        }

        /// <summary>
        /// Tự động Focus thông minh:
        /// - Nếu đã có Tên đăng nhập (ghi nhớ từ phiên trước) -> Focus vào ô Mật khẩu.
        /// - Nếu chưa có Tên đăng nhập -> Focus vào ô Tên đăng nhập.
        /// </summary>
        public void ApplySmartFocus()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DataContext is LoginViewModel vm && !string.IsNullOrWhiteSpace(vm.Username))
                {
                    FocusPassword();
                }
                else
                {
                    txtUsername.Focus();
                    txtUsername.SelectAll();
                }
            }), DispatcherPriority.Input);
        }

        private void FocusPassword()
        {
            if (txtPassword.Visibility == Visibility.Visible)
            {
                txtPassword.Focus();
            }
            else if (txtVisiblePassword.Visibility == Visibility.Visible)
            {
                txtVisiblePassword.Focus();
                txtVisiblePassword.SelectAll();
            }
        }

        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Nếu ô mật khẩu đang trống -> Tự động chuyển focus xuống ô mật khẩu thay vì submit ngay
                bool passwordEmpty = string.IsNullOrEmpty(txtPassword.Password) && string.IsNullOrEmpty(txtVisiblePassword.Text);
                if (passwordEmpty)
                {
                    e.Handled = true;
                    FocusPassword();
                }
            }
        }

        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            // Duy trì vị trí con trỏ chuột khi chuyển đổi giữa PasswordBox và TextBox
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (txtVisiblePassword.Visibility == Visibility.Visible)
                {
                    txtVisiblePassword.Focus();
                    txtVisiblePassword.CaretIndex = txtVisiblePassword.Text.Length;
                }
                else if (txtPassword.Visibility == Visibility.Visible)
                {
                    txtPassword.Focus();
                }
            }), DispatcherPriority.Input);
        }
    }
}