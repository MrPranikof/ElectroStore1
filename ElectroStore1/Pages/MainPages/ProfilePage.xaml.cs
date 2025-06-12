using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Data.SqlClient;
using System.Data;
using ElectroStore;

namespace ElectroStore1.Pages.MainPages
{
    public partial class ProfilePage : Page
    {
        public UserViewModel CurrentUser { get; set; }

        public ProfilePage(UserViewModel user)
        {
            try
            {
                Logger.Info("Инициализация страницы профиля");
                InitializeComponent();
                CurrentUser = user;
                DataContext = CurrentUser;
                LoadAvatar();
                Logger.Info("Страница профиля успешно инициализирована");
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при валидации данных: {ex}");
                throw;
            }
        }

        private void LoadAvatar()
        {
            try
            {
                Logger.Debug("Начало загрузки аватара пользователя");
                using (var connection = DBConnection.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT AvatarImage FROM Users WHERE UserId = @UserId";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", CurrentUser.UserId);

                        var imageData = cmd.ExecuteScalar() as byte[];
                        if (imageData != null && imageData.Length > 0)
                        {
                            Logger.Debug("Аватар найден в базе данных");
                            using (var stream = new MemoryStream(imageData))
                            {
                                var image = new BitmapImage();
                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.StreamSource = stream;
                                image.EndInit();
                                AvatarImage.Source = image;
                            }
                            Logger.Info("Аватар успешно загружен");
                        }
                        else
                        {
                            Logger.Debug("Аватар не найден в базе данных");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка загрузки аватара: {ex}");
                MessageBox.Show($"Ошибка загрузки аватара: {ex.Message}");
            }
        }

        private void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Начало процесса смены аватара");
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Image files (*.jpg, *.png)|*.jpg;*.png",
                    Title = "Выберите изображение профиля",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        Logger.Debug($"Выбран файл: {openFileDialog.FileName}");
                        var fileInfo = new FileInfo(openFileDialog.FileName);
                        if (fileInfo.Length > 2 * 1024 * 1024)
                        {
                            Logger.Warning("Попытка загрузить слишком большой файл");
                            MessageBox.Show("Файл слишком большой. Максимальный размер - 2MB.");
                            return;
                        }

                        byte[] imageData;
                        using (var fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                        {
                            imageData = new byte[fs.Length];
                            fs.Read(imageData, 0, (int)fs.Length);
                        }

                        using (var connection = DBConnection.GetConnection())
                        {
                            connection.Open();
                            string query = "UPDATE Users SET AvatarImage = @AvatarImage WHERE UserId = @UserId";

                            using (var cmd = new SqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                                cmd.Parameters.Add("@AvatarImage", SqlDbType.VarBinary, imageData.Length).Value = imageData;
                                int rowsAffected = cmd.ExecuteNonQuery();
                                Logger.Info($"Аватар обновлен в базе данных. Затронуто строк: {rowsAffected}");
                            }
                        }

                        using (var stream = new MemoryStream(imageData))
                        {
                            var image = new BitmapImage();
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.StreamSource = stream;
                            image.EndInit();
                            AvatarImage.Source = image;
                        }
                        Logger.Info("Аватар успешно изменен");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Ошибка при обновлении аватара: {ex}");
                        MessageBox.Show($"Ошибка при обновлении аватара: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка в процессе смены фото: {ex}");
                MessageBox.Show($"Ошибка при выборе файла: {ex.Message}");
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Начало сохранения профиля");
                if (!ValidateInputs())
                {
                    Logger.Warning("Валидация данных профиля не пройдена");
                    return;
                }

                using (var connection = DBConnection.GetConnection())
                {
                    connection.Open();
                    string query = @"
                    UPDATE Users 
                    SET 
                        Username = @Username,
                        Email = @Email,
                        FirstName = @FirstName,
                        LastName = @LastName,
                        Phone = @Phone,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender,
                        UpdatedAt = GETDATE()
                    WHERE UserId = @UserId";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                        cmd.Parameters.AddWithValue("@Username", CurrentUser.Username ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", CurrentUser.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FirstName", CurrentUser.FirstName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LastName", CurrentUser.LastName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", CurrentUser.Phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DateOfBirth", CurrentUser.DateOfBirth ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Gender", CurrentUser.Gender ?? (object)DBNull.Value);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        Logger.Info($"Профиль обновлен. Затронуто строк: {rowsAffected}");

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Профиль успешно сохранен!", "Успех",
                                         MessageBoxButton.OK, MessageBoxImage.Information);
                            Logger.Info("Профиль успешно сохранен");
                        }
                        else
                        {
                            Logger.Warning("Профиль не был обновлен. Возможно, пользователь не найден");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Logger.Error($"Ошибка базы данных при сохранении профиля: {ex}");
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Logger.Error($"Неизвестная ошибка при сохранении профиля {ex}");
                MessageBox.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка",
                             MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentUser.Username))
                {
                    Logger.Warning("Попытка сохранения с пустым логином");
                    MessageBox.Show("Логин не может быть пустым", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!string.IsNullOrEmpty(CurrentUser.Email) && !CurrentUser.Email.Contains("@"))
                {
                    Logger.Warning($"Некорректный email: {CurrentUser.Email}");
                    MessageBox.Show("Введите корректный email", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при валидации данных: {ex}");
                throw;
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Запрос на изменение пароля");
                var passwordWindow = new ChangePassword(CurrentUser.UserId);
                passwordWindow.Owner = Window.GetWindow(this);
                passwordWindow.ShowDialog();
                Logger.Info("Окно изменения пароля закрыто");
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при открытии окна смены пароля: {ex}");
                MessageBox.Show($"Ошибка при открытии окна смены пароля: {ex.Message}");
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Запрос на выход из системы");
                var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение выхода",
                               MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Logger.Info($"Пользователь {CurrentUser.Username} вышел из системы");
                    AuthService.ClearSavedData();

                    MainWindow loginWindow = new MainWindow();
                    loginWindow.Show();

                    Window.GetWindow(this).Close();
                }
                else
                {
                    Logger.Debug("Выход из системы отменен пользователем");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка при выходе из системы: {ex}");
                MessageBox.Show($"Ошибка при выходе из системы: {ex.Message}");
            }
        }
    }
}