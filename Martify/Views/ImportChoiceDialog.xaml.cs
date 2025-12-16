using System.Windows;
using System.Windows.Input;

namespace Martify.Views
{
    /// <summary>
    /// Interaction logic for ImportChoiceDialog.xaml
    /// </summary>
    public partial class ImportChoiceDialog : Window
    {
        public enum ImportChoice
        {
            None,
            AddNewProduct,
            ImportProducts
        }

        public ImportChoice SelectedChoice { get; private set; } = ImportChoice.None;

        public ImportChoiceDialog()
        {
            InitializeComponent();
        }

        private void AddNewProduct_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedChoice = ImportChoice.AddNewProduct;
            DialogResult = true;
            Close();
        }

        private void ImportProducts_Click(object sender, MouseButtonEventArgs e)
        {
            SelectedChoice = ImportChoice.ImportProducts;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedChoice = ImportChoice.None;
            DialogResult = false;
            Close();
        }
    }
}
