using Martify.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class ImportHistoryVM : BaseVM
    {
        private ObservableCollection<ImportReceiptViewModel> _importReceipts;
        public ObservableCollection<ImportReceiptViewModel> ImportReceipts
        {
            get => _importReceipts;
            set { _importReceipts = value; OnPropertyChanged(); }
        }

        private DateTime? _startDate = DateTime.Now.AddMonths(-1);
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime? _endDate = DateTime.Now;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        private bool _hasNoReceipts;
        public bool HasNoReceipts
        {
            get => _hasNoReceipts;
            set { _hasNoReceipts = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; set; }
        public ICommand ToggleDetailsCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        public ImportHistoryVM()
        {
            RefreshCommand = new RelayCommand<object>((p) => true, (p) => LoadData());
            ToggleDetailsCommand = new RelayCommand<ImportReceiptViewModel>((p) => p != null, (p) => ToggleDetails(p));
            CloseCommand = new RelayCommand<Window>((p) => p != null, (p) => p.Close());

            ImportReceipts = new ObservableCollection<ImportReceiptViewModel>();
            LoadData();
        }

        private void ToggleDetails(ImportReceiptViewModel receipt)
        {
            if (receipt != null)
            {
                receipt.IsExpanded = !receipt.IsExpanded;
            }
        }

        public void LoadData()
        {
            try
            {
                // Validate date range
                if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
                {
                    MessageBox.Show("Ngày bắt đầu không thể lớn hơn ngày kết thúc!",
                                    "Lỗi",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                // Set time to cover full days
                DateTime startDateTime = StartDate?.Date ?? DateTime.MinValue;
                DateTime endDateTime = EndDate?.Date.AddDays(1).AddSeconds(-1) ?? DateTime.MaxValue;

                // Load data with all related entities
                var list = DataProvider.Ins.DB.ImportReceipts
                    .Include(r => r.Supplier)
                    .Include(r => r.Employee)
                    .Include(r => r.ImportReceiptDetails)
                        .ThenInclude(d => d.Product)
                    .AsNoTracking()
                    .Where(r => r.ImportDate >= startDateTime && r.ImportDate <= endDateTime)
                    .OrderByDescending(r => r.ImportDate)
                    .ToList();

                // Convert to ViewModels with calculated subtotals
                var viewModels = list.Select(receipt => new ImportReceiptViewModel(receipt)).ToList();

                ImportReceipts = new ObservableCollection<ImportReceiptViewModel>(viewModels);
                HasNoReceipts = !viewModels.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử nhập hàng:\n{ex.Message}\n\nChi tiết: {ex.InnerException?.Message}",
                                "Lỗi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                ImportReceipts = new ObservableCollection<ImportReceiptViewModel>();
                HasNoReceipts = true;
            }
        }
    }

    // ViewModel wrapper for ImportReceipt with expandable details
    public class ImportReceiptViewModel : BaseVM
    {
        private readonly ImportReceipt _receipt;
        private bool _isExpanded;
        private decimal _totalAmount;

        public ImportReceiptViewModel(ImportReceipt receipt)
        {
            _receipt = receipt;

            // Calculate total amount if not already set
            if (_receipt.ImportReceiptDetails != null && _receipt.ImportReceiptDetails.Any())
            {
                _totalAmount = _receipt.ImportReceiptDetails.Sum(d => d.Quantity * d.UnitPrice);

                // Create detail ViewModels with calculated subtotals
                ImportReceiptDetails = new ObservableCollection<ImportReceiptDetailViewModel>(
                    _receipt.ImportReceiptDetails.Select(d => new ImportReceiptDetailViewModel(d))
                );
            }
            else
            {
                _totalAmount = _receipt.TotalAmount;
                ImportReceiptDetails = new ObservableCollection<ImportReceiptDetailViewModel>();
            }
        }

        public string ReceiptID => _receipt.ReceiptID;
        public DateTime ImportDate => _receipt.ImportDate;

        public decimal TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(); }
        }

        public Employee Employee => _receipt.Employee;
        public Supplier Supplier => _receipt.Supplier;

        public ObservableCollection<ImportReceiptDetailViewModel> ImportReceiptDetails { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }
    }

    // ViewModel for ImportReceiptDetail with calculated subtotal
    public class ImportReceiptDetailViewModel : BaseVM
    {
        private Product _product;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _subtotal;

        public ImportReceiptDetailViewModel(ImportReceiptDetail detail)
        {
            _product = detail.Product;
            _quantity = detail.Quantity;
            _unitPrice = detail.UnitPrice;
            _subtotal = _quantity * _unitPrice;
        }

        public Product Product
        {
            get => _product;
            set { _product = value; OnPropertyChanged(); }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                Subtotal = _quantity * _unitPrice;
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                _unitPrice = value;
                OnPropertyChanged();
                Subtotal = _quantity * _unitPrice;
            }
        }

        public decimal Subtotal
        {
            get => _subtotal;
            set { _subtotal = value; OnPropertyChanged(); }
        }
    }
}