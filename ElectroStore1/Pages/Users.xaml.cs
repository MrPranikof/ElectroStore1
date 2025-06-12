using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Xceed.Document.NET;
using Xceed.Words.NET;
using System.Data.SqlClient;
using Microsoft.Win32;
using System.IO;
using Alignment = Xceed.Document.NET.Alignment;

namespace ElectroStore1.Pages
{
    public partial class Users : Window
    {
        private ObservableCollection<UserViewModel> _users;
        private List<RoleViewModel> _roles;

        public Users()
        {
            InitializeComponent();
            _users = new ObservableCollection<UserViewModel>();
            UserDG.ItemsSource = _users;
            LoadRoles();
            LoadUsers();
        }

        private void LoadRoles()
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    _roles = new List<RoleViewModel>();
                    string query = "SELECT RoleId, RoleName FROM Roles ORDER BY RoleName";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _roles.Add(new RoleViewModel
                            {
                                RoleId = reader.GetInt32(0),
                                RoleName = reader.GetString(1)
                            });
                        }
                    }

                    var roleColumn = UserDG.Columns.OfType<DataGridComboBoxColumn>()
                        .FirstOrDefault(c => c.Header.ToString() == "РОЛЬ");
                    if (roleColumn != null) roleColumn.ItemsSource = _roles;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}");
            }
        }

        public void LoadUsers()
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT 
                            u.UserId, u.Username, u.Email, 
                            u.FirstName, u.LastName, u.RoleId,
                            u.DateOfBirth, u.Gender, u.Phone,
                            u.LastLoginDate, u.CreatedAt, u.UpdatedAt,
                            r.RoleName
                        FROM Users u
                        LEFT JOIN Roles r ON u.RoleId = r.RoleId";

                    _users.Clear();

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _users.Add(new UserViewModel
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                                FirstName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                LastName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                RoleId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                DateOfBirth = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                Gender = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Phone = reader.IsDBNull(8) ? null : reader.GetString(8),
                                LastLoginDate = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9),
                                CreatedAt = reader.GetDateTime(10),
                                UpdatedAt = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11),
                                RoleName = reader.IsDBNull(12) ? "Не указана" : reader.GetString(12)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                UserDG.ItemsSource = _users;
            }
            else
            {
                var filtered = _users
                    .Where(u => (u.Username?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (u.Email?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (u.FirstName?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (u.LastName?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (u.Phone?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
                UserDG.ItemsSource = filtered;
            }
        }

        private void EditSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserDG.SelectedItem is UserViewModel selectedUser)
            {
                try
                {
                    using (SqlConnection connection = DBConnection.GetConnection())
                    {
                        connection.Open();

                        string updateQuery = @"
                    UPDATE Users 
                    SET 
                        Username = @Username,
                        Email = @Email,
                        FirstName = @FirstName,
                        LastName = @LastName,
                        RoleId = @RoleId,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender,
                        Phone = @Phone,
                        UpdatedAt = GETDATE()
                    WHERE UserId = @UserId";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@UserId", selectedUser.UserId);
                            cmd.Parameters.AddWithValue("@Username", (object)selectedUser.Username ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Email", (object)selectedUser.Email ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FirstName", (object)selectedUser.FirstName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LastName", (object)selectedUser.LastName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RoleId", selectedUser.RoleId > 0 ? (object)selectedUser.RoleId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateOfBirth", (object)selectedUser.DateOfBirth ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Gender", (object)selectedUser.Gender ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Phone", (object)selectedUser.Phone ?? DBNull.Value);

                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Изменения сохранены!", "Успех",
                                             MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadUsers(); // Обновляем данные
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                 MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для редактирования", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserDG.SelectedItem is UserViewModel selectedUser)
            {
                var result = MessageBox.Show(
                    $"Удалить пользователя '{selectedUser.Username ?? "без логина"}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection connection = DBConnection.GetConnection())
                        {
                            connection.Open();

                            // Сначала удаляем связанные записи в LoginHistory
                            string deleteHistoryQuery = "DELETE FROM LoginHistory WHERE UserId = @UserId";
                            using (SqlCommand cmd = new SqlCommand(deleteHistoryQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@UserId", selectedUser.UserId);
                                cmd.ExecuteNonQuery();
                            }

                            // Затем удаляем самого пользователя
                            string deleteUserQuery = "DELETE FROM Users WHERE UserId = @UserId";
                            using (SqlCommand cmd = new SqlCommand(deleteUserQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@UserId", selectedUser.UserId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        _users.Remove(selectedUser);
                        MessageBox.Show("Пользователь успешно удален!", "Успех",
                                     MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                                     MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = true;
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Пользователи_{DateTime.Now:yyyy-MM-dd}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Пользователи");

                        // Заголовки
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Логин";
                        worksheet.Cell(1, 3).Value = "Email";
                        worksheet.Cell(1, 4).Value = "Имя";
                        worksheet.Cell(1, 5).Value = "Фамилия";
                        worksheet.Cell(1, 6).Value = "Роль";
                        worksheet.Cell(1, 7).Value = "Дата рождения";
                        worksheet.Cell(1, 8).Value = "Пол";
                        worksheet.Cell(1, 9).Value = "Телефон";
                        worksheet.Cell(1, 10).Value = "Последний вход";
                        worksheet.Cell(1, 11).Value = "Создан";
                        worksheet.Cell(1, 12).Value = "Обновлен";

                        var headerRange = worksheet.Range(1, 1, 1, 12);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Данные
                        int row = 2;
                        foreach (var user in _users)
                        {
                            worksheet.Cell(row, 1).Value = user.UserId;
                            worksheet.Cell(row, 2).Value = user.Username ?? "Не указано";
                            worksheet.Cell(row, 3).Value = user.Email ?? "Не указано";
                            worksheet.Cell(row, 4).Value = user.FirstName ?? "Не указано";
                            worksheet.Cell(row, 5).Value = user.LastName ?? "Не указано";
                            worksheet.Cell(row, 6).Value = user.RoleName;
                            worksheet.Cell(row, 7).Value = user.DateOfBirth?.ToString("dd.MM.yyyy") ?? "Не указано";
                            worksheet.Cell(row, 8).Value = user.Gender ?? "Не указано";
                            worksheet.Cell(row, 9).Value = user.Phone ?? "Не указано";
                            worksheet.Cell(row, 10).Value = user.LastLoginDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не входил";
                            worksheet.Cell(row, 11).Value = user.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                            worksheet.Cell(row, 12).Value = user.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Не обновлялся";
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveDialog.FileName);
                    }

                    MessageBox.Show("Экспорт в Excel выполнен успешно!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCSV_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv",
                    FileName = $"Пользователи_{DateTime.Now:yyyy-MM-dd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("ID;Логин;Email;Имя;Фамилия;Роль;Дата рождения;Пол;Телефон;Последний вход;Создан;Обновлен");

                    foreach (var user in _users)
                    {
                        csv.AppendLine(
                            $"{user.UserId};" +
                            $"{user.Username ?? "Не указано"};" +
                            $"{user.Email ?? "Не указано"};" +
                            $"{user.FirstName ?? "Не указано"};" +
                            $"{user.LastName ?? "Не указано"};" +
                            $"{user.RoleName};" +
                            $"{(user.DateOfBirth.HasValue ? user.DateOfBirth.Value.ToString("dd.MM.yyyy") : "Не указано")};" +
                            $"{user.Gender ?? "Не указано"};" +
                            $"{user.Phone ?? "Не указано"};" +
                            $"{(user.LastLoginDate.HasValue ? user.LastLoginDate.Value.ToString("dd.MM.yyyy HH:mm") : "Не входил")};" +
                            $"{user.CreatedAt:dd.MM.yyyy HH:mm};" +
                            $"{(user.UpdatedAt.HasValue ? user.UpdatedAt.Value.ToString("dd.MM.yyyy HH:mm") : "Не обновлялся")}");
                    }

                    File.WriteAllText(saveDialog.FileName, csv.ToString(), Encoding.UTF8);

                    MessageBox.Show("Экспорт в CSV выполнен успешно!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в CSV: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToWord_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word документы (*.docx)|*.docx",
                    FileName = $"Пользователи_{DateTime.Now:yyyy-MM-dd}.docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var doc = DocX.Create(saveDialog.FileName))
                    {
                        doc.InsertParagraph("Отчет по пользователям")
                           .FontSize(20)
                           .Bold()
                           .Alignment = Alignment.center;

                        doc.InsertParagraph($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm}")
                           .FontSize(12)
                           .Alignment = Alignment.center;

                        doc.InsertParagraph();

                        var table = doc.AddTable(_users.Count + 1, 12);
                        table.Design = TableDesign.ColorfulList;

                        // Заголовки таблицы
                        table.Rows[0].Cells[0].Paragraphs[0].Append("ID").Bold();
                        table.Rows[0].Cells[1].Paragraphs[0].Append("Логин").Bold();
                        table.Rows[0].Cells[2].Paragraphs[0].Append("Email").Bold();
                        table.Rows[0].Cells[3].Paragraphs[0].Append("Имя").Bold();
                        table.Rows[0].Cells[4].Paragraphs[0].Append("Фамилия").Bold();
                        table.Rows[0].Cells[5].Paragraphs[0].Append("Роль").Bold();
                        table.Rows[0].Cells[6].Paragraphs[0].Append("Дата рождения").Bold();
                        table.Rows[0].Cells[7].Paragraphs[0].Append("Пол").Bold();
                        table.Rows[0].Cells[8].Paragraphs[0].Append("Телефон").Bold();
                        table.Rows[0].Cells[9].Paragraphs[0].Append("Последний вход").Bold();
                        table.Rows[0].Cells[10].Paragraphs[0].Append("Создан").Bold();
                        table.Rows[0].Cells[11].Paragraphs[0].Append("Обновлен").Bold();

                        // Данные
                        int rowIndex = 1;
                        foreach (var user in _users)
                        {
                            table.Rows[rowIndex].Cells[0].Paragraphs[0].Append(user.UserId.ToString());
                            table.Rows[rowIndex].Cells[1].Paragraphs[0].Append(user.Username ?? "Не указано");
                            table.Rows[rowIndex].Cells[2].Paragraphs[0].Append(user.Email ?? "Не указано");
                            table.Rows[rowIndex].Cells[3].Paragraphs[0].Append(user.FirstName ?? "Не указано");
                            table.Rows[rowIndex].Cells[4].Paragraphs[0].Append(user.LastName ?? "Не указано");
                            table.Rows[rowIndex].Cells[5].Paragraphs[0].Append(user.RoleName);
                            table.Rows[rowIndex].Cells[6].Paragraphs[0].Append(user.DateOfBirth?.ToString("dd.MM.yyyy") ?? "Не указано");
                            table.Rows[rowIndex].Cells[7].Paragraphs[0].Append(user.Gender ?? "Не указано");
                            table.Rows[rowIndex].Cells[8].Paragraphs[0].Append(user.Phone ?? "Не указано");
                            table.Rows[rowIndex].Cells[9].Paragraphs[0].Append(user.LastLoginDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не входил");
                            table.Rows[rowIndex].Cells[10].Paragraphs[0].Append(user.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
                            table.Rows[rowIndex].Cells[11].Paragraphs[0].Append(user.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Не обновлялся");
                            rowIndex++;
                        }

                        doc.InsertTable(table);

                        doc.InsertParagraph();
                        doc.InsertParagraph($"Всего пользователей: {_users.Count}")
                           .FontSize(12)
                           .Bold();

                        var activeUsers = _users.Count(u => u.LastLoginDate.HasValue);
                        doc.InsertParagraph($"Активных пользователей (с входом в систему): {activeUsers}")
                           .FontSize(12)
                           .Bold();

                        doc.Save();
                    }

                    MessageBox.Show("Экспорт в Word выполнен успешно!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Word: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class UserViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RoleViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }
}