using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

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

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Окно продукты успешно открыто");
                Product nextWindow = new Product();
                nextWindow.Show();
            }
            catch(Exception ex)
            {
                Logger.Error($"Ошибка: {ex}");
            }
        }

        private void Category_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Окно категории успешно открыто");
                Category nextWindow = new Category();
                nextWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка: {ex}");
            }
        }

        private void Brand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Окно бренда успешно открыто");
                Brand nextWindow = new Brand();
                nextWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка: {ex}");
            }
        }
        private void Users_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Окно пользователей успешно открыто");
                Users nextWindow = new Users();
                nextWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка: {ex}");
            }
        }
        private void Logger_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Папка логов успешно открыта");
                string LogFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                System.Diagnostics.Process.Start(LogFolder);
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка: {ex}");
            }

        }

        private void Carts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Окно корзины успешно открыто");
                Carts nextWindow = new Carts();
                nextWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка: {ex}");
            }
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Окно заказов успешно открыто");
                Orders nextWindow = new Orders();
                nextWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка: {ex}");
            }
        }
    }
}
