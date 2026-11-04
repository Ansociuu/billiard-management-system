using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using billiard_management_system.Core;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using billiard_management_system.Services;

namespace billiard_management_system.ViewModels
{
    public class ReportManagementViewModel : BaseViewModel
    {
        private readonly IReportService _reportService;

        private DateTime _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _toDate = DateTime.Today;

        private int _paidInvoiceCount;
        private int _pendingInvoiceCount;
        private int _cancelledInvoiceCount;
        private decimal _tableRevenue;
        private decimal _productRevenue;
        private decimal _totalDiscount;
        private decimal _netRevenue;
        private decimal _cashRevenue;
        private decimal _bankTransferRevenue;

        private int _totalSessionCount;
        private int _completedSessionCount;
        private int _activeSessionCount;
        private int _cancelledSessionCount;

        private ObservableCollection<DailyRevenueItem> _dailyBreakdown = new ObservableCollection<DailyRevenueItem>();
        private ObservableCollection<TableRevenueItem> _tableBreakdown = new ObservableCollection<TableRevenueItem>();
        private ObservableCollection<ProductRevenueItem> _topProducts = new ObservableCollection<ProductRevenueItem>();
        private ObservableCollection<Invoice> _paidInvoicesForReconciliation = new ObservableCollection<Invoice>();

        public event Action<string, string> NotificationRequested;

        public ReportManagementViewModel(IReportService reportService = null)
        {
            _reportService = reportService ?? new ReportService();
            Title = "Báo cáo Doanh thu & Thống kê Hoạt động";

            FilterReportCommand = new AsyncRelayCommand(LoadReportAsync);
            FilterTodayCommand = new AsyncRelayCommand(ExecuteFilterTodayAsync);
            FilterLast7DaysCommand = new AsyncRelayCommand(ExecuteFilterLast7DaysAsync);
            FilterCurrentMonthCommand = new AsyncRelayCommand(ExecuteFilterCurrentMonthAsync);

            _ = LoadReportAsync();
        }

        #region Properties

        public bool CanViewReport => UserSession.HasPermission(PermissionCodes.ReportView);

        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public int PaidInvoiceCount
        {
            get => _paidInvoiceCount;
            set => SetProperty(ref _paidInvoiceCount, value);
        }

        public int PendingInvoiceCount
        {
            get => _pendingInvoiceCount;
            set => SetProperty(ref _pendingInvoiceCount, value);
        }

        public int CancelledInvoiceCount
        {
            get => _cancelledInvoiceCount;
            set => SetProperty(ref _cancelledInvoiceCount, value);
        }

        public decimal TableRevenue
        {
            get => _tableRevenue;
            set => SetProperty(ref _tableRevenue, value);
        }

        public decimal ProductRevenue
        {
            get => _productRevenue;
            set => SetProperty(ref _productRevenue, value);
        }

        public decimal TotalDiscount
        {
            get => _totalDiscount;
            set => SetProperty(ref _totalDiscount, value);
        }

        public decimal NetRevenue
        {
            get => _netRevenue;
            set => SetProperty(ref _netRevenue, value);
        }

        public decimal CashRevenue
        {
            get => _cashRevenue;
            set => SetProperty(ref _cashRevenue, value);
        }

        public decimal BankTransferRevenue
        {
            get => _bankTransferRevenue;
            set => SetProperty(ref _bankTransferRevenue, value);
        }

        public int TotalSessionCount
        {
            get => _totalSessionCount;
            set => SetProperty(ref _totalSessionCount, value);
        }

        public int CompletedSessionCount
        {
            get => _completedSessionCount;
            set => SetProperty(ref _completedSessionCount, value);
        }

        public int ActiveSessionCount
        {
            get => _activeSessionCount;
            set => SetProperty(ref _activeSessionCount, value);
        }

        public int CancelledSessionCount
        {
            get => _cancelledSessionCount;
            set => SetProperty(ref _cancelledSessionCount, value);
        }

