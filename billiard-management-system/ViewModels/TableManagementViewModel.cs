using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
        private readonly IPricingRuleService _pricingRuleService;
        private readonly DispatcherTimer _timer;

        private ObservableCollection<BilliardTable> _allTables = new ObservableCollection<BilliardTable>();
        private ObservableCollection<BilliardTable> _allTablesIncludingInactive = new ObservableCollection<BilliardTable>();
        private ObservableCollection<BilliardTable> _filteredTables = new ObservableCollection<BilliardTable>();
        private ObservableCollection<BilliardTable> _availableSwitchTargets = new ObservableCollection<BilliardTable>();
        private ObservableCollection<TableType> _tableTypes = new ObservableCollection<TableType>();
        private ObservableCollection<TableType> _allTableTypesIncludingInactive = new ObservableCollection<TableType>();
        private ObservableCollection<SessionProduct> _currentSessionProducts = new ObservableCollection<SessionProduct>();
        private ObservableCollection<SessionPricingDetail> _currentPricingSegments = new ObservableCollection<SessionPricingDetail>();
        private ObservableCollection<Product> _availableProducts = new ObservableCollection<Product>();

        private BilliardTable _selectedTable;
        private BilliardTable _targetSwitchTable;
        private int? _selectedTypeId;
        private string _statusFilter = "All";

        // Thông tin khách hàng khi mở phiên chơi mới (Issue #16)
        private string _openCustomerName = string.Empty;
        private string _openCustomerPhone = string.Empty;

        // Chọn sản phẩm & nhập số lượng vào phiên chơi (Issue #17)
        private Product _selectedOrderProduct;
        private string _orderProductQuantityText = "1";

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

        // Quản lý Quy tắc giá bàn & Khuyến mãi (CRUD TablePricingRules - Issue #15)
        private ObservableCollection<TablePricingRule> _allPricingRules = new ObservableCollection<TablePricingRule>();
        private ObservableCollection<TablePricingRule> _filteredPricingRules = new ObservableCollection<TablePricingRule>();
        private string _ruleTypeFilter = "All";
        private string _ruleStatusFilter = "All";

        private TablePricingRule _selectedPricingRule;
        private bool _isRuleEditMode;
        private long _editingPricingRuleId;
        private string _formRuleName = string.Empty;
        private int _formRuleTableTypeId = 1;
        private string _formRuleType = "BaseRate";
        private string _formRuleDayType = "Weekday";
        private bool _formRuleIsAllDay = true;
        private string _formRuleStartTimeText = "08:00";
        private string _formRuleEndTimeText = "17:00";
        private string _formRuleHourlyRateText = "50000";
        private string _formRuleDiscountType = "Percentage";
        private string _formRuleDiscountValueText = "10";
        private DateTime _formRuleEffectiveFrom = DateTime.Today;
        private bool _formRuleHasEndDate;
        private DateTime? _formRuleEffectiveTo;
        private string _formRulePriorityText = "1";
        private bool _formRuleIsActive = true;

        public event Action<string, string> NotificationRequested;

        public TableManagementViewModel(
            ITableService tableService = null,
            IProductService productService = null,
            IOrderService orderService = null,
            IPricingRuleService pricingRuleService = null)
        {
            _tableService = tableService ?? new TableService();
            _productService = productService ?? new ProductService();
            _orderService = orderService ?? new OrderService();
            _pricingRuleService = pricingRuleService ?? new PricingRuleService();

            Title = "Quản lý Bàn Chơi, Loại Bàn & Quy tắc Giá";

            // Khởi tạo các Commands vận hành bàn
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            SelectTableCommand = new RelayCommand<BilliardTable>(OnTableSelected);
            FilterTypeCommand = new RelayCommand<object>(OnFilterType);
            FilterStatusCommand = new RelayCommand<string>(OnFilterStatus);
            StartSessionCommand = new AsyncRelayCommand(OnStartSessionAsync, () => SelectedTable != null && SelectedTable.IsAvailable);
            CancelSessionCommand = new AsyncRelayCommand(OnCancelSessionAsync, () => SelectedTable != null && SelectedTable.IsOccupied);
            SwitchTableCommand = new AsyncRelayCommand(OnSwitchTableAsync);
            SetMaintenanceCommand = new AsyncRelayCommand(() => OnChangeSelectedTableStatusAsync("Maintenance"));
            SetAvailableFromMaintenanceCommand = new AsyncRelayCommand(() => OnChangeSelectedTableStatusAsync("Available"));

            // Commands gọi món trong phiên (Issue #17)
            AddProductCommand = new RelayCommand<Product>(OnAddProductAsync);
            AddSelectedProductWithQuantityCommand = new AsyncRelayCommand(OnAddSelectedProductWithQuantityAsync);
            IncreaseProductQuantityCommand = new RelayCommand<SessionProduct>(OnIncreaseProductQuantityAsync);
            DecreaseProductQuantityCommand = new RelayCommand<SessionProduct>(OnDecreaseProductQuantityAsync);
            RemoveProductCommand = new RelayCommand<SessionProduct>(OnRemoveProductAsync);

            CheckoutCommand = new AsyncRelayCommand(OnCheckoutAsync, () => SelectedTable != null && SelectedTable.IsOccupied);

            // Commands CRUD Bàn (Tables)
            PrepareNewTableCommand = new RelayCommand(ExecutePrepareNewTable);
            SaveTableCommand = new AsyncRelayCommand(ExecuteSaveTableAsync);
            ToggleTableActiveCommand = new AsyncRelayCommand(ExecuteToggleTableActiveAsync);

            // Commands CRUD Loại bàn (TableTypes)
            PrepareNewTableTypeCommand = new RelayCommand(ExecutePrepareNewTableType);
            SaveTableTypeCommand = new AsyncRelayCommand(ExecuteSaveTableTypeAsync);

            // Commands CRUD Quy tắc giá & Khuyến mãi (TablePricingRules - Issue #15)
            FilterRuleTypeCommand = new RelayCommand<string>(OnFilterRuleType);
            FilterRuleStatusCommand = new RelayCommand<string>(OnFilterRuleStatus);
            PrepareNewPricingRuleCommand = new RelayCommand(ExecutePrepareNewPricingRule);
            SavePricingRuleCommand = new AsyncRelayCommand(ExecuteSavePricingRuleAsync);
            TogglePricingRuleActiveCommand = new AsyncRelayCommand(ExecuteTogglePricingRuleActiveAsync);

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

        public ObservableCollection<SessionPricingDetail> CurrentPricingSegments
        {
            get => _currentPricingSegments;
            set => SetProperty(ref _currentPricingSegments, value);
        }

        public ObservableCollection<Product> AvailableProducts
        {
            get => _availableProducts;
            set => SetProperty(ref _availableProducts, value);
        }

        public Product SelectedOrderProduct
        {
            get => _selectedOrderProduct;
            set => SetProperty(ref _selectedOrderProduct, value);
        }

        public string OrderProductQuantityText
        {
            get => _orderProductQuantityText;
            set => SetProperty(ref _orderProductQuantityText, value);
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

        public string OpenCustomerName
        {
            get => _openCustomerName;
            set => SetProperty(ref _openCustomerName, value);
        }

        public string OpenCustomerPhone
        {
            get => _openCustomerPhone;
            set => SetProperty(ref _openCustomerPhone, value);
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

        #region Properties - CRUD Quy tắc Giá bàn & Khuyến mãi (Issue #15)

        public List<string> AllowedRuleTypes { get; } = new List<string> { "BaseRate", "Promotion" };
        public List<string> AllowedDayTypes { get; } = new List<string> { "Weekday", "Weekend", "Holiday", "Any" };
        public List<string> AllowedDiscountTypes { get; } = new List<string> { "Percentage", "FixedAmount" };

        public ObservableCollection<TablePricingRule> FilteredPricingRules
        {
            get => _filteredPricingRules;
            set => SetProperty(ref _filteredPricingRules, value);
        }

        public int ActiveBaseRulesCount => _allPricingRules.Count(r => r.IsActive && !r.IsPromotion);
        public int ActivePromotionsCount => _allPricingRules.Count(r => r.IsActive && r.IsPromotion);
        public int InactiveRulesCount => _allPricingRules.Count(r => !r.IsActive);

        public TablePricingRule SelectedPricingRule
        {
            get => _selectedPricingRule;
            set
            {
                if (SetProperty(ref _selectedPricingRule, value) && value != null)
                {
                    ClearError();
                    _editingPricingRuleId = value.PricingRuleId;
                    FormRuleName = value.RuleName;
                    FormRuleTableTypeId = value.TableTypeId;
                    FormRuleType = value.RuleType;
                    FormRuleDayType = value.DayType;

                    if (value.StartTime.HasValue && value.EndTime.HasValue)
                    {
                        FormRuleIsAllDay = false;
                        FormRuleStartTimeText = value.StartTime.Value.ToString(@"hh\:mm");
                        FormRuleEndTimeText = value.EndTime.Value.ToString(@"hh\:mm");
                    }
                    else
                    {
                        FormRuleIsAllDay = true;
                        FormRuleStartTimeText = "08:00";
                        FormRuleEndTimeText = "17:00";
                    }

                    FormRuleHourlyRateText = (value.HourlyRate ?? 50000m).ToString("0", CultureInfo.InvariantCulture);
                    FormRuleDiscountType = string.IsNullOrWhiteSpace(value.DiscountType) ? "Percentage" : value.DiscountType;
                    FormRuleDiscountValueText = (value.DiscountValue ?? 10m).ToString("0.##", CultureInfo.InvariantCulture);
                    FormRuleEffectiveFrom = value.EffectiveFrom;
                    FormRuleHasEndDate = value.EffectiveTo.HasValue;
                    FormRuleEffectiveTo = value.EffectiveTo ?? DateTime.Today.AddMonths(1);
                    FormRulePriorityText = value.Priority.ToString(CultureInfo.InvariantCulture);
                    FormRuleIsActive = value.IsActive;

                    IsRuleEditMode = true;
                    OnPropertyChanged(nameof(RuleFormTitle));
                    OnPropertyChanged(nameof(ToggleRuleActiveButtonText));
                }
            }
        }

        public bool IsRuleEditMode
        {
            get => _isRuleEditMode;
            set
            {
                if (SetProperty(ref _isRuleEditMode, value))
                {
                    OnPropertyChanged(nameof(RuleFormTitle));
                    OnPropertyChanged(nameof(RuleSaveButtonText));
                }
            }
        }

        public string RuleFormTitle => IsRuleEditMode ? $"Chỉnh sửa Quy tắc: #{_editingPricingRuleId}" : "Tạo Quy tắc Giá / Khuyến mãi mới";
        public string RuleSaveButtonText => IsRuleEditMode ? "CẬP NHẬT QUY TẮC" : "THÊM QUY TẮC MỚI";
        public string ToggleRuleActiveButtonText => FormRuleIsActive ? "Tạm ngưng áp dụng (Không tính tiền)" : "Kích hoạt áp dụng lại";

        public string FormRuleName
        {
            get => _formRuleName;
            set => SetProperty(ref _formRuleName, value);
        }

        public int FormRuleTableTypeId
        {
            get => _formRuleTableTypeId;
            set => SetProperty(ref _formRuleTableTypeId, value);
        }

        public string FormRuleType
        {
            get => _formRuleType;
            set
            {
                if (SetProperty(ref _formRuleType, value))
                {
                    OnPropertyChanged(nameof(IsBaseRateRule));
                    OnPropertyChanged(nameof(IsPromotionRule));
                }
            }
        }

        public bool IsBaseRateRule => string.Equals(_formRuleType, "BaseRate", StringComparison.OrdinalIgnoreCase);
        public bool IsPromotionRule => string.Equals(_formRuleType, "Promotion", StringComparison.OrdinalIgnoreCase);

        public string FormRuleDayType
        {
            get => _formRuleDayType;
            set => SetProperty(ref _formRuleDayType, value);
        }

        public bool FormRuleIsAllDay
        {
            get => _formRuleIsAllDay;
            set
            {
                if (SetProperty(ref _formRuleIsAllDay, value))
                {
                    OnPropertyChanged(nameof(IsCustomTimeSlot));
                }
            }
        }

        public bool IsCustomTimeSlot => !_formRuleIsAllDay;

        public string FormRuleStartTimeText
        {
            get => _formRuleStartTimeText;
            set => SetProperty(ref _formRuleStartTimeText, value);
        }

        public string FormRuleEndTimeText
        {
            get => _formRuleEndTimeText;
            set => SetProperty(ref _formRuleEndTimeText, value);
        }

        public string FormRuleHourlyRateText
        {
            get => _formRuleHourlyRateText;
            set => SetProperty(ref _formRuleHourlyRateText, value);
        }

        public string FormRuleDiscountType
        {
            get => _formRuleDiscountType;
            set
            {
                if (SetProperty(ref _formRuleDiscountType, value))
                {
                    OnPropertyChanged(nameof(DiscountValueHint));
                }
            }
        }

        public string DiscountValueHint => string.Equals(_formRuleDiscountType, "Percentage", StringComparison.OrdinalIgnoreCase)
            ? "Mức giảm phần trăm (1 - 100%) *"
            : "Số tiền giảm cố định (VNĐ/giờ > 0) *";

        public string FormRuleDiscountValueText
        {
            get => _formRuleDiscountValueText;
            set => SetProperty(ref _formRuleDiscountValueText, value);
        }

        public DateTime FormRuleEffectiveFrom
        {
            get => _formRuleEffectiveFrom;
            set => SetProperty(ref _formRuleEffectiveFrom, value);
        }

        public bool FormRuleHasEndDate
        {
            get => _formRuleHasEndDate;
            set
            {
                if (SetProperty(ref _formRuleHasEndDate, value))
                {
                    if (value && !_formRuleEffectiveTo.HasValue)
                    {
                        FormRuleEffectiveTo = _formRuleEffectiveFrom.AddMonths(1);
                    }
                }
            }
        }

        public DateTime? FormRuleEffectiveTo
        {
            get => _formRuleEffectiveTo;
            set => SetProperty(ref _formRuleEffectiveTo, value);
        }

        public string FormRulePriorityText
        {
            get => _formRulePriorityText;
            set => SetProperty(ref _formRulePriorityText, value);
        }

        public bool FormRuleIsActive
        {
            get => _formRuleIsActive;
            set
            {
                if (SetProperty(ref _formRuleIsActive, value))
                {
                    OnPropertyChanged(nameof(ToggleRuleActiveButtonText));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand SelectTableCommand { get; }
        public ICommand FilterTypeCommand { get; }
        public ICommand FilterStatusCommand { get; }
        public ICommand StartSessionCommand { get; }
        public ICommand CancelSessionCommand { get; }
        public ICommand SwitchTableCommand { get; }
        public ICommand SetMaintenanceCommand { get; }
        public ICommand SetAvailableFromMaintenanceCommand { get; }

        public ICommand AddProductCommand { get; }
        public ICommand AddSelectedProductWithQuantityCommand { get; }
        public ICommand IncreaseProductQuantityCommand { get; }
        public ICommand DecreaseProductQuantityCommand { get; }
        public ICommand RemoveProductCommand { get; }

        public ICommand CheckoutCommand { get; }

        public ICommand PrepareNewTableCommand { get; }
        public ICommand SaveTableCommand { get; }
        public ICommand ToggleTableActiveCommand { get; }

        public ICommand PrepareNewTableTypeCommand { get; }
        public ICommand SaveTableTypeCommand { get; }

        public ICommand FilterRuleTypeCommand { get; }
        public ICommand FilterRuleStatusCommand { get; }
        public ICommand PrepareNewPricingRuleCommand { get; }
        public ICommand SavePricingRuleCommand { get; }
        public ICommand TogglePricingRuleActiveCommand { get; }

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
                if (SelectedOrderProduct == null || !AvailableProducts.Any(p => p.ProductId == SelectedOrderProduct.ProductId))
                {
                    SelectedOrderProduct = AvailableProducts.FirstOrDefault();
                }

                var rules = await _pricingRuleService.GetPricingRulesAsync(includeInactive: true);
                _allPricingRules = new ObservableCollection<TablePricingRule>(rules);
                ApplyPricingRuleFilter();
                OnPropertyChanged(nameof(ActiveBaseRulesCount));
                OnPropertyChanged(nameof(ActivePromotionsCount));
                OnPropertyChanged(nameof(InactiveRulesCount));

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
                CurrentPricingSegments.Clear();
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

            var (tableAmt, srvAmt, totalAmt, dur, segments) =
                await _orderService.CalculateSessionBillingDetailedAsync(SelectedTable.CurrentSession.SessionId);
            CurrentTableAmount = tableAmt;
            CurrentServiceAmount = srvAmt;
            CurrentTotalAmount = totalAmt;
            CurrentDuration = dur;
            CurrentPricingSegments = new ObservableCollection<SessionPricingDetail>(segments ?? new List<SessionPricingDetail>());
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
            var (success, msg, sessionId) = await _tableService.StartSessionAsync(
                targetTableId, userId, null, OpenCustomerName, OpenCustomerPhone);

            if (success)
            {
                OpenCustomerName = string.Empty;
                OpenCustomerPhone = string.Empty;
                Notify(msg, "Success");
                await LoadDataAsync();
                SelectedTable = _allTables.FirstOrDefault(t => t.TableId == targetTableId);
            }
            else
            {
                ErrorMessage = msg;
                Notify(msg, "Error");
            }
        }

        private async Task OnCancelSessionAsync()
        {
            if (SelectedTable == null || !SelectedTable.IsOccupied || SelectedTable.CurrentSession == null) return;

            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionClose, "Hủy / kết thúc phiên chơi");
            if (!allowed)
            {
                ErrorMessage = deniedMsg;
                Notify(deniedMsg, "Error");
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn hủy phiên chơi #{SelectedTable.CurrentSession.SessionId} của bàn [{SelectedTable.TableNumber}] và trả bàn về trạng thái Trống không?",
                "Xác nhận hủy phiên chơi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            int targetTableId = SelectedTable.TableId;
            long sessionId = SelectedTable.CurrentSession.SessionId;
            var (success, msg) = await _tableService.EndSessionAsync(sessionId, targetTableId, 0m, isCancelled: true);

            if (success)
            {
                Notify(msg, "Success");
                await LoadDataAsync();
                SelectedTable = _allTables.FirstOrDefault(t => t.TableId == targetTableId);
            }
            else
            {
                ErrorMessage = msg;
                Notify(msg, "Error");
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
            await AddProductToCurrentSessionInternalAsync(product, 1);
        }

        private async Task OnAddSelectedProductWithQuantityAsync()
        {
            if (SelectedTable == null || !SelectedTable.IsOccupied || SelectedTable.CurrentSession == null)
            {
                ErrorMessage = "Chỉ có thể thêm sản phẩm vào bàn đang chơi (Active)!";
                return;
            }

            if (SelectedOrderProduct == null)
            {
                ErrorMessage = "Vui lòng chọn sản phẩm cần thêm vào bàn!";
                return;
            }

            if (!int.TryParse(OrderProductQuantityText?.Trim(), out int qty) || qty <= 0)
            {
                ErrorMessage = "Số lượng sản phẩm phải là số nguyên lớn hơn 0!";
                Notify(ErrorMessage, "Error");
                return;
            }

            await AddProductToCurrentSessionInternalAsync(SelectedOrderProduct, qty);
            OrderProductQuantityText = "1";
        }

        private async Task AddProductToCurrentSessionInternalAsync(Product product, int quantity)
        {
            ClearError();

            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Thêm dịch vụ vào bàn");
            if (!allowed)
            {
                ErrorMessage = deniedMsg;
                Notify(deniedMsg, "Error");
                return;
            }

            var (success, msg) = await _productService.AddProductToSessionAsync(
                SelectedTable.CurrentSession.SessionId, product.ProductId, quantity, product.Price);

            if (success)
            {
                var items = await _productService.GetSessionProductsAsync(SelectedTable.CurrentSession.SessionId);
                CurrentSessionProducts = new ObservableCollection<SessionProduct>(items);
                await UpdateSelectedTableBillingAsync();
            }
            else
            {
                ErrorMessage = msg;
                Notify(msg, "Error");
            }
        }

        private async void OnIncreaseProductQuantityAsync(SessionProduct sessionProduct)
        {
            if (sessionProduct == null || SelectedTable == null || SelectedTable.CurrentSession == null) return;

            int newQty = sessionProduct.Quantity + 1;
            var (success, msg) = await _productService.UpdateSessionProductQuantityAsync(sessionProduct.SessionProductId, newQty);
            if (success)
            {
                sessionProduct.Quantity = newQty;
                await UpdateSelectedTableBillingAsync();
            }
            else
            {
                ErrorMessage = msg;
                Notify(msg, "Error");
            }
        }

        private async void OnDecreaseProductQuantityAsync(SessionProduct sessionProduct)
        {
            if (sessionProduct == null || SelectedTable == null || SelectedTable.CurrentSession == null) return;

            if (sessionProduct.Quantity <= 1)
            {
                OnRemoveProductAsync(sessionProduct);
                return;
            }

            int newQty = sessionProduct.Quantity - 1;
            var (success, msg) = await _productService.UpdateSessionProductQuantityAsync(sessionProduct.SessionProductId, newQty);
            if (success)
            {
                sessionProduct.Quantity = newQty;
                await UpdateSelectedTableBillingAsync();
            }
            else
            {
                ErrorMessage = msg;
                Notify(msg, "Error");
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

        #endregion

        #region Methods - CRUD Quy tắc Giá bàn & Khuyến mãi (Issue #15)

        private void OnFilterRuleType(string ruleType)
        {
            _ruleTypeFilter = string.IsNullOrWhiteSpace(ruleType) ? "All" : ruleType;
            ApplyPricingRuleFilter();
        }

        private void OnFilterRuleStatus(string status)
        {
            _ruleStatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status;
            ApplyPricingRuleFilter();
        }

        private void ApplyPricingRuleFilter()
        {
            var query = _allPricingRules.AsEnumerable();

            if (!string.Equals(_ruleTypeFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => string.Equals(r.RuleType, _ruleTypeFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals(_ruleStatusFilter, "Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.IsActive);
            }
            else if (string.Equals(_ruleStatusFilter, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => !r.IsActive);
            }

            FilteredPricingRules = new ObservableCollection<TablePricingRule>(query);
        }

        private void ExecutePrepareNewPricingRule()
        {
            ClearError();
            _selectedPricingRule = null;
            OnPropertyChanged(nameof(SelectedPricingRule));

            _editingPricingRuleId = 0;
            FormRuleName = string.Empty;
            FormRuleTableTypeId = TableTypes.FirstOrDefault()?.TableTypeId ?? 1;
            FormRuleType = "BaseRate";
            FormRuleDayType = "Weekday";
            FormRuleIsAllDay = true;
            FormRuleStartTimeText = "08:00";
            FormRuleEndTimeText = "17:00";
            FormRuleHourlyRateText = "50000";
            FormRuleDiscountType = "Percentage";
            FormRuleDiscountValueText = "10";
            FormRuleEffectiveFrom = DateTime.Today;
            FormRuleHasEndDate = false;
            FormRuleEffectiveTo = DateTime.Today.AddMonths(1);
            FormRulePriorityText = "1";
            FormRuleIsActive = true;

            IsRuleEditMode = false;
            OnPropertyChanged(nameof(RuleFormTitle));
        }

        private async Task ExecuteSavePricingRuleAsync()
        {
            if (IsBusy) return;
            ClearError();

            if (!int.TryParse(FormRulePriorityText?.Trim(), out int priority) || priority < 1)
            {
                ErrorMessage = "Độ ưu tiên (Priority) phải là số nguyên dương (>= 1).";
                return;
            }

            TimeSpan? startTime = null;
            TimeSpan? endTime = null;
            if (!FormRuleIsAllDay)
            {
                if (!TimeSpan.TryParseExact(FormRuleStartTimeText?.Trim(), new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out TimeSpan parsedStart))
                {
                    ErrorMessage = "Giờ bắt đầu không đúng định dạng HH:mm (ví dụ: 08:00 hoặc 18:30).";
                    return;
                }

                if (!TimeSpan.TryParseExact(FormRuleEndTimeText?.Trim(), new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out TimeSpan parsedEnd))
                {
                    ErrorMessage = "Giờ kết thúc không đúng định dạng HH:mm (ví dụ: 17:00 hoặc 23:30).";
                    return;
                }

                startTime = parsedStart;
                endTime = parsedEnd;
            }

            decimal? hourlyRate = null;
            string discountType = null;
            decimal? discountValue = null;

            if (IsBaseRateRule)
            {
                string cleanRate = (FormRuleHourlyRateText ?? string.Empty).Replace(",", "").Replace(".", "").Trim();
                if (!decimal.TryParse(cleanRate, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedRate) || parsedRate <= 0)
                {
                    ErrorMessage = "Đơn giá giờ cơ bản (HourlyRate) phải là số lớn hơn 0 VNĐ!";
                    return;
                }
                hourlyRate = parsedRate;
            }
            else
            {
                discountType = FormRuleDiscountType;
                string cleanDisc = (FormRuleDiscountValueText ?? string.Empty).Replace(",", ".").Trim();
                if (!decimal.TryParse(cleanDisc, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedDisc) || parsedDisc <= 0)
                {
                    ErrorMessage = "Giá trị giảm giá khuyến mãi phải lớn hơn 0!";
                    return;
                }
                discountValue = parsedDisc;
            }

            var model = new TablePricingRule
            {
                PricingRuleId = _editingPricingRuleId,
                TableTypeId = FormRuleTableTypeId,
                RuleName = FormRuleName,
                RuleType = FormRuleType,
                DayType = FormRuleDayType,
                StartTime = startTime,
                EndTime = endTime,
                HourlyRate = hourlyRate,
                DiscountType = discountType,
                DiscountValue = discountValue,
                EffectiveFrom = FormRuleEffectiveFrom.Date,
                EffectiveTo = FormRuleHasEndDate ? FormRuleEffectiveTo?.Date : null,
                Priority = priority,
                IsActive = FormRuleIsActive
            };

            IsBusy = true;
            try
            {
                if (IsRuleEditMode)
                {
                    var (success, msg) = await _pricingRuleService.UpdatePricingRuleAsync(model);
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
                    var (success, msg, newId) = await _pricingRuleService.CreatePricingRuleAsync(model);
                    if (success)
                    {
                        Notify(msg, "Success");
                        ExecutePrepareNewPricingRule();
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

        private async Task ExecuteTogglePricingRuleActiveAsync()
        {
            if (!IsRuleEditMode || _editingPricingRuleId <= 0) return;

            bool targetActive = !FormRuleIsActive;
            string actionDesc = targetActive
                ? "kích hoạt áp dụng lại"
                : "tạm ngưng áp dụng (không dùng để tính tiền)";

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn {actionDesc} quy tắc [{FormRuleName}] không?",
                "Xác nhận trạng thái quy tắc giá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var (success, msg) = await _pricingRuleService.TogglePricingRuleActiveAsync(_editingPricingRuleId, targetActive);
                if (success)
                {
                    FormRuleIsActive = targetActive;
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
                var diff = DateTime.Now - SelectedTable.CurrentSession.StartTime;
                CurrentDuration = diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
                if (SelectedTable.HourlyRate > 0)
                {
                    double hours = Math.Max(1.0 / 60.0, CurrentDuration.TotalHours);
                    CurrentTableAmount = Math.Round((decimal)hours * SelectedTable.HourlyRate, 0);
                    CurrentTotalAmount = CurrentTableAmount + CurrentServiceAmount;
                }
            }
        }

        #endregion
    }
}
