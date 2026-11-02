using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace billiard_management_system.ViewModels
{
    /// <summary>
    /// Base ViewModel triển khai INotifyPropertyChanged cho tất cả ViewModel trong hệ thống.
    /// Cung cấp các hàm SetProperty và kích hoạt PropertyChanged an toàn kiểu dữ liệu.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Kích hoạt sự kiện PropertyChanged
        /// </summary>
        /// <param name="propertyName">Tên thuộc tính (tự động suy luận bằng CallerMemberName)</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Gán giá trị mới cho trường backing field, nếu giá trị thay đổi thì kích hoạt OnPropertyChanged
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của property</typeparam>
        /// <param name="storage">Tham chiếu backing field</param>
        /// <param name="value">Giá trị mới</param>
        /// <param name="propertyName">Tên property</param>
        /// <returns>True nếu giá trị thực sự thay đổi</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private bool _isBusy;
        /// <summary>
        /// Trạng thái đang tải/xử lý bất đồng bộ
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _busyMessage;
        /// <summary>
        /// Thông điệp hiển thị kèm theo khi đang bận
        /// </summary>
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }
    }
}
