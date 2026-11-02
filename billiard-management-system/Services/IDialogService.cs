using System.Windows;

namespace billiard_management_system.Services
{
    /// <summary>
    /// Service hiển thị thông báo hộp thoại, tách biệt MessageBox khỏi ViewModel
    /// </summary>
    public interface IDialogService
    {
        void ShowInformation(string message, string title = "Thông báo");
        void ShowWarning(string message, string title = "Cảnh báo");
        void ShowError(string message, string title = "Lỗi");
        bool ShowConfirmation(string message, string title = "Xác nhận");
    }

    public class DialogService : IDialogService
    {
        public void ShowInformation(string message, string title = "Thông báo")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title = "Cảnh báo")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title = "Lỗi")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool ShowConfirmation(string message, string title = "Xác nhận")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
