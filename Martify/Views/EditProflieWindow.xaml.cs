using System.Windows;
using System.Windows.Input;

namespace Martify.Views
{
    /// <summary>
    /// Interaction logic for EditProfileWindow.xaml
    /// </summary>
    public partial class EditProfileWindow : Window // [QUAN TRỌNG] Phải kế thừa từ Window
    {
        public EditProfileWindow()
        {
            InitializeComponent();
        }

        // [QUAN TRỌNG] Đây là hàm còn thiếu gây ra lỗi của bạn
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}