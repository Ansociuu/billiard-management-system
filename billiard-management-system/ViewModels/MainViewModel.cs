using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using billiard_management_system.Core;
using billiard_management_system.Helpers;
using billiard_management_system.Services;

namespace billiard_management_system.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private BaseViewModel _currentViewModel;
        private string _activeMenu = "Tables";
        private string _currentTimeString;
        private string _sessionDurationString = "00:00:00";
        private string _notificationMessage;
        private string _notificationType = "Info"; // "Info", "Success", "Error"
        private bool _isNotificationVisible;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _notificationTimer;

        public event Action LogoutRequested;

        public MainViewModel(IAuthService authService = null)
        {
            _authService = authService ?? new AuthService();
            Title = "Hệ Thống Quản Lý Quán Billiard Chuyên Nghiệp";

            // Khởi tạo các ViewModel con
            TableManagementVM = new TableManagementViewModel();
            ProductsVM = new MenuPlaceholderViewModel("Quản lý Dịch vụ", "FoodForkDrink", "Theo dõi danh mục sản phẩm, đồ uống, món ăn phục vụ tại quán.");
            InvoicesVM = new MenuPlaceholderViewModel("Hóa đơn & Tra cứu Thu ngân", "ReceiptTextOutline", "Xem lại lịch sử phiên chơi, chi tiết hóa đơn thanh toán và đối soát ca trực.");
            ReportsVM = new MenuPlaceholderViewModel("Báo cáo Doanh thu & Thống kê", "ChartBar", "Biểu đồ thống kê doanh số theo ngày, tuần, tháng và công suất khai thác bàn.");
            StaffVM = new MenuPlaceholderViewModel("Quản lý Nhân viên & Phân quyền", "AccountGroupOutline", "Danh sách nhân viên, thông tin tài khoản và cấu hình quyền truy cập hệ thống.");

            // Mặc định màn hình đầu tiên là Sơ đồ bàn
            CurrentViewModel = TableManagementVM;
            ActiveMenu = "Tables";

            // Khởi tạo Navigation Commands
            NavigateToTablesCommand = new RelayCommand(() =>
            {
                CurrentViewModel = TableManagementVM;
                ActiveMenu = "Tables";
            });

            NavigateToProductsCommand = new RelayCommand(() =>
            {
                CurrentViewModel = ProductsVM;
                ActiveMenu = "Products";
            });

            NavigateToInvoicesCommand = new RelayCommand(() =>
            {
                CurrentViewModel = InvoicesVM;
                ActiveMenu = "Invoices";
            });

            NavigateToReportsCommand = new RelayCommand(() =>
            {
                if (!CanAccessReports)
                {
                    ShowNotification("Chỉ Quản trị viên (Admin) mới có quyền truy cập Báo cáo!", "Error");
                    return;
                }
                CurrentViewModel = ReportsVM;
                ActiveMenu = "Reports";
            });

            NavigateToStaffCommand = new RelayCommand(() =>
            {
                if (!CanAccessStaff)
                {
                    ShowNotification("Chỉ Quản trị viên (Admin) mới có quyền Quản lý Nhân sự!", "Error");
                    return;
                }
                CurrentViewModel = StaffVM;
                ActiveMenu = "Staff";
            });

            DismissNotificationCommand = new RelayCommand(() => IsNotificationVisible = false);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            // Đồng hồ thời gian thực & theo dõi thời lượng phiên làm việc
            UpdateTime();
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) => UpdateTime();
            _clockTimer.Start();

            // Timer tự ẩn thông báo sau 4 giây
            _notificationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            _notificationTimer.Tick += (s, e) =>
            {
                IsNotificationVisible = false;
                _notificationTimer.Stop();
            };
        }

        public TableManagementViewModel TableManagementVM { get; }
        public MenuPlaceholderViewModel ProductsVM { get; }
        public MenuPlaceholderViewModel InvoicesVM { get; }
        public MenuPlaceholderViewModel ReportsVM { get; }
        public MenuPlaceholderViewModel StaffVM { get; }

        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string ActiveMenu
        {
            get => _activeMenu;
            set => SetProperty(ref _activeMenu, value);
        }

        #region User & Session Info

        public string CurrentUserFullName => UserSession.CurrentUser?.FullName ?? "Quản trị viên";
        public string CurrentUserRole => UserSession.CurrentUser?.RoleName ?? "Admin";
        public string CurrentUsername => UserSession.CurrentUser?.Username ?? string.Empty;

        public string LoginTimeFormatted => UserSession.LoginTime != default
            ? UserSession.LoginTime.ToString("HH:mm - dd/MM/yyyy")
            : "Chưa xác định";

        public string SessionDurationString
        {
            get => _sessionDurationString;
            set => SetProperty(ref _sessionDurationString, value);
        }

        public string CurrentTimeString
        {
            get => _currentTimeString;
            set => SetProperty(ref _currentTimeString, value);
        }

        #endregion

        #region Role-Based Access Control (RBAC) Permissions

        // Phân quyền hiển thị Menu theo RBAC
        public bool IsAdmin => UserSession.IsAdmin;
        public bool CanAccessReports => UserSession.HasPermission("REPORT_VIEW");
        public bool CanAccessStaff => UserSession.HasPermission("USER_MANAGE");
        public bool CanAccessProducts => UserSession.HasPermission("PRODUCT_MANAGE");
        public bool CanAccessInvoices => UserSession.HasPermission("INVOICE_PAY");

        #endregion

        #region Notifications

        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetProperty(ref _notificationMessage, value);
        }

        public string NotificationType
        {
            get => _notificationType;
            set => SetProperty(ref _notificationType, value);
        }

        public bool IsNotificationVisible
        {
            get => _isNotificationVisible;
            set => SetProperty(ref _isNotificationVisible, value);
        }

        public void ShowNotification(string message, string type = "Info")
        {
            NotificationMessage = message;
            NotificationType = type;
            IsNotificationVisible = true;

            _notificationTimer.Stop();
            _notificationTimer.Start();
        }

        #endregion

        #region Commands

        public ICommand NavigateToTablesCommand { get; }
        public ICommand NavigateToProductsCommand { get; }
        public ICommand NavigateToInvoicesCommand { get; }
        public ICommand NavigateToReportsCommand { get; }
        public ICommand NavigateToStaffCommand { get; }
        public ICommand DismissNotificationCommand { get; }
        public ICommand LogoutCommand { get; }

        #endregion

        private void UpdateTime()
        {
            CurrentTimeString = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            if (UserSession.IsLoggedIn)
            {
                var duration = UserSession.SessionDuration;
                SessionDurationString = string.Format("{0:00}:{1:00}:{2:00}", (int)duration.TotalHours, duration.Minutes, duration.Seconds);
            }
        }

        private async void ExecuteLogout()
        {
            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn đăng xuất khỏi tài khoản [{CurrentUserFullName}] không?",
                "Xác nhận đăng xuất",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _clockTimer.Stop();

                // Ghi vết đăng xuất vào AuditLogs và giải phóng phiên
                await _authService.LogoutAsync(UserSession.CurrentUser?.UserId);

                LogoutRequested?.Invoke();
            }
        }
    }
}
