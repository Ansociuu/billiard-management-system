using System;
using billiard_management_system.Core;

namespace billiard_management_system.Models
{
    public enum TableStatusEnum
    {
        Available,
        Occupied,
        Maintenance
    }

    /// <summary>
    /// Thực thể bàn chơi Billiard kế thừa ObservableObject để cập nhật trạng thái UI thời gian thực
    /// </summary>
    public class BilliardTable : ObservableObject
    {
        private int _tableId;
        private int _tableTypeId;
        private string _tableNumber;
        private string _status = "Available";
        private bool _isActive = true;

        // Extra UI / Display properties
        private string _typeName;
        private decimal _hourlyRate;
        private BilliardSession _currentSession;
        private TimeSpan _elapsedTime;

        public int TableId
        {
            get => _tableId;
            set => SetProperty(ref _tableId, value);
        }

        public int TableTypeId
        {
            get => _tableTypeId;
            set => SetProperty(ref _tableTypeId, value);
        }

        public string TableNumber
        {
            get => _tableNumber;
            set => SetProperty(ref _tableNumber, value);
        }

        /// <summary>
        /// Available, Occupied, Maintenance
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(IsAvailable));
                    OnPropertyChanged(nameof(IsOccupied));
                    OnPropertyChanged(nameof(StatusDisplayName));
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string TypeName
        {
            get => _typeName;
            set => SetProperty(ref _typeName, value);
        }

        public decimal HourlyRate
        {
            get => _hourlyRate;
            set => SetProperty(ref _hourlyRate, value);
        }

        public BilliardSession CurrentSession
        {
            get => _currentSession;
            set
            {
                if (SetProperty(ref _currentSession, value))
                {
                    UpdateElapsed();
                }
            }
        }

        public TimeSpan ElapsedTime
        {
            get => _elapsedTime;
            set => SetProperty(ref _elapsedTime, value);
        }

        public bool IsAvailable => Status == "Available";
        public bool IsOccupied => Status == "Occupied";
        public bool IsMaintenance => Status == "Maintenance";

        public string StatusDisplayName
        {
            get
            {
                switch (Status)
                {
                    case "Available": return "Bàn trống";
                    case "Occupied": return "Đang chơi";
                    case "Maintenance": return "Bảo trì";
                    default: return Status;
                }
            }
        }

        /// <summary>
        /// Cập nhật thời gian đã chơi của phiên hiện tại
        /// </summary>
        public void UpdateElapsed()
        {
            if (CurrentSession != null && CurrentSession.StartTime != default)
            {
                ElapsedTime = DateTime.Now - CurrentSession.StartTime;
            }
            else
            {
                ElapsedTime = TimeSpan.Zero;
            }
        }
    }
}

