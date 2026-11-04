using System;
using System.Windows.Input;

namespace billiard_management_system.Core
{
    /// <summary>
    /// Triển khai ICommand hỗ trợ ủy quyền thực thi (Delegate / Action)
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _executeWithParam;
        private readonly Action _execute;
        private readonly Predicate<object> _canExecuteWithParam;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithParam = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
                return _canExecute();
            if (_canExecuteWithParam != null)
                return _canExecuteWithParam(parameter);
            return true;
        }

        public void Execute(object parameter)
        {
            try
            {
                if (_execute != null)
                    _execute();
                else
                    _executeWithParam?.Invoke(parameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RelayCommand Error]: {ex}");
                System.Windows.MessageBox.Show(
                    $"Không thể thực hiện thao tác:\n\n{ex.Message}",
                    "Thông báo lỗi thao tác",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Triển khai Generic ICommand với kiểu tham số tường minh T
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            if (parameter == null && typeof(T).IsValueType) return false;
            return _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            try
            {
                _execute((T)parameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RelayCommand<T> Error]: {ex}");
                System.Windows.MessageBox.Show(
                    $"Không thể thực hiện thao tác:\n\n{ex.Message}",
                    "Thông báo lỗi thao tác",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}

