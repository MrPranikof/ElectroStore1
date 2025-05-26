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
using ElectroStore1.Pages.MainPages;

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
            MainFrame.Navigate(new MainPage());
        }
        private void SetupUI()
        {
            if (_roleId != 1)
            {
                GridMenu.Children.Remove(AdminPanelButton);
                AdminCol.Width = new GridLength(0);
            }
        }
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск")
                SearchTextBox.Text = "";
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                SearchTextBox.Text = "Поиск";
        }

        private void GoToMain_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MainPage());
        }

        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AdminPanelPage());
        }
        private void CatalogButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Catalog());
        }

        private void Search_Button(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SearchPage());
        }

        private void Basket_Button(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BasketPage());
        }
        private void Favourites_Button(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new FavouritesPage());
        }
        private void Delivery_Button(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DeliveryPage());
        }
        private void Profile_Button(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage());
        }
    }
}