using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using billiard_management_system.Core;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using billiard_management_system.Services;

namespace billiard_management_system.ViewModels
{
    public class PermissionCheckItem : ObservableObject
    {
        private bool _isGranted;
        private bool _isEditable = true;

        public int PermissionId { get; set; }
        public string PermissionCode { get; set; }
        public string PermissionName { get; set; }
        public string Description { get; set; }

        public bool IsGranted
        {
            get => _isGranted;
            set => SetProperty(ref _isGranted, value);
        }

        public bool IsEditable
        {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }
    }

    public class StaffManagementViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        private ObservableCollection<User> _allUsers = new ObservableCollection<User>();
        private ObservableCollection<User> _filteredUsers = new ObservableCollection<User>();
        private ObservableCollection<Role> _roles = new ObservableCollection<Role>();
        private ObservableCollection<PermissionCheckItem> _rolePermissionItems = new ObservableCollection<PermissionCheckItem>();
        private List<Permission> _allPermissions = new List<Permission>();

        private User _selectedUser;
        private Role _selectedRoleForRbac;
        private string _searchKeyword = string.Empty;
        private string _roleFilter = "All"; // "All", "1", "2"
        private string _statusFilter = "All"; // "All", "Active", "Locked"

        // Form fields (Thêm mới / Chỉnh sửa)
        private bool _isEditMode;
        private long _editingUserId;
        private string _formUsername = string.Empty;
        private string _formPassword = string.Empty;
        private int _formRoleId = 2; // Mặc định Staff
        private string _formFullName = string.Empty;
        private string _formPhone = string.Empty;
        private string _formEmail = string.Empty;
        private string _formAddress = string.Empty;
        private string _formPosition = "Nhân viên Thu ngân";
        private DateTime? _formHireDate = DateTime.Today;
        private bool _formIsActive = true;

        public event Action<string, string> NotificationRequested;

        public StaffManagementViewModel(IUserService userService = null, IAuthService authService = null)
        {
            _userService = userService ?? new UserService();
            _authService = authService ?? new AuthService();

            Title = "Quản lý Tài khoản & Hồ sơ Nhân viên";

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            PrepareNewUserCommand = new RelayCommand(ExecutePrepareNewUser);
            SelectUserCommand = new RelayCommand<User>(OnSelectUser);
            FilterRoleCommand = new RelayCommand<string>(OnFilterRole);
            FilterStatusCommand = new RelayCommand<string>(OnFilterStatus);
            SaveUserCommand = new AsyncRelayCommand(ExecuteSaveUserAsync);
            ToggleActiveStatusCommand = new AsyncRelayCommand(ExecuteToggleActiveStatusAsync);
            DeleteUserCommand = new AsyncRelayCommand(ExecuteDeleteUserAsync);
            SaveRolePermissionsCommand = new AsyncRelayCommand(ExecuteSaveRolePermissionsAsync);

            _ = LoadDataAsync();
        }

