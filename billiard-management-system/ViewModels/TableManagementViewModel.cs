using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using billiard_management_system.Core;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using billiard_management_system.Services;

namespace billiard_management_system.ViewModels
{
    public class TableManagementViewModel : BaseViewModel
    {
        private readonly ITableService _tableService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly DispatcherTimer _timer;

        private ObservableCollection<BilliardTable> _allTables = new ObservableCollection<BilliardTable>();
        private ObservableCollection<BilliardTable> _allTablesIncludingInactive = new ObservableCollection<BilliardTable>();
        private ObservableCollection<BilliardTable> _filteredTables = new ObservableCollection<BilliardTable>();
        private ObservableCollection<BilliardTable> _availableSwitchTargets = new ObservableCollection<BilliardTable>();
        private ObservableCollection<TableType> _tableTypes = new ObservableCollection<TableType>();
        private ObservableCollection<TableType> _allTableTypesIncludingInactive = new ObservableCollection<TableType>();
        private ObservableCollection<SessionProduct> _currentSessionProducts = new ObservableCollection<SessionProduct>();
        private ObservableCollection<Product> _availableProducts = new ObservableCollection<Product>();

        private BilliardTable _selectedTable;
        private BilliardTable _targetSwitchTable;
        private int? _selectedTypeId;
        private string _statusFilter = "All";

        // Current Billing stats for selected table
        private decimal _currentTableAmount;
        private decimal _currentServiceAmount;
        private decimal _currentTotalAmount;
        private TimeSpan _currentDuration;

        // Form Quản lý Bàn (CRUD Tables)
        private BilliardTable _selectedManageTable;
        private bool _isTableEditMode;
        private int _editingTableId;
        private string _formTableNumber = string.Empty;
        private int _formTableTypeId = 1;
        private string _formTableStatus = "Available";
        private bool _formTableIsActive = true;

        // Form Quản lý Loại Bàn (CRUD TableTypes)
        private TableType _selectedManageTableType;
        private bool _isTypeEditMode;
        private int _editingTableTypeId;
        private string _formTypeName = string.Empty;
        private string _formTypeDescription = string.Empty;
        private bool _formTypeIsActive = true;

        public event Action<string, string> NotificationRequested;

        public TableManagementViewModel(
            ITableService tableService = null,
            IProductService productService = null,
            IOrderService orderService = null)
        {
            _tableService = tableService ?? new TableService();
            _productService = productService ?? new ProductService();
            _orderService = orderService ?? new OrderService();

            Title = "Quản lý Bàn Chơi & Loại Bàn";

            // Khởi tạo các Commands vận hành bàn
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            SelectTableCommand = new RelayCommand<BilliardTable>(OnTableSelected);
            FilterTypeCommand = new RelayCommand<object>(OnFilterType);
            FilterStatusCommand = new RelayCommand<string>(OnFilterStatus);
            StartSessionCommand = new AsyncRelayCommand(OnStartSessionAsync, () => SelectedTable != null && SelectedTable.IsAvailable);
            SwitchTableCommand = new AsyncRelayCommand(OnSwitchTableAsync);
            SetMaintenanceCommand = new AsyncRelayCommand(() => OnChangeSelectedTableStatusAsync("Maintenance"));
            SetAvailableFromMaintenanceCommand = new AsyncRelayCommand(() => OnChangeSelectedTableStatusAsync("Available"));
            AddProductCommand = new RelayCommand<Product>(OnAddProductAsync);
            RemoveProductCommand = new RelayCommand<SessionProduct>(OnRemoveProductAsync);
            CheckoutCommand = new AsyncRelayCommand(OnCheckoutAsync, () => SelectedTable != null && SelectedTable.IsOccupied);

            // Commands CRUD Bàn (Tables)
            PrepareNewTableCommand = new RelayCommand(ExecutePrepareNewTable);
            SaveTableCommand = new AsyncRelayCommand(ExecuteSaveTableAsync);
            ToggleTableActiveCommand = new AsyncRelayCommand(ExecuteToggleTableActiveAsync);

            // Commands CRUD Loại bàn (TableTypes)
            PrepareNewTableTypeCommand = new RelayCommand(ExecutePrepareNewTableType);
            SaveTableTypeCommand = new AsyncRelayCommand(ExecuteSaveTableTypeAsync);

            // Timer cập nhật đồng hồ bàn chơi mỗi giây
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Load data ban đầu
            _ = LoadDataAsync();
        }

