using System;
using System.Collections.Generic;
using billiard_management_system.ViewModels;

namespace billiard_management_system.Services
{
    /// <summary>
    /// Triển khai INavigationService dựa trên việc thay đổi CurrentViewModel
    /// </summary>
    public class NavigationService : BaseViewModel, INavigationService
    {
        private readonly Func<Type, BaseViewModel> _viewModelFactory;
        private BaseViewModel _currentViewModel;

        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        object INavigationService.CurrentViewModel => CurrentViewModel;

        public NavigationService(Func<Type, BaseViewModel> viewModelFactory = null)
        {
            _viewModelFactory = viewModelFactory ?? (type => (BaseViewModel)Activator.CreateInstance(type));
        }

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            var vm = _viewModelFactory(typeof(TViewModel));
            CurrentViewModel = vm;
        }

        public void NavigateTo(BaseViewModel viewModel)
        {
            CurrentViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}