        public ObservableCollection<DailyRevenueItem> DailyBreakdown
        {
            get => _dailyBreakdown;
            set => SetProperty(ref _dailyBreakdown, value);
        }

        public ObservableCollection<TableRevenueItem> TableBreakdown
        {
            get => _tableBreakdown;
            set => SetProperty(ref _tableBreakdown, value);
        }

        public ObservableCollection<ProductRevenueItem> TopProducts
        {
            get => _topProducts;
            set => SetProperty(ref _topProducts, value);
        }

        public ObservableCollection<Invoice> PaidInvoicesForReconciliation
        {
            get => _paidInvoicesForReconciliation;
            set => SetProperty(ref _paidInvoicesForReconciliation, value);
        }

        // Đối chiếu tổng số liệu từ danh sách hóa đơn Paid với tổng báo cáo
        public decimal ReconciledInvoicesSum => _paidInvoicesForReconciliation.Sum(i => i.TotalAmount);
        public string ReconciliationStatusText => ReconciledInvoicesSum == NetRevenue
            ? $"✅ Khớp 100% với tổng {PaidInvoiceCount} hóa đơn Paid ({ReconciledInvoicesSum:N0} đ)"
            : $"⚠️ Chênh lệch đối chiếu: {Math.Abs(ReconciledInvoicesSum - NetRevenue):N0} đ";

        #endregion

        #region Commands

        public ICommand FilterReportCommand { get; }
        public ICommand FilterTodayCommand { get; }
        public ICommand FilterLast7DaysCommand { get; }
        public ICommand FilterCurrentMonthCommand { get; }

        #endregion

        #region Methods

        public async Task LoadReportAsync()
        {
            if (FromDate.Date > ToDate.Date)
            {
                ErrorMessage = "Ngày bắt đầu không được lớn hơn ngày kết thúc!";
                Notify(ErrorMessage, "Error");
                return;
            }

            IsBusy = true;
            ClearError();

            try
            {
                var (success, msg, report) = await _reportService.GetRevenueReportAsync(FromDate, ToDate);
                if (!success || report == null)
                {
                    ErrorMessage = msg;
                    Notify(msg, "Error");
                    return;
                }

                PaidInvoiceCount = report.PaidInvoiceCount;
                PendingInvoiceCount = report.PendingInvoiceCount;
                CancelledInvoiceCount = report.CancelledInvoiceCount;

                TableRevenue = report.TableRevenue;
                ProductRevenue = report.ProductRevenue;
                TotalDiscount = report.TotalDiscount;
                NetRevenue = report.NetRevenue;
                CashRevenue = report.CashRevenue;
                BankTransferRevenue = report.BankTransferRevenue;

                TotalSessionCount = report.TotalSessionCount;
                CompletedSessionCount = report.CompletedSessionCount;
                ActiveSessionCount = report.ActiveSessionCount;
                CancelledSessionCount = report.CancelledSessionCount;

                DailyBreakdown = new ObservableCollection<DailyRevenueItem>(report.DailyBreakdown);
                TableBreakdown = new ObservableCollection<TableRevenueItem>(report.TableBreakdown);
                TopProducts = new ObservableCollection<ProductRevenueItem>(report.TopProducts);
                PaidInvoicesForReconciliation = new ObservableCollection<Invoice>(report.PaidInvoicesForReconciliation);

                OnPropertyChanged(nameof(ReconciledInvoicesSum));
                OnPropertyChanged(nameof(ReconciliationStatusText));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải báo cáo: {ex.Message}";
                Notify(ErrorMessage, "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteFilterTodayAsync()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            await LoadReportAsync();
        }

        private async Task ExecuteFilterLast7DaysAsync()
        {
            FromDate = DateTime.Today.AddDays(-6);
            ToDate = DateTime.Today;
            await LoadReportAsync();
        }

        private async Task ExecuteFilterCurrentMonthAsync()
        {
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDate = DateTime.Today;
            await LoadReportAsync();
        }

        private void Notify(string message, string type = "Info")
        {
            NotificationRequested?.Invoke(message, type);
        }

        #endregion
    }
}
