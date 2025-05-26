using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElectroStore1.Pages.MainPages
{
    /// <summary>
    /// Логика взаимодействия для AdminPanelPage.xaml
    /// </summary>
    public partial class AdminPanelPage : Page
    {
        public AdminPanelPage()
        {
            InitializeComponent();
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            AddProduct nextWindow = new AddProduct();
            nextWindow.Show();
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            AddCategory nextWindow = new AddCategory();
            nextWindow.Show();
        }

        private void AddBrand_Click(object sender, RoutedEventArgs e)
        {
            AddBrand nextWindow = new AddBrand();
            nextWindow.Show();
        }
    }
}
