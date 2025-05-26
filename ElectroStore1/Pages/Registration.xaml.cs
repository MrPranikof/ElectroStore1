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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Text.RegularExpressions;

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
            string userLogin = LoginTextBox.Text.Trim();
            string userEmail = EmailTextBox.Text.Trim();
            string userPassword = null;

            if (hidePassword.Visibility == Visibility.Visible)
                userPassword = hidePassword.Password.Trim();
            if (showPassword.Visibility == Visibility.Visible)
                userPassword = showPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(userLogin) || string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(userPassword))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            if (!Regex.IsMatch(userEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Некорректный формат email.");
                return;
            }

            using (SqlConnection conn = DBConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    string checkUsers = @"SELECT COUNT(*) FROM Users WHERE Username = @userLogin OR Email = @userEmail";
                    SqlCommand checkCommand = new SqlCommand(checkUsers, conn);

                    checkCommand.Parameters.AddWithValue("@userLogin", userLogin);
                    checkCommand.Parameters.AddWithValue("@userEmail", userEmail);

                    int existingCount = (int)checkCommand.ExecuteScalar();

                    if (existingCount > 0)
                    {
                        MessageBox.Show("Пользователь с таким логином или почтой уже существует.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при проверке пользователя: {ex.Message}");
                    return;
                }
            }

            string hashedPassword = Hash.HashPassword(userPassword);

            using (SqlConnection conn = DBConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    string insertQuery = @"INSERT INTO Users (Username, Email, PasswordHash, RoleId) 
                                   VALUES (@userLogin, @userEmail, @hashedPassword, 3)";

                    SqlCommand insertCommand = new SqlCommand(insertQuery, conn);
                    insertCommand.Parameters.AddWithValue("@userLogin", userLogin);
                    insertCommand.Parameters.AddWithValue("@userEmail", userEmail);
                    insertCommand.Parameters.AddWithValue("@hashedPassword", hashedPassword);

                    insertCommand.ExecuteNonQuery();
                    MessageBox.Show("Вы успешно зарегистрировались!");

                    MainWindow nextWindow = new MainWindow();
                    nextWindow.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при регистрации: {ex.Message}");
                }
            }
        }
    }
}
