using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        private ObservableCollection<BilliardTable> _filteredTables = new ObservableCollection<BilliardTable>();
        private ObservableCollection<TableType> _tableTypes = new ObservableCollection<TableType>();
        private ObservableCollection<SessionProduct> _currentSessionProducts = new ObservableCollection<SessionProduct>();
        private ObservableCollection<Product> _availableProducts = new ObservableCollection<Product>();

        private BilliardTable _selectedTable;
        private int? _selectedTypeId;
        private string _statusFilter = "All";

        // Current Billing stats for selected table
        private decimal _currentTableAmount;
        private decimal _currentServiceAmount;
        private decimal _currentTotalAmount;
        private TimeSpan _currentDuration;

        public TableManagementViewModel(
            ITableService tableService = null,
            IProductService productService = null,
            IOrderService orderService = null)
        {
            _tableService = tableService ?? new TableService();
            _productService = productService ?? new ProductService();
            _orderService = orderService ?? new OrderService();

            Title = "Quản lý Bàn Chơi";

            // Khởi tạo các Commands
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            SelectTableCommand = new RelayCommand<BilliardTable>(OnTableSelected);
            FilterTypeCommand = new RelayCommand<object>(OnFilterType);
            FilterStatusCommand = new RelayCommand<string>(OnFilterStatus);
            StartSessionCommand = new AsyncRelayCommand(OnStartSessionAsync, () => SelectedTable != null && SelectedTable.IsAvailable);
            AddProductCommand = new RelayCommand<Product>(OnAddProductAsync);
            RemoveProductCommand = new RelayCommand<SessionProduct>(OnRemoveProductAsync);
            CheckoutCommand = new AsyncRelayCommand(OnCheckoutAsync, () => SelectedTable != null && SelectedTable.IsOccupied);

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

        #region Properties

        public ObservableCollection<BilliardTable> FilteredTables
        {
            get => _filteredTables;
            set => SetProperty(ref _filteredTables, value);
        }

        public ObservableCollection<TableType> TableTypes
        {
            get => _tableTypes;
            set => SetProperty(ref _tableTypes, value);
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
                    _ = OnSelectedTableChangedAsync();
                }
            }
        }

        public bool HasSelectedTable => _selectedTable != null;
        public bool CanStartSession => _selectedTable != null && _selectedTable.IsAvailable;
        public bool CanCheckout => _selectedTable != null && _selectedTable.IsOccupied;

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
        public int TotalCount => _allTables.Count;

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand SelectTableCommand { get; }
        public ICommand FilterTypeCommand { get; }
        public ICommand FilterStatusCommand { get; }
        public ICommand StartSessionCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand RemoveProductCommand { get; }
        public ICommand CheckoutCommand { get; }

        #endregion

        #region Methods

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            ClearError();

            try
            {
                var tables = await _tableService.GetAllTablesAsync();
                _allTables = new ObservableCollection<BilliardTable>(tables);

                var types = await _tableService.GetTableTypesAsync();
                TableTypes = new ObservableCollection<TableType>(types);

                var products = await _productService.GetProductsAsync();
                AvailableProducts = new ObservableCollection<Product>(products);

                ApplyFilter();
                UpdateSummaryStats();

                if (SelectedTable != null)
                {
                    SelectedTable = _allTables.FirstOrDefault(t => t.TableId == SelectedTable.TableId);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu bàn chơi: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnTableSelected(BilliardTable table)
        {
            SelectedTable = table;
        }

        private void OnFilterType(object param)
        {
            if (param == null)
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
            _statusFilter = status ?? "All";
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

            long userId = UserSession.CurrentUser?.UserId ?? 1;
            var (success, msg, sessionId) = await _tableService.StartSessionAsync(SelectedTable.TableId, userId);

            if (success)
            {
                await LoadDataAsync();
                SelectedTable = _allTables.FirstOrDefault(t => t.TableId == SelectedTable.TableId);
            }
            else
            {
                ErrorMessage = msg;
            }
        }

        private async void OnAddProductAsync(Product product)
        {
            if (product == null || SelectedTable == null || SelectedTable.CurrentSession == null) return;

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

            long userId = UserSession.CurrentUser?.UserId ?? 1;
            var (success, invoiceCode, msg) = await _orderService.CheckoutAsync(
                SelectedTable.CurrentSession.SessionId, SelectedTable.TableId, userId, 0, "Cash");

            if (success)
            {
                await LoadDataAsync();
                SelectedTable = _allTables.FirstOrDefault(t => t.TableId == SelectedTable.TableId);
            }
            else
            {
                ErrorMessage = msg;
            }
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
