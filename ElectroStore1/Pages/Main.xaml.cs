using DocumentFormat.OpenXml.Spreadsheet;
using ElectroStore.Pages.MainPages;
using ElectroStore1.Pages.MainPages;
using ElectroStore1.ViewModels;
using ElectroStore1.Windows.Pages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
            MainFrame.Navigate(new MainPage(userId, roleId));
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
            MainFrame.Navigate(new MainPage(_userId, _roleId));
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }

        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AdminPanelPage());
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }
        private void CatalogButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Catalog(_userId, _roleId));
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }

        private void Search_Button(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();

            if (SearchTextBox.Text == "Поиск")
            {
                searchText = string.Empty;
            }

            MainFrame.Navigate(new SearchPage(searchText, _userId, _roleId));
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search_Button(sender, e);
            }
        }

        private void Basket_Button(object sender, RoutedEventArgs e)
        {
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                return;
            }
            MainFrame.Navigate(new BasketPage(_userId));
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }
        private void Favourites_Button(object sender, RoutedEventArgs e)
        {
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                return;
            }
            MainFrame.Navigate(new FavouritesPage(_userId, _roleId));
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }
        private void Orders_Button(object sender, RoutedEventArgs e)
        {
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                return;
            }
            MainFrame.Navigate(new OrdersPage(_userId));
            if (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }
        private void Profile_Button(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_roleId == 4)
                {
                    string message = "Гостевой доступ ограничен.\nЧтобы получить полный доступ к профилю и другим функциям, необходимо зарегистрироваться.";
                    string caption = "Требуется регистрация";

                    var result = MessageBox.Show(message, caption,
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Question,
                                              MessageBoxResult.No);

                    if (result == MessageBoxResult.Yes)
                    {
                        var registration = new Registration();
                        registration.Show();
                        this.Close();
                    }
                    return;
                }

                var user = GetUserFromDatabase(_userId);
                MainFrame.Navigate(new ProfilePage(user));
                if (MainFrame.CanGoBack)
                {
                    MainFrame.RemoveBackEntry();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть профиль: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UserViewModel GetUserFromDatabase(int userId)
        {
            try
            {
                using (var connection = DBConnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    UserId, Username, Email, 
                    FirstName, LastName, Phone, 
                    DateOfBirth, Gender, RoleId,
                    LastLoginDate, CreatedAt, UpdatedAt
                FROM Users 
                WHERE UserId = @UserId";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var user = new UserViewModel
                                {
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString(reader.GetOrdinal("Username")),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                                    FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                                    DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                                    Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? null : reader.GetString(reader.GetOrdinal("Gender")),
                                    RoleId = reader.IsDBNull(reader.GetOrdinal("RoleId")) ? 0 : reader.GetInt32(reader.GetOrdinal("RoleId")),
                                    LastLoginDate = reader.IsDBNull(reader.GetOrdinal("LastLoginDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastLoginDate")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                                };

                                // Получаем название роли отдельным запросом
                                if (user.RoleId > 0)
                                {
                                    user.RoleName = GetRoleName(user.RoleId);
                                }

                                return user;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при загрузке пользователя: {ex.Message}");
            }

            throw new Exception("Пользователь не найден");
        }

        private string GetRoleName(int roleId)
        {
            using (var connection = DBConnection.GetConnection())
            {
                connection.Open();
                string query = "SELECT RoleName FROM Roles WHERE RoleId = @RoleId";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@RoleId", roleId);
                    return cmd.ExecuteScalar()?.ToString() ?? "Не указана";
                }
            }
        }

        private void ShowGuestRestrictionMessage()
        {
            string message = "Гостевой доступ ограничен.\nЧтобы получить полный доступ к этой функции, необходимо зарегистрироваться.";
            string caption = "Требуется регистрация";

            var result = MessageBox.Show(message, caption,
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Question,
                                      MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                var registration = new Registration();
                registration.Show();
                this.Close();
            }
        }
    }
}