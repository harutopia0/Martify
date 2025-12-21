using Martify.Models;
using Martify.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Martify.ViewModels
{
    public class NavigationVM : BaseVM
    {
        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        #region commands
        public ICommand DashboardCommand { get; set; }
        public ICommand ProductSelectionCommand { get; set; }
        public ICommand ProductsCommand { get; set; }
        public ICommand EmployeesCommand { get; set; }
        public ICommand InvoicesCommand { get; set; }
        #endregion
        public ICommand SettingsCommand { get; set; }
        private void Dashboard(object obj) => CurrentView = new DashboardVM();
        private void ProductSelection(object obj) => CurrentView = new ProductSelectionVM();
        private void Products(object obj) => CurrentView = new ProductsVM();
        private void Employees(object obj) => CurrentView = new EmployeeVM();
        private void Invoices(object obj) => CurrentView = new InvoicesVM();
        private void Settings(object obj) => CurrentView = new SettingsVM();

        /// <summary>
        /// Điều hướng đến Products với bộ lọc cảnh báo tồn kho
        /// </summary>
        /// <param name="alertType">Loại cảnh báo (LowStock hoặc SlowMoving)</param>
        public void NavigateToProductsWithAlert(InventoryAlertType alertType)
        {
            var productsView = new Products();
            if (productsView.DataContext is ProductsVM productsVM)
            {
                productsVM.SetInventoryAlertFilter(alertType);
            }

            _ = NavigateToAsync(productsView);
        }

        private async Task NavigateToAsync(object newView)
        {
            // Start loading
            IsLoading = true;

            try
            {
                // Simulate minimum loading time for smooth UX (optional)
                await Task.Delay(300);

                // Navigate to new view
                App.Current.Dispatcher.Invoke(() =>
                {
                    CurrentView = newView;
                });

                // Give time for view to load
                await Task.Delay(200);
            }
            finally
            {
                // Stop loading
                IsLoading = false;
            }
        }

        public NavigationVM()
        {
            DashboardCommand = new RelayCommand(Dashboard);
            ProductSelectionCommand = new RelayCommand(ProductSelection);
            ProductsCommand = new RelayCommand(Products);
            EmployeesCommand = new RelayCommand(Employees);
            InvoicesCommand = new RelayCommand(Invoices);
            SettingsCommand = new RelayCommand(Settings);
            // Startup Page
            CurrentView = new DashboardVM();
        }
    }

    class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
    }
}
