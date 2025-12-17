using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Martify.Models
{
    public class SelectableProduct : INotifyPropertyChanged
    {
        private bool _isSelected;

        public Product Product { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public SelectableProduct(Product product)
        {
            Product = product;
            IsSelected = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