        #region Properties

        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set => SetProperty(ref _filteredUsers, value);
        }

        public ObservableCollection<Role> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
        }

        public ObservableCollection<PermissionCheckItem> RolePermissionItems
        {
            get => _rolePermissionItems;
            set => SetProperty(ref _rolePermissionItems, value);
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value) && value != null)
                {
                    PopulateFormFromUser(value);
                }
            }
        }

        public Role SelectedRoleForRbac
        {
            get => _selectedRoleForRbac;
            set
            {
                if (SetProperty(ref _selectedRoleForRbac, value))
                {
                    OnPropertyChanged(nameof(CanEditSelectedRolePermissions));
                    _ = LoadPermissionsForSelectedRoleAsync();
                }
            }
        }

        public bool CanEditSelectedRolePermissions =>
            SelectedRoleForRbac != null &&
            SelectedRoleForRbac.RoleId != 1 &&
            UserSession.HasPermission(PermissionCodes.RoleManage);

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    ApplyFilter();
                }
            }
        }

        public int TotalStaffCount => _allUsers.Count;
        public int ActiveStaffCount => _allUsers.Count(u => u.IsActive);
        public int LockedStaffCount => _allUsers.Count(u => !u.IsActive);

        // Form Properties
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(IsCreateMode));
                    OnPropertyChanged(nameof(FormHeaderTitle));
                    OnPropertyChanged(nameof(PasswordHintText));
                    OnPropertyChanged(nameof(SaveButtonText));
                    OnPropertyChanged(nameof(ToggleStatusButtonText));
                }
            }
        }

        public bool IsCreateMode => !_isEditMode;
        public string FormHeaderTitle => IsEditMode ? $"Cập nhật Hồ sơ: #{_editingUserId} ({FormUsername})" : "Tạo Tài khoản & Hồ sơ Nhân viên mới";
        public string PasswordHintText => IsEditMode ? "Mật khẩu mới (để trống nếu không đổi)" : "Mật khẩu khởi tạo (* tối thiểu 6 ký tự)";
        public string SaveButtonText => IsEditMode ? "LƯU CẬP NHẬT HỒ SƠ" : "TẠO TÀI KHOẢN MỚI";
        public string ToggleStatusButtonText => FormIsActive ? "Khóa tài khoản" : "Mở khóa tài khoản";

        public string FormUsername
        {
            get => _formUsername;
            set => SetProperty(ref _formUsername, value);
        }

        public string FormPassword
        {
            get => _formPassword;
            set => SetProperty(ref _formPassword, value);
        }

        public int FormRoleId
        {
            get => _formRoleId;
            set => SetProperty(ref _formRoleId, value);
        }

        public string FormFullName
        {
            get => _formFullName;
            set => SetProperty(ref _formFullName, value);
        }

        public string FormPhone
        {
            get => _formPhone;
            set => SetProperty(ref _formPhone, value);
        }

        public string FormEmail
        {
            get => _formEmail;
            set => SetProperty(ref _formEmail, value);
        }

        public string FormAddress
        {
            get => _formAddress;
            set => SetProperty(ref _formAddress, value);
        }

        public string FormPosition
        {
            get => _formPosition;
            set => SetProperty(ref _formPosition, value);
        }

        public DateTime? FormHireDate
        {
            get => _formHireDate;
            set => SetProperty(ref _formHireDate, value);
        }

        public bool FormIsActive
        {
            get => _formIsActive;
            set
            {
                if (SetProperty(ref _formIsActive, value))
                {
                    OnPropertyChanged(nameof(ToggleStatusButtonText));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand PrepareNewUserCommand { get; }
        public ICommand SelectUserCommand { get; }
        public ICommand FilterRoleCommand { get; }
        public ICommand FilterStatusCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand ToggleActiveStatusCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand SaveRolePermissionsCommand { get; }

        #endregion

        #region Methods

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            ClearError();

            try
            {
                var users = await _userService.GetAllUsersWithProfileAsync();
                _allUsers = new ObservableCollection<User>(users);

                var roles = await _authService.GetAllRolesAsync();
                Roles = new ObservableCollection<Role>(roles);

                _allPermissions = await _authService.GetAllPermissionsAsync();

                ApplyFilter();
                UpdateSummaryCounts();

                if (SelectedRoleForRbac == null && Roles.Any())
                {
                    SelectedRoleForRbac = Roles.FirstOrDefault(r => r.RoleId == 2) ?? Roles.First();
                }
                else
                {
                    await LoadPermissionsForSelectedRoleAsync();
                }

                if (_editingUserId > 0)
                {
                    var match = _allUsers.FirstOrDefault(u => u.UserId == _editingUserId);
                    if (match != null)
                    {
                        SelectedUser = match;
                    }
                    else
                    {
                        ExecutePrepareNewUser();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải danh sách nhân viên: {ex.Message}";
                Notify(ErrorMessage, "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var query = _allUsers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim().ToLowerInvariant();
                query = query.Where(u =>
                    (u.Username != null && u.Username.ToLowerInvariant().Contains(kw)) ||
                    (u.FullName != null && u.FullName.ToLowerInvariant().Contains(kw)) ||
                    (u.Phone != null && u.Phone.ToLowerInvariant().Contains(kw)) ||
                    (u.Email != null && u.Email.ToLowerInvariant().Contains(kw)) ||
                    (u.PositionDisplay != null && u.PositionDisplay.ToLowerInvariant().Contains(kw)));
            }

            if (_roleFilter != "All" && int.TryParse(_roleFilter, out int roleId))
            {
                query = query.Where(u => u.RoleId == roleId);
            }

            if (_statusFilter == "Active")
            {
                query = query.Where(u => u.IsActive);
            }
            else if (_statusFilter == "Locked")
            {
                query = query.Where(u => !u.IsActive);
            }

            FilteredUsers = new ObservableCollection<User>(query);
        }

        private void UpdateSummaryCounts()
        {
            OnPropertyChanged(nameof(TotalStaffCount));
            OnPropertyChanged(nameof(ActiveStaffCount));
            OnPropertyChanged(nameof(LockedStaffCount));
        }

        private void OnFilterRole(string roleParam)
        {
            _roleFilter = string.IsNullOrWhiteSpace(roleParam) ? "All" : roleParam;
            ApplyFilter();
        }

        private void OnFilterStatus(string statusParam)
        {
            _statusFilter = string.IsNullOrWhiteSpace(statusParam) ? "All" : statusParam;
            ApplyFilter();
        }

        private void OnSelectUser(User user)
        {
            if (user == null) return;
            SelectedUser = user;
        }

        private void PopulateFormFromUser(User user)
        {
            ClearError();
            _editingUserId = user.UserId;
            FormUsername = user.Username;
            FormPassword = string.Empty;
            FormRoleId = user.RoleId;
            FormFullName = user.FullName;
            FormPhone = user.Phone ?? string.Empty;
            FormEmail = user.Email ?? string.Empty;
            FormAddress = user.Address ?? string.Empty;
            FormPosition = user.Position ?? (user.RoleId == 1 ? "Quản lý / Chủ quán" : "Nhân viên Thu ngân");
            FormHireDate = user.HireDate ?? user.CreatedAt.Date;
            FormIsActive = user.IsActive;
            IsEditMode = true;
            OnPropertyChanged(nameof(FormHeaderTitle));
        }

        private void ExecutePrepareNewUser()
        {
            ClearError();
            _selectedUser = null;
            OnPropertyChanged(nameof(SelectedUser));

            _editingUserId = 0;
            FormUsername = string.Empty;
            FormPassword = string.Empty;
            FormRoleId = 2;
            FormFullName = string.Empty;
            FormPhone = string.Empty;
            FormEmail = string.Empty;
            FormAddress = string.Empty;
            FormPosition = "Nhân viên Thu ngân";
            FormHireDate = DateTime.Today;
            FormIsActive = true;
            IsEditMode = false;
            OnPropertyChanged(nameof(FormHeaderTitle));
        }

        private async Task ExecuteSaveUserAsync()
        {
            if (IsBusy) return;
            ClearError();

            var userModel = new User
            {
                UserId = _editingUserId,
                Username = FormUsername,
                RoleId = FormRoleId,
                FullName = FormFullName,
                Phone = FormPhone,
                Email = FormEmail,
                Address = FormAddress,
                Position = FormPosition,
                HireDate = FormHireDate,
                IsActive = FormIsActive
            };

            IsBusy = true;
            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _userService.UpdateUserWithProfileAsync(userModel, FormPassword);
                    if (success)
                    {
                        FormPassword = string.Empty;
                        Notify(message, "Success");
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = message;
                        Notify(message, "Error");
                    }
                }
                else
                {
                    var (success, message, newUserId) = await _userService.CreateUserWithProfileAsync(userModel, FormPassword);
                    if (success)
                    {
                        _editingUserId = newUserId;
                        FormPassword = string.Empty;
                        Notify(message, "Success");
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = message;
                        Notify(message, "Error");
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteToggleActiveStatusAsync()
        {
            if (!IsEditMode || _editingUserId <= 0) return;

            bool targetStatus = !FormIsActive;
            string actionName = targetStatus ? "MỞ KHÓA (kích hoạt lại)" : "TẠM KHÓA (vô hiệu hóa)";

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn {actionName} tài khoản [{FormUsername} - {FormFullName}] không?",
                "Xác nhận thay đổi trạng thái tài khoản",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var (success, message) = await _userService.ToggleUserActiveStatusAsync(_editingUserId, targetStatus);
                if (success)
                {
                    FormIsActive = targetStatus;
                    Notify(message, "Success");
                    await LoadDataAsync();
                }
                else
                {
                    ErrorMessage = message;
                    Notify(message, "Error");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteDeleteUserAsync()
        {
            if (!IsEditMode || _editingUserId <= 0) return;

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa hoặc ngừng sử dụng tài khoản [{FormUsername} - {FormFullName}]?\n\nLưu ý: Nếu nhân viên đã phát sinh phiên chơi hoặc hóa đơn, hệ thống sẽ tự động chuyển sang trạng thái ĐÃ KHÓA để bảo toàn lịch sử dữ liệu.",
                "Xác nhận xóa / ngừng sử dụng tài khoản",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var (success, message) = await _userService.DeleteOrDeactivateUserAsync(_editingUserId);
                if (success)
                {
                    Notify(message, "Success");
                    ExecutePrepareNewUser();
                    await LoadDataAsync();
                }
                else
                {
                    ErrorMessage = message;
                    Notify(message, "Error");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadPermissionsForSelectedRoleAsync()
        {
            if (SelectedRoleForRbac == null || _allPermissions == null) return;

            try
            {
                var grantedIds = await _authService.GetRolePermissionIdsAsync(SelectedRoleForRbac.RoleId);
                bool isAdminRole = SelectedRoleForRbac.RoleId == 1;

                var list = _allPermissions.Select(p => new PermissionCheckItem
                {
                    PermissionId = p.PermissionId,
                    PermissionCode = p.PermissionCode,
                    PermissionName = p.PermissionName,
                    Description = p.Description,
                    IsGranted = isAdminRole || grantedIds.Contains(p.PermissionId),
                    IsEditable = !isAdminRole && UserSession.HasPermission(PermissionCodes.RoleManage)
                });

                RolePermissionItems = new ObservableCollection<PermissionCheckItem>(list);
            }
            catch (Exception ex)
            {
                Notify($"Lỗi tải cấu hình phân quyền: {ex.Message}", "Error");
            }
        }

        private async Task ExecuteSaveRolePermissionsAsync()
        {
            if (SelectedRoleForRbac == null) return;

            var selectedIds = RolePermissionItems
                .Where(p => p.IsGranted)
                .Select(p => p.PermissionId)
                .ToList();

            IsBusy = true;
            try
            {
                var (success, message) = await _authService.UpdateRolePermissionsAsync(SelectedRoleForRbac.RoleId, selectedIds);
                if (success)
                {
                    Notify(message, "Success");
                }
                else
                {
                    ErrorMessage = message;
                    Notify(message, "Error");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Notify(string message, string type = "Info")
        {
            NotificationRequested?.Invoke(message, type);
        }

        #endregion
    }
}
