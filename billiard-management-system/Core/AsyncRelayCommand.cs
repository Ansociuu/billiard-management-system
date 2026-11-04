using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace billiard_management_system.Core
{
    /// <summary>
    /// Triển khai ICommand bất đồng bộ, hỗ trợ theo dõi trạng thái đang thực thi (IsExecuting)
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                RaiseCanExecuteChanged();
            }
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute());
        }

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter)) return;

            try
            {
                IsExecuting = true;
                await _execute();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AsyncRelayCommand Error]: {ex}");
                System.Windows.MessageBox.Show(
                    $"Không thể hoàn tất thao tác do phát sinh lỗi:\n\n{ex.Message}",
                    "Thông báo lỗi thao tác",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
            finally
            {
                IsExecuting = false;
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

