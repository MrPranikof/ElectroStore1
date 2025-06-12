using ClosedXML.Excel;
using ElectroStore1.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace ElectroStore1.Pages
{
    public partial class Brand : Window
    {
        private ObservableCollection<BrandViewModel> _brands;

        public Brand()
        {
            InitializeComponent();
            _brands = new ObservableCollection<BrandViewModel>();
            BrandDG.ItemsSource = _brands;
            LoadBrands();
        }

        public void LoadBrands()
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT BrandId, BrandName, CreatedAt, UpdatedAt FROM Brands";

                    _brands.Clear();

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _brands.Add(new BrandViewModel
                            {
                                BrandId = reader.GetInt32(0),
                                BrandName = reader.GetString(1),
                                CreatedAt = reader.GetDateTime(2),
                                UpdatedAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                BrandDG.ItemsSource = _brands;
            }
            else
            {
                var filtered = _brands
                    .Where(b => b.BrandName?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                BrandDG.ItemsSource = filtered;
            }
        }

        private void AddBrandButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddBrand();
            if (addWindow.ShowDialog() == true)
            {
                LoadBrands();
            }
        }

        private void EditSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    foreach (BrandViewModel brand in _brands)
                    {
                        string updateQuery = @"
                            UPDATE Brands 
                            SET 
                                BrandName = @BrandName,
                                UpdatedAt = GETDATE()
                            WHERE BrandId = @BrandId";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@BrandId", brand.BrandId);
                            cmd.Parameters.AddWithValue("@BrandName", brand.BrandName);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Изменения сохранены!", "Успех",
                                 MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadBrands();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                 MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (BrandDG.SelectedItem is BrandViewModel selectedBrand)
            {
                // Проверка на использование бренда в товарах
                bool isUsed = CheckBrandUsage(selectedBrand.BrandId);

                if (isUsed)
                {
                    MessageBox.Show("Этот бренд используется в товарах и не может быть удален.",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show(
                    $"Удалить бренд '{selectedBrand.BrandName}'?",
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
                            string deleteQuery = "DELETE FROM Brands WHERE BrandId = @BrandId";

                            using (SqlCommand cmd = new SqlCommand(deleteQuery, connection))
                            {
                                cmd.Parameters.AddWithValue("@BrandId", selectedBrand.BrandId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        _brands.Remove(selectedBrand);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                                     MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private bool CheckBrandUsage(int brandId)
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Products WHERE BrandId = @BrandId";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@BrandId", brandId);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch
            {
                return true; // В случае ошибки предполагаем, что бренд используется
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
                    FileName = $"Бренды_{DateTime.Now:yyyy-MM-dd}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Бренды");

                        // Заголовки
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Название";
                        worksheet.Cell(1, 3).Value = "Добавлен";
                        worksheet.Cell(1, 4).Value = "Обновлен";

                        var headerRange = worksheet.Range(1, 1, 1, 4);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Данные
                        int row = 2;
                        foreach (var brand in _brands)
                        {
                            worksheet.Cell(row, 1).Value = brand.BrandId;
                            worksheet.Cell(row, 2).Value = brand.BrandName;
                            worksheet.Cell(row, 3).Value = brand.CreatedAt.ToString("dd.MM.yyyy");
                            worksheet.Cell(row, 4).Value = brand.UpdatedAt?.ToString("dd.MM.yyyy") ?? "-";
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
                    FileName = $"Бренды_{DateTime.Now:yyyy-MM-dd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("ID;Название;Добавлен;Обновлен");

                    foreach (var brand in _brands)
                    {
                        csv.AppendLine($"{brand.BrandId};" +
                                     $"{brand.BrandName};" +
                                     $"{brand.CreatedAt:dd.MM.yyyy};" +
                                     $"{(brand.UpdatedAt.HasValue ? brand.UpdatedAt.Value.ToString("dd.MM.yyyy") : "-")}");
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
                    FileName = $"Бренды_{DateTime.Now:yyyy-MM-dd}.docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var doc = DocX.Create(saveDialog.FileName))
                    {
                        doc.InsertParagraph("Отчет по брендам")
                           .FontSize(20)
                           .Bold()
                           .Alignment = Alignment.center;

                        doc.InsertParagraph($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm}")
                           .FontSize(12)
                           .Alignment = Alignment.center;

                        doc.InsertParagraph();

                        var table = doc.AddTable(_brands.Count + 1, 4);
                        table.Design = TableDesign.ColorfulList;

                        table.Rows[0].Cells[0].Paragraphs[0].Append("ID").Bold();
                        table.Rows[0].Cells[1].Paragraphs[0].Append("Название").Bold();
                        table.Rows[0].Cells[2].Paragraphs[0].Append("Добавлен").Bold();
                        table.Rows[0].Cells[3].Paragraphs[0].Append("Обновлен").Bold();

                        int rowIndex = 1;
                        foreach (var brand in _brands)
                        {
                            table.Rows[rowIndex].Cells[0].Paragraphs[0].Append(brand.BrandId.ToString());
                            table.Rows[rowIndex].Cells[1].Paragraphs[0].Append(brand.BrandName);
                            table.Rows[rowIndex].Cells[2].Paragraphs[0].Append(brand.CreatedAt.ToString("dd.MM.yyyy"));
                            table.Rows[rowIndex].Cells[3].Paragraphs[0].Append(brand.UpdatedAt?.ToString("dd.MM.yyyy") ?? "-");
                            rowIndex++;
                        }

                        doc.InsertTable(table);

                        doc.InsertParagraph();
                        doc.InsertParagraph($"Всего брендов: {_brands.Count}")
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

    public class BrandViewModel
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}