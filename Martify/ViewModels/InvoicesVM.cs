using Martify.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Martify.ViewModels
{
    public class InvoicesVM : BaseVM
    {
        private ObservableCollection<Invoice> _invoices;
        public ObservableCollection<Invoice> Invoices { get => _invoices; set { _invoices = value; OnPropertyChanged(); } }

        private Invoice _selectedInvoice;
        public Invoice SelectedInvoice { get => _selectedInvoice; set { _selectedInvoice = value; OnPropertyChanged(); } }

        private bool _isDetailsPanelOpen;
        public bool IsDetailsPanelOpen { get => _isDetailsPanelOpen; set { _isDetailsPanelOpen = value; OnPropertyChanged(); } }

        private string _keyword;
        public string Keyword { get => _keyword; set { _keyword = value; OnPropertyChanged(); LoadList(); } }

        private int? _selectedMonth;
        public int? SelectedMonth { get => _selectedMonth; set { _selectedMonth = value; OnPropertyChanged(); LoadList(); } }

        private int? _selectedYear;
        public int? SelectedYear { get => _selectedYear; set { _selectedYear = value; OnPropertyChanged(); LoadList(); } }

        public ObservableCollection<int> Months { get; set; } = new ObservableCollection<int>();
        public ObservableCollection<int> Years { get; set; } = new ObservableCollection<int>();

        public ICommand ClearFilterCommand { get; set; }
        public ICommand OpenDetailsCommand { get; set; }

        public InvoicesVM()
        {
            InitFilterData();
            LoadList();

            OpenDetailsCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                if (p is Invoice inv)
                {
                    // Load chi tiết hóa đơn (kèm thông tin sản phẩm và nhân viên)
                    var fullInvoice = DataProvider.Ins.DB.Invoices
                        .Include(x => x.Employee) // Load nhân viên tạo
                        .Include(x => x.InvoiceDetails) // Load chi tiết
                        .ThenInclude(d => d.Product) // Load sản phẩm trong chi tiết
                        .AsNoTracking()
                        .FirstOrDefault(x => x.InvoiceID == inv.InvoiceID);

                    SelectedInvoice = fullInvoice;
                    IsDetailsPanelOpen = true;
                }
            });

            ClearFilterCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Keyword = string.Empty;
                SelectedMonth = null;
                SelectedYear = null;
                IsDetailsPanelOpen = false;
                LoadList();
            });
        }

        void InitFilterData()
        {
            Months.Clear(); for (int i = 1; i <= 12; i++) Months.Add(i);
            Years.Clear();
            var dbYears = DataProvider.Ins.DB.Invoices.Select(x => x.CreatedDate.Year).Distinct().OrderByDescending(y => y).ToList();
            foreach (var y in dbYears) Years.Add(y);
            if (Years.Count == 0) Years.Add(DateTime.Now.Year);
        }

        void LoadList()
        {
            var query = DataProvider.Ins.DB.Invoices.Include(x => x.Employee).AsNoTracking().AsQueryable();

            if (SelectedMonth.HasValue) query = query.Where(x => x.CreatedDate.Month == SelectedMonth.Value);
            if (SelectedYear.HasValue) query = query.Where(x => x.CreatedDate.Year == SelectedYear.Value);

            var list = query.OrderByDescending(x => x.CreatedDate).ToList();

            if (!string.IsNullOrEmpty(Keyword))
            {
                string k = Keyword.ToLower();
                list = list.Where(x => x.InvoiceID.ToLower().Contains(k) ||
                                       (x.Employee != null && x.Employee.FullName.ToLower().Contains(k))).ToList();
            }
            Invoices = new ObservableCollection<Invoice>(list);
        }
    }
}