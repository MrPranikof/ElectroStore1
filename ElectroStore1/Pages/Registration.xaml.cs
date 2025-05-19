using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
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
using System.Security.Cryptography;
using System.Security.Policy;
using System.Windows.Media.Animation;

namespace ElectroStore1.Pages
{
    /// <summary>
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Window
    {
        public Registration()
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
        private void ButtonEsc(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LogNav(object sender, RoutedEventArgs e)
        {
            MainWindow nextWindow = new MainWindow();
            nextWindow.Show();
            this.Close();
        }

        private void ButtonReg(object sender, RoutedEventArgs e)
        {
            string userLogin = LoginTextBox.Text;
            string userEmail = EmailTextBox.Text;
            string userPassword = null;
            if (hidePassword.Visibility == Visibility.Visible)
                userPassword = hidePassword.Password;
            if (showPassword.Visibility == Visibility.Visible)
                userPassword = showPassword.Text;

            string HashedPassword = Hash.HashPassword(userPassword);

            using (SqlConnection connection = DBConnection.GetConnection())
            {
                string query = @"INSERT INTO Users (Username, Email, PasswordHash, RoleId)" + "VALUES (@userLogin, @userEmail, @HashedPassword, 3)";
                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@userLogin", userLogin);
                command.Parameters.AddWithValue("@userEmail", userEmail);
                command.Parameters.AddWithValue("@HashedPassword", HashedPassword);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    MessageBox.Show("Вы успешно зарегестрировались");

                    MainWindow nextWindow = new MainWindow();
                    nextWindow.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex}");
                }
            }    
        }
    }
}
