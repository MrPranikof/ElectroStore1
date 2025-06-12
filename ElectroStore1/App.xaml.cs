using DocumentFormat.OpenXml.Drawing.Charts;
using ElectroStore;
using ElectroStore.Properties;
using ElectroStore1.LoaderFolder;
using ElectroStore1.Pages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ElectroStore1
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Logger.WriteSeparator("ЗАПУСК ПРИЛОЖЕНИЯ");
            Logger.Info($"Версия приложения: {Assembly.GetExecutingAssembly().GetName().Version}");
            Logger.Info($"Пользователь: {Environment.UserName}");
            Logger.Info($"Компьютер: {Environment.MachineName}");
            Logger.Info($"ОС: {Environment.OSVersion}");

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Logger.Error("КРИТИЧЕСКАЯ ОШИБКА ПРИЛОЖЕНИЯ", args.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                Logger.Error("Необработанное исключение UI", args.Exception);
                MessageBox.Show(
                    "Произошла ошибка. Подробности сохранены в логах.\n" +
                    $"Файл: {Path.Combine("Logs", $"log_{DateTime.Now:yyyy-MM-dd}.txt")}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            LoaderWindow loader = new LoaderWindow();
            loader.Show();

            bool autoLoginSuccess = false;
            int userId = 0;
            int roleId = 0;

            if (Settings.Default.RememberMe &&
                !string.IsNullOrEmpty(Settings.Default.SavedUsername) &&
                !string.IsNullOrEmpty(Settings.Default.SavedPasswordHash))
            {
                var autoLoginResult = await TryAutoLogin();
                autoLoginSuccess = autoLoginResult.success;
                userId = autoLoginResult.userId;
                roleId = autoLoginResult.roleId;
            }

            for (int i = 0; i <= 100; i++)
            {
                await Task.Delay(30);
                loader.progressFill.Value = i;

                switch (i)
                {
                    case 5:
                        loader.tbStatus.Text = "Будим разработчиков...";
                        break;
                    case 15:
                        loader.tbStatus.Text = "Находим кофе...";
                        break;
                    case 25:
                        loader.tbStatus.Text = "Загружаем котиков для настроения...";
                        break;
                    case 40:
                        loader.tbStatus.Text = "Чиним космические кабели...";
                        break;
                    case 50:
                        loader.tbStatus.Text = "Запускаем квантовые вычисления...";
                        break;
                    case 80:
                        loader.tbStatus.Text = "Последние штрихи...";
                        break;
                    case 95:
                        loader.tbStatus.Text = "Запуск...";
                        break;
                }
            }
            if (autoLoginSuccess)
            {
                Main mainWindow = new Main(userId, roleId);
                mainWindow.Show();
                loader.Close();
            }
            else
            {
                MainWindow authWindow = new MainWindow();
                authWindow.Show();
                loader.Close();
            }
        }

        private async Task<(bool success, int userId, int roleId)> TryAutoLogin()
        {
            string username = AuthService.GetSavedUsername();
            string savedPasswordHash = Settings.Default.SavedPasswordHash;

            if (string.IsNullOrEmpty(savedPasswordHash))
            {
                Logger.Warning("Нет сохранённого хеша пароля для автоматического входа");
                return (false, 0, 0);
            }

            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    await connection.OpenAsync();

                    string query = @"SELECT userId, RoleId, PasswordHash FROM Users WHERE username = @userLogin";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userLogin", username);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string dbPasswordHash = reader["PasswordHash"].ToString();

                                if (dbPasswordHash == savedPasswordHash)
                                {
                                    int userId = reader.GetInt32(0);
                                    int roleId = reader.GetInt32(1);

                                    reader.Close();

                                    string ipAddress = GetIPAddress();

                                    string sql = @"INSERT INTO LoginHistory (UserId, LoginTime, IPAddress) 
                                                  VALUES (@UserId, @LoginTime, @IPAddress)";
                                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                                    {
                                        cmd.Parameters.AddWithValue("@UserId", userId);
                                        cmd.Parameters.AddWithValue("@LoginTime", DateTime.Now);
                                        cmd.Parameters.AddWithValue("@IPAddress", ipAddress);
                                        await cmd.ExecuteNonQueryAsync();
                                    }

                                    Logger.Info($"Успешный автоматический вход: {username}");
                                    return (true, userId, roleId);
                                }
                                else
                                {
                                    Logger.Warning("Сохранённый хеш не совпадает с текущим");
                                    ClearPasswordData();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка автоматического входа", ex);
                ClearPasswordData();
            }

            return (false, 0, 0);
        }

        private void ClearPasswordData()
        {
            Settings.Default.SavedPasswordHash = string.Empty;
            Settings.Default.Save();
        }

        private static string GetIPAddress()
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

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.WriteSeparator("ЗАВЕРШЕНИЕ РАБОТЫ");
            Logger.Info("Приложение закрыто");
            base.OnExit(e);
        }
    }
}