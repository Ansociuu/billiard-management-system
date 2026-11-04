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
    public class InvoiceManagementViewModel : BaseViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IPricingRuleService _pricingRuleService;
        private readonly IProductService _productService;

        // Danh sách Hóa đơn & Chi tiết
        private ObservableCollection<Invoice> _invoices = new ObservableCollection<Invoice>();
        private ObservableCollection<InvoiceDetail> _selectedInvoiceDetails = new ObservableCollection<InvoiceDetail>();
        private ObservableCollection<SessionPricingDetail> _selectedInvoicePricingSegments = new ObservableCollection<SessionPricingDetail>();
        private Invoice _selectedInvoice;

        // Bộ lọc Hóa đơn
        private string _invoiceSearchKeyword = string.Empty;
        private DateTime? _invoiceFromDate = DateTime.Today.AddDays(-30);
        private DateTime? _invoiceToDate = DateTime.Today;
        private string _invoiceStatusFilter = "All";
        private string _pendingPaymentMethod = "Cash";

        // Danh sách Lịch sử Phiên chơi & Chi tiết
        private ObservableCollection<BilliardSession> _sessionsHistory = new ObservableCollection<BilliardSession>();
        private ObservableCollection<SessionProduct> _selectedSessionProducts = new ObservableCollection<SessionProduct>();
        private ObservableCollection<SessionPricingDetail> _selectedSessionPricingSegments = new ObservableCollection<SessionPricingDetail>();
        private BilliardSession _selectedHistorySession;

        // Bộ lọc Phiên chơi
        private string _sessionSearchKeyword = string.Empty;
        private DateTime? _sessionFromDate = DateTime.Today.AddDays(-30);
        private DateTime? _sessionToDate = DateTime.Today;
        private string _sessionStatusFilter = "All";

        public event Action<string, string> NotificationRequested;

        public InvoiceManagementViewModel(
            IOrderService orderService = null,
            IPricingRuleService pricingRuleService = null,
            IProductService productService = null)
        {
            _orderService = orderService ?? new OrderService();
            _pricingRuleService = pricingRuleService ?? new PricingRuleService();
            _productService = productService ?? new ProductService();

            Title = "Tra cứu Hóa đơn & Lịch sử Phiên chơi";

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            SearchInvoicesCommand = new AsyncRelayCommand(LoadInvoicesAsync);
            ResetInvoiceFilterCommand = new AsyncRelayCommand(ExecuteResetInvoiceFilterAsync);
            FilterInvoiceStatusCommand = new RelayCommand<string>(status => _ = OnFilterInvoiceStatusAsync(status));
            PayPendingInvoiceCommand = new AsyncRelayCommand(ExecutePayPendingInvoiceAsync);

            SearchSessionsCommand = new AsyncRelayCommand(LoadSessionsAsync);
            ResetSessionFilterCommand = new AsyncRelayCommand(ExecuteResetSessionFilterAsync);
            FilterSessionStatusCommand = new RelayCommand<string>(status => _ = OnFilterSessionStatusAsync(status));

            _ = LoadDataAsync();
        }

        #region Properties - Invoices Lookup

        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set => SetProperty(ref _invoices, value);
        }

        public ObservableCollection<InvoiceDetail> SelectedInvoiceDetails
        {
            get => _selectedInvoiceDetails;
            set => SetProperty(ref _selectedInvoiceDetails, value);
        }

        public ObservableCollection<SessionPricingDetail> SelectedInvoicePricingSegments
        {
            get => _selectedInvoicePricingSegments;
            set => SetProperty(ref _selectedInvoicePricingSegments, value);
        }

        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                if (SetProperty(ref _selectedInvoice, value))
                {
                    OnPropertyChanged(nameof(HasSelectedInvoice));
                    OnPropertyChanged(nameof(IsSelectedInvoicePending));
                    OnPropertyChanged(nameof(IsSelectedInvoicePaid));
                    _ = LoadSelectedInvoiceDetailsAsync();
                }
            }
        }

        public bool HasSelectedInvoice => _selectedInvoice != null;
        public bool IsSelectedInvoicePending => _selectedInvoice != null && string.Equals(_selectedInvoice.Status, "Pending", StringComparison.OrdinalIgnoreCase);
        public bool IsSelectedInvoicePaid => _selectedInvoice != null && string.Equals(_selectedInvoice.Status, "Paid", StringComparison.OrdinalIgnoreCase);

        public string InvoiceSearchKeyword
        {
            get => _invoiceSearchKeyword;
            set => SetProperty(ref _invoiceSearchKeyword, value);
        }

        public DateTime? InvoiceFromDate
        {
            get => _invoiceFromDate;
            set => SetProperty(ref _invoiceFromDate, value);
        }

        public DateTime? InvoiceToDate
        {
            get => _invoiceToDate;
            set => SetProperty(ref _invoiceToDate, value);
        }

        public string InvoiceStatusFilter
        {
            get => _invoiceStatusFilter;
            set => SetProperty(ref _invoiceStatusFilter, value);
        }

        public List<string> AllowedPaymentMethods { get; } = new List<string> { "Cash", "BankTransfer" };

        public string PendingPaymentMethod
        {
            get => _pendingPaymentMethod;
            set => SetProperty(ref _pendingPaymentMethod, value);
        }

        public int TotalInvoicesCount => _invoices.Count;
        public int PaidInvoicesCount => _invoices.Count(i => string.Equals(i.Status, "Paid", StringComparison.OrdinalIgnoreCase));
        public int PendingInvoicesCount => _invoices.Count(i => string.Equals(i.Status, "Pending", StringComparison.OrdinalIgnoreCase));
        public decimal FilteredPaidRevenue => _invoices
            .Where(i => string.Equals(i.Status, "Paid", StringComparison.OrdinalIgnoreCase))
            .Sum(i => i.TotalAmount);

        #endregion

        #region Properties - Sessions History Lookup

        public ObservableCollection<BilliardSession> SessionsHistory
        {
            get => _sessionsHistory;
            set => SetProperty(ref _sessionsHistory, value);
        }

        public ObservableCollection<SessionProduct> SelectedSessionProducts
        {
            get => _selectedSessionProducts;
            set => SetProperty(ref _selectedSessionProducts, value);
        }

        public ObservableCollection<SessionPricingDetail> SelectedSessionPricingSegments
        {
            get => _selectedSessionPricingSegments;
            set => SetProperty(ref _selectedSessionPricingSegments, value);
        }

        public BilliardSession SelectedHistorySession
        {
            get => _selectedHistorySession;
            set
            {
                if (SetProperty(ref _selectedHistorySession, value))
                {
                    OnPropertyChanged(nameof(HasSelectedHistorySession));
                    _ = LoadSelectedSessionDetailsAsync();
                }
            }
        }

        public bool HasSelectedHistorySession => _selectedHistorySession != null;

        public string SessionSearchKeyword
        {
            get => _sessionSearchKeyword;
            set => SetProperty(ref _sessionSearchKeyword, value);
        }

        public DateTime? SessionFromDate
        {
            get => _sessionFromDate;
            set => SetProperty(ref _sessionFromDate, value);
        }

        public DateTime? SessionToDate
        {
            get => _sessionToDate;
            set => SetProperty(ref _sessionToDate, value);
        }

        public string SessionStatusFilter
        {
            get => _sessionStatusFilter;
            set => SetProperty(ref _sessionStatusFilter, value);
        }

        public int TotalSessionsCount => _sessionsHistory.Count;
        public int ActiveSessionsCount => _sessionsHistory.Count(s => string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase));
        public int CompletedSessionsCount => _sessionsHistory.Count(s => string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase));
        public int CancelledSessionsCount => _sessionsHistory.Count(s => string.Equals(s.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand SearchInvoicesCommand { get; }
        public ICommand ResetInvoiceFilterCommand { get; }
        public ICommand FilterInvoiceStatusCommand { get; }
        public ICommand PayPendingInvoiceCommand { get; }

        public ICommand SearchSessionsCommand { get; }
        public ICommand ResetSessionFilterCommand { get; }
        public ICommand FilterSessionStatusCommand { get; }

        #endregion

        #region Methods

        public async Task LoadDataAsync()
        {
            await LoadInvoicesAsync();
            await LoadSessionsAsync();
        }

        public async Task LoadInvoicesAsync()
        {
            if (InvoiceFromDate.HasValue && InvoiceToDate.HasValue && InvoiceFromDate.Value.Date > InvoiceToDate.Value.Date)
            {
                ErrorMessage = "Khoảng ngày lọc hóa đơn không hợp lệ: Ngày bắt đầu không được lớn hơn ngày kết thúc!";
                Notify(ErrorMessage, "Error");
                return;
            }

            IsBusy = true;
            ClearError();

            try
            {
                var list = await _orderService.GetInvoicesAsync(
                    InvoiceFromDate, InvoiceToDate, InvoiceStatusFilter, InvoiceSearchKeyword);
                Invoices = new ObservableCollection<Invoice>(list);

                OnPropertyChanged(nameof(TotalInvoicesCount));
                OnPropertyChanged(nameof(PaidInvoicesCount));
                OnPropertyChanged(nameof(PendingInvoicesCount));
                OnPropertyChanged(nameof(FilteredPaidRevenue));

                if (SelectedInvoice != null)
                {
                    SelectedInvoice = Invoices.FirstOrDefault(i => i.InvoiceId == SelectedInvoice.InvoiceId) ?? Invoices.FirstOrDefault();
                }
                else
                {
                    SelectedInvoice = Invoices.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải danh sách hóa đơn: {ex.Message}";
                Notify(ErrorMessage, "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSelectedInvoiceDetailsAsync()
        {
            if (SelectedInvoice == null)
            {
                SelectedInvoiceDetails = new ObservableCollection<InvoiceDetail>();
                SelectedInvoicePricingSegments = new ObservableCollection<SessionPricingDetail>();
                return;
            }

            try
            {
                var details = await _orderService.GetInvoiceDetailsAsync(SelectedInvoice.InvoiceId);
                SelectedInvoiceDetails = new ObservableCollection<InvoiceDetail>(details);

                var segments = await _pricingRuleService.GetSessionPricingDetailsAsync(SelectedInvoice.SessionId);
                SelectedInvoicePricingSegments = new ObservableCollection<SessionPricingDetail>(segments);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải chi tiết hóa đơn: {ex.Message}";
            }
        }

        private async Task OnFilterInvoiceStatusAsync(string status)
        {
            InvoiceStatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status;
            await LoadInvoicesAsync();
        }

        private async Task ExecuteResetInvoiceFilterAsync()
        {
            InvoiceSearchKeyword = string.Empty;
            InvoiceFromDate = DateTime.Today.AddDays(-30);
            InvoiceToDate = DateTime.Today;
            InvoiceStatusFilter = "All";
            await LoadInvoicesAsync();
        }

        private async Task ExecutePayPendingInvoiceAsync()
        {
            if (SelectedInvoice == null || !IsSelectedInvoicePending) return;

            var confirm = MessageBox.Show(
                $"Xác nhận thanh toán hóa đơn chờ [{SelectedInvoice.InvoiceCode}] của bàn [{SelectedInvoice.TableNumber}]?\n\n" +
                $"• Tổng thanh toán: {SelectedInvoice.TotalAmount:N0} VNĐ\n" +
                $"• Phương thức: {PendingPaymentMethod}",
                "Xác nhận thanh toán hóa đơn chờ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            long userId = UserSession.CurrentUser?.UserId ?? 1;
            var (success, _, msg) = await _orderService.PayInvoiceAsync(
                SelectedInvoice.InvoiceId, userId, SelectedInvoice.DiscountAmount, PendingPaymentMethod);

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

        public async Task LoadSessionsAsync()
        {
            if (SessionFromDate.HasValue && SessionToDate.HasValue && SessionFromDate.Value.Date > SessionToDate.Value.Date)
            {
                ErrorMessage = "Khoảng ngày lọc phiên chơi không hợp lệ: Ngày bắt đầu không được lớn hơn ngày kết thúc!";
                Notify(ErrorMessage, "Error");
                return;
            }

            IsBusy = true;
            ClearError();

            try
            {
                var list = await _orderService.GetSessionsHistoryAsync(
                    SessionFromDate, SessionToDate, SessionStatusFilter, SessionSearchKeyword);
                SessionsHistory = new ObservableCollection<BilliardSession>(list);

                OnPropertyChanged(nameof(TotalSessionsCount));
                OnPropertyChanged(nameof(ActiveSessionsCount));
                OnPropertyChanged(nameof(CompletedSessionsCount));
                OnPropertyChanged(nameof(CancelledSessionsCount));

                if (SelectedHistorySession != null)
                {
                    SelectedHistorySession = SessionsHistory.FirstOrDefault(s => s.SessionId == SelectedHistorySession.SessionId) ?? SessionsHistory.FirstOrDefault();
                }
                else
                {
                    SelectedHistorySession = SessionsHistory.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải lịch sử phiên chơi: {ex.Message}";
                Notify(ErrorMessage, "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSelectedSessionDetailsAsync()
        {
            if (SelectedHistorySession == null)
            {
                SelectedSessionProducts = new ObservableCollection<SessionProduct>();
                SelectedSessionPricingSegments = new ObservableCollection<SessionPricingDetail>();
                return;
            }

            try
            {
                var prods = await _productService.GetSessionProductsAsync(SelectedHistorySession.SessionId);
                SelectedSessionProducts = new ObservableCollection<SessionProduct>(prods);

                var segments = await _pricingRuleService.GetSessionPricingDetailsAsync(SelectedHistorySession.SessionId);
                SelectedSessionPricingSegments = new ObservableCollection<SessionPricingDetail>(segments);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải chi tiết phiên chơi: {ex.Message}";
            }
        }

        private async Task OnFilterSessionStatusAsync(string status)
        {
            SessionStatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status;
            await LoadSessionsAsync();
        }

        private async Task ExecuteResetSessionFilterAsync()
        {
            SessionSearchKeyword = string.Empty;
            SessionFromDate = DateTime.Today.AddDays(-30);
            SessionToDate = DateTime.Today;
            SessionStatusFilter = "All";
            await LoadSessionsAsync();
        }

        private void Notify(string message, string type = "Info")
        {
            NotificationRequested?.Invoke(message, type);
        }

        #endregion
    }
}
