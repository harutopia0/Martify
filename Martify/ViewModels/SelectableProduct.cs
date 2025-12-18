using Martify.ViewModels;

using Martify.Models;

namespace Martify.ViewModels
{
    public class SelectableProduct : BaseVM
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
    }
}
