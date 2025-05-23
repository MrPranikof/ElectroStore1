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
using System.Windows.Shapes;
using System.Data.SqlClient;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Data;
using System.Data.Common;
using ElectroStore1.Windows.Pages;

namespace ElectroStore1.Pages
{
    public partial class Main : Window
    {
        private int _userId;
        private int _roleId;
        public Main(int userId, int roleId)
        {
            InitializeComponent();
            _userId = userId;
            _roleId = roleId;
            SetupUI();
        }
        private void SetupUI()
        {
            if (_roleId != 1)
            {
                AdminPanelButton.Visibility = Visibility.Collapsed;
            }
        }
        private void CatalogButton_Enter(object sender, RoutedEventArgs e)
        {
            canvasCatalog.Visibility = Visibility.Visible;
        }
        private void FavouritesButton_Enter(object sender, RoutedEventArgs e)
        {
            canvasFavourites.Visibility = Visibility.Visible;
        }
        private void BasketButton_Enter(object sender, RoutedEventArgs e)
        {
            canvasBasket.Visibility = Visibility.Visible;
        }
        private void AdminPanelButton_Enter(object sender, RoutedEventArgs e)
        {
            canvasAdminPanel.Visibility = Visibility.Visible;
        }

        private void UIElement_Leave(object sender, RoutedEventArgs e)
        {
            if (!IsMouseOverPanel())
            {
                HideAllPanels();
            }
        }

        private void Panel_MouseEnter(object sender, RoutedEventArgs e)
        {
        }

        private void Panel_MouseLeave(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
        }

        private bool IsMouseOverPanel()
        {
            return canvasCatalog.IsMouseOver ||
                   canvasFavourites.IsMouseOver ||
                   canvasBasket.IsMouseOver ||
                   canvasAdminPanel.IsMouseOver;
        }

        private void HideAllPanels()
        {
            canvasCatalog.Visibility = Visibility.Collapsed;
            canvasFavourites.Visibility = Visibility.Collapsed;
            canvasBasket.Visibility = Visibility.Collapsed;
            canvasAdminPanel.Visibility = Visibility.Collapsed;
        }
        private void CatalogButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Catalog());
        }
    }
}