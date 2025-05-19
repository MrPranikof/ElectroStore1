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
            LoadData();
        }

        public void LoadData()
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();
                    SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM Users", connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    DG_Client.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
            string userLogin = LoginTextBox.Text;
            string userPassword = null;
            if (hidePassword.Visibility == Visibility.Visible)
                userPassword = hidePassword.Password;
            if (showPassword.Visibility == Visibility.Visible)
                userPassword = showPassword.Text;
            string HashedPassword = Hash.HashPassword(userPassword);

            using (SqlConnection connection = DBConnection.GetConnection())
            {
                string query = @"SELECT userId, RoleId FROM Users WHERE username = @userLogin AND PasswordHash = @HashedPassword";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@userLogin", userLogin);
                command.Parameters.AddWithValue("@HashedPassword", HashedPassword);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        int userId = (int)reader["userId"];
                        int RoleId = (int)reader["RoleId"];

                        MessageBox.Show("Вы успешно авторизовались");
                        Main nextWindow = new Main(userId, RoleId);
                        nextWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль");
                    }
                    reader.Close();
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex}");
                }
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