using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace billiard_management_system.Core
{
    /// <summary>
    /// Lớp cơ sở cung cấp cơ chế thông báo thay đổi thuộc tính (INotifyPropertyChanged)
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

