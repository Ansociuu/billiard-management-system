using billiard_management_system.Core;

namespace billiard_management_system.ViewModels
{
    public abstract class BaseViewModel : ObservableObject
    {
        private string _title;
        private bool _isBusy;
        private string _errorMessage;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                }
            }
        }

        public bool IsNotBusy => !_isBusy;

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public void ClearError()
        {
            ErrorMessage = string.Empty;
        }
    }
}

