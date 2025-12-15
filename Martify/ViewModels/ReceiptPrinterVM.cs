using Martify.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Martify.ViewModels
{
    public class ReceiptPrinterVM : BaseVM
    {
        private Invoice _invoice;
        public Invoice CurrentInvoice
        {
            get => _invoice;
            set { _invoice = value; OnPropertyChanged(); }
        }

        public ICommand CloseCommand { get; set; }
        // Command này sẽ được gọi từ Code-behind sau khi Animation chạy xong hoặc khi bấm nút
        public ICommand SaveImageCommand { get; set; }

        public ReceiptPrinterVM(Invoice invoice)
        {
            CurrentInvoice = invoice;
            CloseCommand = new RelayCommand<Window>((w) => w != null, (w) => w.Close());
        }
    }
}