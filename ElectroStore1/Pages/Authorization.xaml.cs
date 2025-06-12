using ElectroStore1.Pages;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Security.Cryptography;
using System.Net;
using System.Net.Http;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using ElectroStore;
using ElectroStore.Properties;

namespace ElectroStore1
{
    /// <summary>
    /// Логика взаимодействия для Authorization.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadSavedUsername();
        }
        private void LoadSavedUsername()
        {
            if (AuthService.HasSavedUsername())
            {
                LoginTextBox.Text = AuthService.GetSavedUsername();
                RememberMeCheckBox.IsChecked = true;
                hidePassword.Focus();
            }
        }

        private bool x = true;
        private void PasswordButtonImage(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            x = !x;

            if (x == false)
            {
                button.Content = new Image()
                {
                    Source = new BitmapImage(new Uri("/Images/hidepassword.png", UriKind.Relative))
                };
                showPassword.Text = hidePassword.Password;
                showPassword.Visibility = Visibility.Visible;
                hidePassword.Visibility = Visibility.Hidden;
            }
            else
            {
                button.Content = new Image()
                {
                    Source = new BitmapImage(new Uri("/Images/showpassword.png", UriKind.Relative))
                };
                hidePassword.Password = showPassword.Text;
                showPassword.Visibility = Visibility.Hidden;
                hidePassword.Visibility = Visibility.Visible;
            }
        }
        private void RegistNav(object sender, RoutedEventArgs e)
        {
            Registration nextWindow = new Registration();
            nextWindow.Show();
            this.Close();
        }

        private void ButtonEsc(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ButtonLogin(object sender, RoutedEventArgs e)
        {
            string userLogin = LoginTextBox.Text.Trim();
            string userPassword = hidePassword.Visibility == Visibility.Visible ?
                                hidePassword.Password :
                                showPassword.Text;

            if (string.IsNullOrWhiteSpace(userLogin) || string.IsNullOrWhiteSpace(userPassword))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    int userId = 0;
                    int roleId = 0;
                    string storedHash = null;

                    string userQuery = @"SELECT userId, RoleId, PasswordHash 
                       FROM Users 
                       WHERE username = @userLogin";

                    using (SqlCommand userCommand = new SqlCommand(userQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@userLogin", userLogin);

                        using (SqlDataReader reader = userCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userId = reader.GetInt32(0);
                                roleId = reader.GetInt32(1);
                                storedHash = reader["PasswordHash"].ToString();
                            }
                        }
                    }

                    if (storedHash != null && Hash.VerifyPassword(userPassword, storedHash))
                    {
                        bool rememberMe = RememberMeCheckBox.IsChecked ?? false;
                        if (rememberMe)
                        {
                            Settings.Default.SavedUsername = userLogin;
                            Settings.Default.SavedPasswordHash = storedHash;
                            Settings.Default.RememberMe = true;
                            Settings.Default.Save();

                            Logger.Info($"Сохранены данные для автовхода: {userLogin}");
                        }
                        else
                        {
                            AuthService.ClearSavedData();
                        }

                        string historyQuery = @"INSERT INTO LoginHistory 
                              (UserId, LoginTime, IPAddress) 
                              VALUES (@UserId, @LoginTime, @IPAddress)";

                        using (SqlCommand historyCommand = new SqlCommand(historyQuery, connection))
                        {
                            historyCommand.Parameters.AddWithValue("@UserId", userId);
                            historyCommand.Parameters.AddWithValue("@LoginTime", DateTime.Now);
                            historyCommand.Parameters.AddWithValue("@IPAddress", GetIPAddress());

                            historyCommand.ExecuteNonQuery();
                        }

                        Logger.Info($"Успешный вход: {userLogin}");

                        Main nextWindow = new Main(userId, roleId);
                        nextWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        Logger.Warning($"Неверные учетные данные для: {userLogin}");
                        MessageBox.Show("Неверный логин или пароль");
                        hidePassword.Clear();
                        showPassword.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при входе", ex);
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
        public static string GetIPAddress()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);

                    string IP = client.GetStringAsync("https://api.ipify.org").GetAwaiter().GetResult();
                    return IP;
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
        }

        private void GuestNav(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                string query = @"SELECT UserId, RoleId FROM Users WHERE Username = 'guest'";
                SqlCommand command = new SqlCommand(query, connection);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        int userId = (int)reader["userId"];
                        int RoleId = (int)reader["RoleId"];
                        MessageBox.Show("Вы вошли как гость.");
                        Main nextWindow = new Main(userId, RoleId);
                        nextWindow.Show();
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex}");
                }
            }
        }
    }
}