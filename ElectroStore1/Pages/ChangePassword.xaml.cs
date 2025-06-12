using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ElectroStore1.Pages
{
    public partial class ChangePassword : Window
    {
        private readonly int _userId;
        private bool _isPasswordVisible = true;

        public ChangePassword(int userId)
        {
            try
            {
                InitializeComponent();
                _userId = userId;
                Logger.Info($"Окно смены пароля открыто для пользователя ID: {_userId}");

                InitializeToggleButtons();
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при инициализации окна смены пароля: {ex}");
                throw;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                using (var connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    if (!VerifyCurrentPassword(connection))
                        return;

                    UpdatePassword(connection);
                }
            }
            catch (SqlException ex)
            {
                Logger.Error($"Ошибка базы данных: {ex}");
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Logger.Error($"Неизвестная ошибка: {ex}");
                MessageBox.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(OldPasswordBox.Text))
            {
                Logger.Warning("Не введен текущий пароль");
                MessageBox.Show("Введите текущий пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (NewPasswordBox.Password.Length < 6)
            {
                Logger.Warning("Слишком короткий пароль");
                MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                Logger.Warning("Пароли не совпадают");
                MessageBox.Show("Новые пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool VerifyCurrentPassword(SqlConnection connection)
        {
            string query = "SELECT PasswordHash FROM Users WHERE UserId = @UserId";
            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", _userId);
                var currentHash = cmd.ExecuteScalar()?.ToString();

                if (string.IsNullOrEmpty(currentHash) || !Hash.VerifyPassword(OldPasswordBox.Text, currentHash))
                {
                    Logger.Warning("Неверный текущий пароль");
                    MessageBox.Show("Неверный текущий пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                return true;
            }
        }

        private void UpdatePassword(SqlConnection connection)
        {
            string query = "UPDATE Users SET PasswordHash = @NewHash WHERE UserId = @UserId";
            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", _userId);
                cmd.Parameters.AddWithValue("@NewHash", Hash.HashPassword(NewPasswordBox.Password));

                if (cmd.ExecuteNonQuery() > 0)
                {
                    Logger.Info("Пароль успешно изменен");
                    MessageBox.Show("Пароль успешно изменен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    Logger.Error("Пароль не изменен");
                    MessageBox.Show("Не удалось изменить пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Смена пароля отменена");
            DialogResult = false;
            Close();
        }

        private bool _isNewPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

        private void InitializeToggleButtons()
        {
            try
            {
                ToggleNewPasswordButton.Content = new Image()
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/showpassword.png"))
                };

                ToggleConfirmPasswordButton.Content = new Image()
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/showpassword.png"))
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при инициализации кнопок переключения: {ex}");
            }
        }
        private void ToggleNewPasswordVisibility(object sender, RoutedEventArgs e)
        {
            try
            {
                _isNewPasswordVisible = !_isNewPasswordVisible;

                if (_isNewPasswordVisible)
                {
                    // Показываем пароль
                    NewPasswordTextBox.Text = NewPasswordBox.Password;
                    ToggleNewPasswordButton.Content = new Image()
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Images/hidepassword.png"))
                    };
                    NewPasswordTextBox.Visibility = Visibility.Visible;
                    NewPasswordBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Скрываем пароль
                    NewPasswordBox.Password = NewPasswordTextBox.Text;
                    ToggleNewPasswordButton.Content = new Image()
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Images/showpassword.png"))
                    };
                    NewPasswordBox.Visibility = Visibility.Visible;
                    NewPasswordTextBox.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка переключения видимости нового пароля: {ex}");
                MessageBox.Show("Ошибка при переключении видимости пароля", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleConfirmPasswordVisibility(object sender, RoutedEventArgs e)
        {
            try
            {
                _isConfirmPasswordVisible = !_isConfirmPasswordVisible;

                if (_isConfirmPasswordVisible)
                {
                    // Показываем пароль
                    ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                    ToggleConfirmPasswordButton.Content = new Image()
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Images/hidepassword.png"))
                    };
                    ConfirmPasswordTextBox.Visibility = Visibility.Visible;
                    ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Скрываем пароль
                    ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                    ToggleConfirmPasswordButton.Content = new Image()
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Images/showpassword.png"))
                    };
                    ConfirmPasswordBox.Visibility = Visibility.Visible;
                    ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка переключения видимости подтверждения пароля: {ex}");
                MessageBox.Show("Ошибка при переключении видимости пароля", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}