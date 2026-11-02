using System.Windows.Input;
using billiard_management_system.Core;

namespace billiard_management_system.ViewModels
{
    public class MenuPlaceholderViewModel : BaseViewModel
    {
        public string IconKind { get; }
        public string Subtitle { get; }

        public ICommand RefreshCommand { get; }

        public MenuPlaceholderViewModel(string title, string iconKind, string subtitle)
        {
            Title = title;
            IconKind = iconKind;
            Subtitle = subtitle;
            RefreshCommand = new RelayCommand(ExecuteRefresh);
        }

        private void ExecuteRefresh()
        {
            IsBusy = true;
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
            {
                IsBusy = false;
            }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
