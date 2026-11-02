using System;
using System.Collections.ObjectModel;
using billiard_management_system.Core;

namespace billiard_management_system.Models
{
    /// <summary>
    /// Phiên chơi tại bàn (Session)
    /// </summary>
    public class BilliardSession : ObservableObject
    {
        private long _sessionId;
        private int _tableId;
        private long _userId;
        private long? _customerId;
        private DateTime _startTime;
        private DateTime? _endTime;
        private string _status = "Active";
        private decimal _tableAmount;

        // UI / Display properties
        private string _tableNumber;
        private string _staffName;
        private string _customerName;
        private ObservableCollection<SessionProduct> _orderedProducts = new ObservableCollection<SessionProduct>();

        public long SessionId
        {
            get => _sessionId;
            set => SetProperty(ref _sessionId, value);
        }

        public int TableId
        {
            get => _tableId;
            set => SetProperty(ref _tableId, value);
        }

        public long UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        public long? CustomerId
        {
            get => _customerId;
            set => SetProperty(ref _customerId, value);
        }

        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public decimal TableAmount
        {
            get => _tableAmount;
            set => SetProperty(ref _tableAmount, value);
        }

        public string TableNumber
        {
            get => _tableNumber;
            set => SetProperty(ref _tableNumber, value);
        }

        public string StaffName
        {
            get => _staffName;
            set => SetProperty(ref _staffName, value);
        }

        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        public ObservableCollection<SessionProduct> OrderedProducts
        {
            get => _orderedProducts;
            set => SetProperty(ref _orderedProducts, value);
        }
    }
}