        #region Properties - Floor Plan & Session

        public ObservableCollection<BilliardTable> FilteredTables
        {
            get => _filteredTables;
            set => SetProperty(ref _filteredTables, value);
        }

        public ObservableCollection<BilliardTable> AllTablesIncludingInactive
        {
            get => _allTablesIncludingInactive;
            set => SetProperty(ref _allTablesIncludingInactive, value);
        }

        public ObservableCollection<BilliardTable> AvailableSwitchTargets
        {
            get => _availableSwitchTargets;
            set => SetProperty(ref _availableSwitchTargets, value);
        }

        public ObservableCollection<TableType> TableTypes
        {
            get => _tableTypes;
            set => SetProperty(ref _tableTypes, value);
        }

        public ObservableCollection<TableType> AllTableTypesIncludingInactive
        {
            get => _allTableTypesIncludingInactive;
            set => SetProperty(ref _allTableTypesIncludingInactive, value);
        }

        public ObservableCollection<SessionProduct> CurrentSessionProducts
        {
            get => _currentSessionProducts;
            set => SetProperty(ref _currentSessionProducts, value);
        }

        public ObservableCollection<Product> AvailableProducts
        {
            get => _availableProducts;
            set => SetProperty(ref _availableProducts, value);
        }

        public BilliardTable SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (SetProperty(ref _selectedTable, value))
                {
                    OnPropertyChanged(nameof(HasSelectedTable));
                    OnPropertyChanged(nameof(CanStartSession));
                    OnPropertyChanged(nameof(CanCheckout));
                    OnPropertyChanged(nameof(IsSelectedTableMaintenance));
                    UpdateSwitchTargets();
                    _ = OnSelectedTableChangedAsync();
                }
            }
        }

        public BilliardTable TargetSwitchTable
        {
            get => _targetSwitchTable;
            set => SetProperty(ref _targetSwitchTable, value);
        }

        public bool HasSelectedTable => _selectedTable != null;
        public bool CanStartSession => _selectedTable != null && _selectedTable.IsAvailable;
        public bool CanCheckout => _selectedTable != null && _selectedTable.IsOccupied;
        public bool IsSelectedTableMaintenance => _selectedTable != null && _selectedTable.IsMaintenance;
        public bool IsAdmin => UserSession.IsAdmin;

        // Quyền RBAC cho các thao tác trên bàn
        public bool HasOpenSessionPermission => UserSession.HasPermission(PermissionCodes.SessionOpen);
        public bool HasCheckoutPermission => UserSession.HasPermission(PermissionCodes.SessionClose) && UserSession.HasPermission(PermissionCodes.InvoicePay);

        public decimal CurrentTableAmount
        {
            get => _currentTableAmount;
            set => SetProperty(ref _currentTableAmount, value);
        }

        public decimal CurrentServiceAmount
        {
            get => _currentServiceAmount;
            set => SetProperty(ref _currentServiceAmount, value);
        }

        public decimal CurrentTotalAmount
        {
            get => _currentTotalAmount;
            set => SetProperty(ref _currentTotalAmount, value);
        }

        public TimeSpan CurrentDuration
        {
            get => _currentDuration;
            set => SetProperty(ref _currentDuration, value);
        }

        public int AvailableCount => _allTables.Count(t => t.IsAvailable);
        public int OccupiedCount => _allTables.Count(t => t.IsOccupied);
        public int MaintenanceCount => _allTables.Count(t => t.IsMaintenance);
        public int TotalCount => _allTables.Count;

        #endregion

        #region Properties - CRUD Quản lý Bàn & Loại bàn

        public List<string> AllowedTableStatuses { get; } = new List<string> { "Available", "Maintenance" };

        public BilliardTable SelectedManageTable
        {
            get => _selectedManageTable;
            set
            {
                if (SetProperty(ref _selectedManageTable, value) && value != null)
                {
                    _editingTableId = value.TableId;
                    FormTableNumber = value.TableNumber;
                    FormTableTypeId = value.TableTypeId;
                    FormTableStatus = value.Status;
                    FormTableIsActive = value.IsActive;
                    IsTableEditMode = true;
                    OnPropertyChanged(nameof(TableFormTitle));
                    OnPropertyChanged(nameof(ToggleTableActiveButtonText));
                }
            }
        }

        public bool IsTableEditMode
        {
            get => _isTableEditMode;
            set
            {
                if (SetProperty(ref _isTableEditMode, value))
                {
                    OnPropertyChanged(nameof(TableFormTitle));
                    OnPropertyChanged(nameof(TableSaveButtonText));
                }
            }
        }

        public string TableFormTitle => IsTableEditMode ? $"Chỉnh sửa Bàn: {FormTableNumber} (#{_editingTableId})" : "Thêm Bàn Billiard mới";
        public string TableSaveButtonText => IsTableEditMode ? "CẬP NHẬT BÀN" : "THÊM BÀN MỚI";
        public string ToggleTableActiveButtonText => FormTableIsActive ? "Ngừng sử dụng bàn" : "Kích hoạt lại bàn";

        public string FormTableNumber
        {
            get => _formTableNumber;
            set => SetProperty(ref _formTableNumber, value);
        }

        public int FormTableTypeId
        {
            get => _formTableTypeId;
            set => SetProperty(ref _formTableTypeId, value);
        }

        public string FormTableStatus
        {
            get => _formTableStatus;
            set => SetProperty(ref _formTableStatus, value);
        }

        public bool FormTableIsActive
        {
            get => _formTableIsActive;
            set
            {
                if (SetProperty(ref _formTableIsActive, value))
                {
                    OnPropertyChanged(nameof(ToggleTableActiveButtonText));
                }
            }
        }

        public TableType SelectedManageTableType
        {
            get => _selectedManageTableType;
            set
            {
                if (SetProperty(ref _selectedManageTableType, value) && value != null)
                {
                    _editingTableTypeId = value.TableTypeId;
                    FormTypeName = value.TypeName;
                    FormTypeDescription = value.Description ?? string.Empty;
                    FormTypeIsActive = value.IsActive;
                    IsTypeEditMode = true;
                    OnPropertyChanged(nameof(TypeFormTitle));
                }
            }
        }

        public bool IsTypeEditMode
        {
            get => _isTypeEditMode;
            set
            {
                if (SetProperty(ref _isTypeEditMode, value))
                {
                    OnPropertyChanged(nameof(TypeFormTitle));
                    OnPropertyChanged(nameof(TypeSaveButtonText));
                }
            }
        }

        public string TypeFormTitle => IsTypeEditMode ? $"Chỉnh sửa Loại bàn: #{_editingTableTypeId}" : "Thêm Loại Bàn mới";
        public string TypeSaveButtonText => IsTypeEditMode ? "CẬP NHẬT LOẠI BÀN" : "THÊM LOẠI BÀN";

        public string FormTypeName
        {
            get => _formTypeName;
            set => SetProperty(ref _formTypeName, value);
        }

        public string FormTypeDescription
        {
            get => _formTypeDescription;
            set => SetProperty(ref _formTypeDescription, value);
        }

        public bool FormTypeIsActive
        {
            get => _formTypeIsActive;
            set => SetProperty(ref _formTypeIsActive, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand SelectTableCommand { get; }
        public ICommand FilterTypeCommand { get; }
        public ICommand FilterStatusCommand { get; }
        public ICommand StartSessionCommand { get; }
        public ICommand SwitchTableCommand { get; }
        public ICommand SetMaintenanceCommand { get; }
        public ICommand SetAvailableFromMaintenanceCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand RemoveProductCommand { get; }
        public ICommand CheckoutCommand { get; }

        public ICommand PrepareNewTableCommand { get; }
        public ICommand SaveTableCommand { get; }
        public ICommand ToggleTableActiveCommand { get; }

        public ICommand PrepareNewTableTypeCommand { get; }
        public ICommand SaveTableTypeCommand { get; }

        #endregion

        #region Methods - Floor Plan & Session

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            ClearError();

            try
            {
                var activeTables = await _tableService.GetAllTablesAsync(includeInactive: false);
                _allTables = new ObservableCollection<BilliardTable>(activeTables);

                var allTables = await _tableService.GetAllTablesAsync(includeInactive: true);
                AllTablesIncludingInactive = new ObservableCollection<BilliardTable>(allTables);

                var activeTypes = await _tableService.GetTableTypesAsync(includeInactive: false);
                TableTypes = new ObservableCollection<TableType>(activeTypes);

                var allTypes = await _tableService.GetTableTypesAsync(includeInactive: true);
                AllTableTypesIncludingInactive = new ObservableCollection<TableType>(allTypes);

                var products = await _productService.GetProductsAsync();
                AvailableProducts = new ObservableCollection<Product>(products);

                ApplyFilter();
                UpdateSummaryStats();

                if (SelectedTable != null)
                {
                    SelectedTable = _allTables.FirstOrDefault(t => t.TableId == SelectedTable.TableId);
                }

                UpdateSwitchTargets();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu bàn chơi: {ex.Message}";
                Notify(ErrorMessage, "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateSwitchTargets()
        {
            if (SelectedTable == null)
            {
                AvailableSwitchTargets = new ObservableCollection<BilliardTable>();
                return;
            }

            var targets = _allTables.Where(t => t.IsAvailable && t.TableId != SelectedTable.TableId).ToList();
            AvailableSwitchTargets = new ObservableCollection<BilliardTable>(targets);
            TargetSwitchTable = targets.FirstOrDefault();
        }

        private void OnTableSelected(BilliardTable table)
        {
            SelectedTable = table;
        }

        private void OnFilterType(object param)
        {
            if (param == null || string.Equals(param.ToString(), "All", StringComparison.OrdinalIgnoreCase))
            {
                _selectedTypeId = null;
            }
            else if (int.TryParse(param.ToString(), out int id))
            {
                _selectedTypeId = id;
            }
            else
            {
                _selectedTypeId = null;
            }
            ApplyFilter();
        }

        private void OnFilterStatus(string status)
        {
            _statusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _allTables.AsEnumerable();

            if (_selectedTypeId.HasValue)
            {
                query = query.Where(t => t.TableTypeId == _selectedTypeId.Value);
            }

            if (_statusFilter != "All")
            {
                query = query.Where(t => t.Status.Equals(_statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            FilteredTables = new ObservableCollection<BilliardTable>(query);
        }

        private void UpdateSummaryStats()
        {
            OnPropertyChanged(nameof(AvailableCount));
            OnPropertyChanged(nameof(OccupiedCount));
            OnPropertyChanged(nameof(MaintenanceCount));
            OnPropertyChanged(nameof(TotalCount));
        }

        private async Task OnSelectedTableChangedAsync()
        {
            if (SelectedTable == null || !SelectedTable.IsOccupied || SelectedTable.CurrentSession == null)
            {
                CurrentSessionProducts.Clear();
                CurrentTableAmount = 0;
                CurrentServiceAmount = 0;
                CurrentTotalAmount = 0;
                CurrentDuration = TimeSpan.Zero;
                return;
            }

            try
            {
                var items = await _productService.GetSessionProductsAsync(SelectedTable.CurrentSession.SessionId);
                CurrentSessionProducts = new ObservableCollection<SessionProduct>(items);
                await UpdateSelectedTableBillingAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi cập nhật hóa đơn bàn: {ex.Message}";
            }
        }

        private async Task UpdateSelectedTableBillingAsync()
        {
            if (SelectedTable == null || SelectedTable.CurrentSession == null) return;

            var (tableAmt, srvAmt, totalAmt, dur) = await _orderService.CalculateSessionBillingAsync(SelectedTable.CurrentSession.SessionId);
            CurrentTableAmount = tableAmt;
            CurrentServiceAmount = srvAmt;
            CurrentTotalAmount = totalAmt;
            CurrentDuration = dur;
        }

        private async Task OnStartSessionAsync()
        {
            if (SelectedTable == null || !SelectedTable.IsAvailable) return;

            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Mở bàn chơi");
            if (!allowed)
            {
                ErrorMessage = deniedMsg;
                return;
            }

            int targetTableId = SelectedTable.TableId;
            long userId = UserSession.CurrentUser?.UserId ?? 1;
            var (success, msg, sessionId) = await _tableService.StartSessionAsync(targetTableId, userId);

            if (success)
            {
                Notify(msg, "Success");
                await LoadDataAsync();
                SelectedTable = _allTables.FirstOrDefault(t => t.TableId == targetTableId);
            }
            else
            {
                ErrorMessage = msg;
            }
        }

        private async Task OnSwitchTableAsync()
        {
            if (SelectedTable == null || !SelectedTable.IsOccupied || SelectedTable.CurrentSession == null) return;
            if (TargetSwitchTable == null)
            {
                ErrorMessage = "Vui lòng chọn bàn trống cần chuyển đến!";
                return;
            }

            int newTableId = TargetSwitchTable.TableId;
            var (success, msg) = await _tableService.SwitchTableAsync(
                SelectedTable.CurrentSession.SessionId, SelectedTable.TableId, newTableId);

            if (success)
            {
                Notify(msg, "Success");
                await LoadDataAsync();
                SelectedTable = _allTables.FirstOrDefault(t => t.TableId == newTableId);
            }
            else
            {
                ErrorMessage = msg;
            }
        }

        private async Task OnChangeSelectedTableStatusAsync(string newStatus)
        {
            if (SelectedTable == null) return;

            int targetTableId = SelectedTable.TableId;
            var (success, msg) = await _tableService.UpdateTableStatusAsync(targetTableId, newStatus);
            if (success)
            {
                Notify(msg, "Success");
                await LoadDataAsync();
                SelectedTable = _allTables.FirstOrDefault(t => t.TableId == targetTableId);
            }
            else
            {
                ErrorMessage = msg;
            }
        }

        private async void OnAddProductAsync(Product product)
        {
            if (product == null || SelectedTable == null || SelectedTable.CurrentSession == null) return;

            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Thêm dịch vụ vào bàn");
            if (!allowed)
            {
                ErrorMessage = deniedMsg;
                return;
            }

            var (success, msg) = await _productService.AddProductToSessionAsync(
                SelectedTable.CurrentSession.SessionId, product.ProductId, 1, product.Price);

            if (success)
            {
                var items = await _productService.GetSessionProductsAsync(SelectedTable.CurrentSession.SessionId);
                CurrentSessionProducts = new ObservableCollection<SessionProduct>(items);
                await UpdateSelectedTableBillingAsync();
            }
            else
            {
                ErrorMessage = msg;
            }
        }

        private async void OnRemoveProductAsync(SessionProduct sessionProduct)
        {
            if (sessionProduct == null || SelectedTable == null || SelectedTable.CurrentSession == null) return;

            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Xóa dịch vụ khỏi bàn");
            if (!allowed)
            {
                ErrorMessage = deniedMsg;
                return;
            }

            var (success, msg) = await _productService.RemoveProductFromSessionAsync(sessionProduct.SessionProductId);
            if (success)
            {
                CurrentSessionProducts.Remove(sessionProduct);
                await UpdateSelectedTableBillingAsync();
            }
            else
            {
                ErrorMessage = msg;
            }
        }

        private async Task OnCheckoutAsync()
        {
            if (SelectedTable == null || !SelectedTable.IsOccupied || SelectedTable.CurrentSession == null) return;

            var (canClose, closeDeniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionClose, "Kết thúc phiên chơi");
            if (!canClose)
            {
                ErrorMessage = closeDeniedMsg;
                return;
            }

            var (canPay, payDeniedMsg) = UserSession.CheckAccess(PermissionCodes.InvoicePay, "Thanh toán hóa đơn");
            if (!canPay)
            {
                ErrorMessage = payDeniedMsg;
                return;
            }

            int targetTableId = SelectedTable.TableId;
            long userId = UserSession.CurrentUser?.UserId ?? 1;
            var (success, invoiceCode, msg) = await _orderService.CheckoutAsync(
                SelectedTable.CurrentSession.SessionId, targetTableId, userId, 0, "Cash");

            if (success)
            {
                Notify(msg, "Success");
                await LoadDataAsync();
                SelectedTable = _allTables.FirstOrDefault(t => t.TableId == targetTableId);
            }
            else
            {
                ErrorMessage = msg;
            }
        }

        #endregion

        #region Methods - CRUD Bàn & Loại Bàn

        private void ExecutePrepareNewTable()
        {
            ClearError();
            _selectedManageTable = null;
            OnPropertyChanged(nameof(SelectedManageTable));

            _editingTableId = 0;
            FormTableNumber = string.Empty;
            FormTableTypeId = TableTypes.FirstOrDefault()?.TableTypeId ?? 1;
            FormTableStatus = "Available";
            FormTableIsActive = true;
            IsTableEditMode = false;
            OnPropertyChanged(nameof(TableFormTitle));
        }

        private async Task ExecuteSaveTableAsync()
        {
            if (IsBusy) return;
            ClearError();

            var model = new BilliardTable
            {
                TableId = _editingTableId,
                TableNumber = FormTableNumber,
                TableTypeId = FormTableTypeId,
                Status = FormTableStatus,
                IsActive = FormTableIsActive
            };

            IsBusy = true;
            try
            {
                if (IsTableEditMode)
                {
                    var (success, msg) = await _tableService.UpdateTableAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = msg;
                        Notify(msg, "Error");
                    }
                }
                else
                {
                    var (success, msg, newId) = await _tableService.CreateTableAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        ExecutePrepareNewTable();
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = msg;
                        Notify(msg, "Error");
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteToggleTableActiveAsync()
        {
            if (!IsTableEditMode || _editingTableId <= 0) return;

            bool targetActive = !FormTableIsActive;
            string actionDesc = targetActive ? "kích hoạt lại" : "ngừng sử dụng";

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn {actionDesc} bàn [{FormTableNumber}] không?",
                "Xác nhận thay đổi trạng thái bàn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var (success, msg) = await _tableService.ToggleTableActiveAsync(_editingTableId, targetActive);
                if (success)
                {
                    FormTableIsActive = targetActive;
                    Notify(msg, "Success");
                    await LoadDataAsync();
                }
                else
                {
                    ErrorMessage = msg;
                    Notify(msg, "Error");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecutePrepareNewTableType()
        {
            ClearError();
            _selectedManageTableType = null;
            OnPropertyChanged(nameof(SelectedManageTableType));

            _editingTableTypeId = 0;
            FormTypeName = string.Empty;
            FormTypeDescription = string.Empty;
            FormTypeIsActive = true;
            IsTypeEditMode = false;
            OnPropertyChanged(nameof(TypeFormTitle));
        }

        private async Task ExecuteSaveTableTypeAsync()
        {
            if (IsBusy) return;
            ClearError();

            var model = new TableType
            {
                TableTypeId = _editingTableTypeId,
                TypeName = FormTypeName,
                Description = FormTypeDescription,
                IsActive = FormTypeIsActive
            };

            IsBusy = true;
            try
            {
                if (IsTypeEditMode)
                {
                    var (success, msg) = await _tableService.UpdateTableTypeAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = msg;
                        Notify(msg, "Error");
                    }
                }
                else
                {
                    var (success, msg, newId) = await _tableService.CreateTableTypeAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        ExecutePrepareNewTableType();
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = msg;
                        Notify(msg, "Error");
                    }
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

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var table in _allTables)
            {
                if (table.IsOccupied)
                {
                    table.UpdateElapsed();
                }
            }

            if (SelectedTable != null && SelectedTable.IsOccupied && SelectedTable.CurrentSession != null)
            {
                CurrentDuration = DateTime.Now - SelectedTable.CurrentSession.StartTime;
                if (SelectedTable.HourlyRate > 0)
                {
                    CurrentTableAmount = Math.Round((decimal)CurrentDuration.TotalHours * SelectedTable.HourlyRate, 0);
                    CurrentTotalAmount = CurrentTableAmount + CurrentServiceAmount;
                }
            }
        }

        #endregion
    }
}
