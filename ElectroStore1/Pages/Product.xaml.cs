using ClosedXML.Excel;
using ElectroStore.Pages;
using ElectroStore1.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace ElectroStore1.Pages.MainPages
{
    public partial class Product : Window
    {
        private ObservableCollection<ProductAdminViewModel> _products;
        private List<dynamic> _brands;
        private List<dynamic> _categories;

        public Product()
        {
            InitializeComponent();
            _products = new ObservableCollection<ProductAdminViewModel>();
            ProductDG.ItemsSource = _products;
            DataContext = this;
            LoadProductsForAdmin();
        }

        public void LoadProductsForAdmin()
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    _brands = new List<dynamic>();
                    string brandQuery = "SELECT BrandId, BrandName FROM Brands";
                    using (SqlCommand cmd = new SqlCommand(brandQuery, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _brands.Add(new { BrandId = reader.GetInt32(0), BrandName = reader.GetString(1) });
                        }
                    }

                    _categories = new List<dynamic>();
                    string categoryQuery = "SELECT CategoryId, CategoryName FROM Categories";
                    using (SqlCommand cmd = new SqlCommand(categoryQuery, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _categories.Add(new { CategoryId = reader.GetInt32(0), CategoryName = reader.GetString(1) });
                        }
                    }

                    var brandColumn = ProductDG.Columns.OfType<DataGridComboBoxColumn>()
                        .FirstOrDefault(c => c.Header.ToString() == "БРЕНД");
                    if (brandColumn != null) brandColumn.ItemsSource = _brands;

                    var categoryColumn = ProductDG.Columns.OfType<DataGridComboBoxColumn>()
                        .FirstOrDefault(c => c.Header.ToString() == "КАТЕГОРИЯ");
                    if (categoryColumn != null) categoryColumn.ItemsSource = _categories;

                    string query = @"
                        SELECT 
                            p.ProductId, p.ProductName, p.Description, p.Price, p.StockCount,
                            p.BrandId, b.BrandName,
                            p.CategoryId, c.CategoryName,
                            p.DiscountPercent, p.DiscountedPrice,
                            p.CreatedAt, p.UpdatedAt
                        FROM Products p
                        JOIN Brands b ON p.BrandId = b.BrandId
                        JOIN Categories c ON p.CategoryId = c.CategoryId";

                    _products.Clear();

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _products.Add(new ProductAdminViewModel
                            {
                                ProductId = reader.GetInt32(0),
                                ProductName = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Price = reader.GetDecimal(3),
                                StockCount = reader.GetInt32(4),
                                BrandId = reader.GetInt32(5),
                                BrandName = reader.GetString(6),
                                CategoryId = reader.GetInt32(7),
                                CategoryName = reader.GetString(8),
                                DiscountPercent = reader.GetInt32(9),
                                DiscountedPrice = reader.IsDBNull(10) ? reader.GetDecimal(3) : reader.GetDecimal(10),
                                CreatedAt = reader.GetDateTime(11),
                                UpdatedAt = reader.IsDBNull(12) ? reader.GetDateTime(11) : reader.GetDateTime(12)
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

        private void BulkImportButton_Click(object sender, RoutedEventArgs e)
        {
            var bulkImportWindow = new BulkImportProduct();
            if (bulkImportWindow.ShowDialog() == true)
            {
                LoadProductsForAdmin();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                ProductDG.ItemsSource = _products;
            }
            else
            {
                var filtered = _products
                    .Where(p => p.ProductName?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                p.Description?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                ProductDG.ItemsSource = filtered;
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddProduct();
            if (addWindow.ShowDialog() == true)
            {
                LoadProductsForAdmin();
            }
        }

        private void EditSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    foreach (ProductAdminViewModel product in _products)
                    {
                        string updateQuery = @"
                            UPDATE Products 
                            SET 
                                ProductName = @ProductName,
                                Description = @Description,
                                Price = @Price,
                                StockCount = @StockCount,
                                BrandId = @BrandId,
                                CategoryId = @CategoryId,
                                DiscountPercent = @DiscountPercent,
                                DiscountedPrice = @DiscountedPrice,
                                UpdatedAt = GETDATE()
                            WHERE ProductId = @ProductId";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
                            cmd.Parameters.AddWithValue("@Description", product.Description ?? string.Empty);
                            cmd.Parameters.AddWithValue("@Price", product.Price);
                            cmd.Parameters.AddWithValue("@StockCount", product.StockCount);
                            cmd.Parameters.AddWithValue("@BrandId", product.BrandId);
                            cmd.Parameters.AddWithValue("@CategoryId", product.CategoryId);
                            cmd.Parameters.AddWithValue("@DiscountPercent", product.DiscountPercent);
                            cmd.Parameters.AddWithValue("@DiscountedPrice", product.DiscountedPrice);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Изменения сохранены!", "Успех",
                                 MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadProductsForAdmin();
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
            if (ProductDG.SelectedItem is ProductAdminViewModel selectedProduct)
            {
                var result = MessageBox.Show(
                    $"Удалить товар '{selectedProduct.ProductName}'?\n\n" +
                    "ВНИМАНИЕ: Товар будет удален из всех корзин пользователей!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection connection = DBConnection.GetConnection())
                        {
                            connection.Open();

                            // Начинаем транзакцию
                            using (SqlTransaction transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    // Сначала удаляем товар из всех корзин
                                    string deleteFromCartsQuery = "DELETE FROM CartItems WHERE ProductId = @ProductId";
                                    using (SqlCommand cmd = new SqlCommand(deleteFromCartsQuery, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@ProductId", selectedProduct.ProductId);
                                        int cartItemsDeleted = cmd.ExecuteNonQuery();

                                        if (cartItemsDeleted > 0)
                                        {
                                            Logger.Info($"Удалено {cartItemsDeleted} записей из корзин");
                                        }
                                    }

                                    // Удаляем товар из избранного
                                    string deleteFromFavouritesQuery = "DELETE FROM Favourites WHERE ProductId = @ProductId";
                                    using (SqlCommand cmd = new SqlCommand(deleteFromFavouritesQuery, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@ProductId", selectedProduct.ProductId);
                                        int favouritesDeleted = cmd.ExecuteNonQuery();

                                        if (favouritesDeleted > 0)
                                        {
                                            Logger.Info($"Удалено {favouritesDeleted} записей из избранного");
                                        }
                                    }

                                    // Теперь удаляем сам товар
                                    string deleteProductQuery = "DELETE FROM Products WHERE ProductId = @ProductId";
                                    using (SqlCommand cmd = new SqlCommand(deleteProductQuery, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@ProductId", selectedProduct.ProductId);
                                        cmd.ExecuteNonQuery();
                                    }

                                    // Подтверждаем транзакцию
                                    transaction.Commit();

                                    _products.Remove(selectedProduct);

                                    MessageBox.Show($"Товар '{selectedProduct.ProductName}' успешно удален!",
                                                  "Успех",
                                                  MessageBoxButton.OK,
                                                  MessageBoxImage.Information);
                                }
                                catch (Exception)
                                {
                                    transaction.Rollback();
                                    throw;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                                     MessageBoxButton.OK, MessageBoxImage.Error);
                        Logger.Error("Ошибка при удалении товара", ex);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите товар для удаления", "Внимание",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    FileName = $"Товары_{DateTime.Now:yyyy-MM-dd}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Товары");

                        // Заголовки
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Название";
                        worksheet.Cell(1, 3).Value = "Описание";
                        worksheet.Cell(1, 4).Value = "Цена";
                        worksheet.Cell(1, 5).Value = "Цена со скидкой";
                        worksheet.Cell(1, 6).Value = "Скидка %";
                        worksheet.Cell(1, 7).Value = "Остаток";
                        worksheet.Cell(1, 8).Value = "Бренд";
                        worksheet.Cell(1, 9).Value = "Категория";
                        worksheet.Cell(1, 10).Value = "Добавлен";
                        worksheet.Cell(1, 11).Value = "Обновлен";

                        var headerRange = worksheet.Range(1, 1, 1, 11);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Данные
                        int row = 2;
                        foreach (var product in _products)
                        {
                            worksheet.Cell(row, 1).Value = product.ProductId;
                            worksheet.Cell(row, 2).Value = product.ProductName;
                            worksheet.Cell(row, 3).Value = product.Description;
                            worksheet.Cell(row, 4).Value = product.Price;
                            worksheet.Cell(row, 5).Value = product.DiscountedPrice;
                            worksheet.Cell(row, 6).Value = product.DiscountPercent;
                            worksheet.Cell(row, 7).Value = product.StockCount;
                            worksheet.Cell(row, 8).Value = product.BrandName;
                            worksheet.Cell(row, 9).Value = product.CategoryName;
                            worksheet.Cell(row, 10).Value = product.CreatedAt.ToString("dd.MM.yyyy");
                            worksheet.Cell(row, 11).Value = product.UpdatedAt.ToString("dd.MM.yyyy");

                            if (product.StockCount == 0)
                            {
                                worksheet.Range(row, 1, row, 11).Style.Fill.BackgroundColor = XLColor.LightCoral;
                            }

                            row++;
                        }

                        // Форматирование
                        worksheet.Column(4).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Column(5).Style.NumberFormat.Format = "#,##0.00 ₽";
                        worksheet.Column(6).Style.NumberFormat.Format = "0";
                        worksheet.Column(3).Width = 30; // Ширина колонки с описанием

                        worksheet.Columns().AdjustToContents();

                        // Итоги
                        worksheet.Cell(row + 1, 2).Value = "ИТОГО:";
                        worksheet.Cell(row + 1, 2).Style.Font.Bold = true;
                        worksheet.Cell(row + 1, 7).FormulaA1 = $"=SUM(G2:G{row - 1})";
                        worksheet.Cell(row + 1, 7).Style.Font.Bold = true;

                        // Границы
                        var dataRange = worksheet.Range(1, 1, row - 1, 11);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

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
                    FileName = $"Товары_{DateTime.Now:yyyy-MM-dd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();

                    // Заголовки
                    csv.AppendLine("ID;Название;Описание;Цена;Цена со скидкой;Скидка %;Остаток;Бренд;Категория;Добавлен;Обновлен");

                    // Данные
                    foreach (var product in _products)
                    {
                        csv.AppendLine($"{product.ProductId};" +
                                     $"{EscapeCsvField(product.ProductName)};" +
                                     $"{EscapeCsvField(product.Description)};" +
                                     $"{product.Price};" +
                                     $"{product.DiscountedPrice};" +
                                     $"{product.DiscountPercent};" +
                                     $"{product.StockCount};" +
                                     $"{EscapeCsvField(product.BrandName)};" +
                                     $"{EscapeCsvField(product.CategoryName)};" +
                                     $"{product.CreatedAt:dd.MM.yyyy};" +
                                     $"{product.UpdatedAt:dd.MM.yyyy}");
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

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            if (field.Contains(";") || field.Contains("\"") || field.Contains("\n"))
                return $"\"{field.Replace("\"", "\"\"")}\"";

            return field;
        }

        private void ExportToWord_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word документы (*.docx)|*.docx",
                    FileName = $"Товары_{DateTime.Now:yyyy-MM-dd}.docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var doc = DocX.Create(saveDialog.FileName))
                    {
                        // Заголовок
                        doc.InsertParagraph("Отчет по товарам")
                           .FontSize(20)
                           .Bold()
                           .Alignment = Alignment.center;

                        doc.InsertParagraph($"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm}")
                           .FontSize(12)
                           .Alignment = Alignment.center;

                        doc.InsertParagraph();

                        // Таблица
                        var table = doc.AddTable(_products.Count + 1, 11);
                        table.Design = TableDesign.ColorfulList;

                        // Заголовки таблицы
                        table.Rows[0].Cells[0].Paragraphs[0].Append("ID").Bold();
                        table.Rows[0].Cells[1].Paragraphs[0].Append("Название").Bold();
                        table.Rows[0].Cells[2].Paragraphs[0].Append("Описание").Bold();
                        table.Rows[0].Cells[3].Paragraphs[0].Append("Цена").Bold();
                        table.Rows[0].Cells[4].Paragraphs[0].Append("Цена со скидкой").Bold();
                        table.Rows[0].Cells[5].Paragraphs[0].Append("Скидка %").Bold();
                        table.Rows[0].Cells[6].Paragraphs[0].Append("Остаток").Bold();
                        table.Rows[0].Cells[7].Paragraphs[0].Append("Бренд").Bold();
                        table.Rows[0].Cells[8].Paragraphs[0].Append("Категория").Bold();
                        table.Rows[0].Cells[9].Paragraphs[0].Append("Добавлен").Bold();
                        table.Rows[0].Cells[10].Paragraphs[0].Append("Обновлен").Bold();

                        // Данные таблицы
                        int rowIndex = 1;
                        foreach (var product in _products)
                        {
                            table.Rows[rowIndex].Cells[0].Paragraphs[0].Append(product.ProductId.ToString());
                            table.Rows[rowIndex].Cells[1].Paragraphs[0].Append(product.ProductName);
                            table.Rows[rowIndex].Cells[2].Paragraphs[0].Append(product.Description ?? string.Empty);
                            table.Rows[rowIndex].Cells[3].Paragraphs[0].Append($"{product.Price:N2} ₽");
                            table.Rows[rowIndex].Cells[4].Paragraphs[0].Append($"{product.DiscountedPrice:N2} ₽");
                            table.Rows[rowIndex].Cells[5].Paragraphs[0].Append($"{product.DiscountPercent}%");

                            if (product.StockCount == 0)
                            {
                                table.Rows[rowIndex].Cells[6].Paragraphs[0].Append("0").Bold().Append(" (НЕТ)").Bold();
                            }
                            else
                            {
                                table.Rows[rowIndex].Cells[6].Paragraphs[0].Append(product.StockCount.ToString());
                            }

                            table.Rows[rowIndex].Cells[7].Paragraphs[0].Append(product.BrandName);
                            table.Rows[rowIndex].Cells[8].Paragraphs[0].Append(product.CategoryName);
                            table.Rows[rowIndex].Cells[9].Paragraphs[0].Append(product.CreatedAt.ToString("dd.MM.yyyy"));
                            table.Rows[rowIndex].Cells[10].Paragraphs[0].Append(product.UpdatedAt.ToString("dd.MM.yyyy"));

                            rowIndex++;
                        }

                        doc.InsertTable(table);

                        // Итоговая информация
                        doc.InsertParagraph();
                        doc.InsertParagraph($"Всего товаров: {_products.Count}")
                           .FontSize(12)
                           .Bold();

                        var totalValue = _products.Sum(p => p.DiscountedPrice * p.StockCount);
                        doc.InsertParagraph($"Общая стоимость товаров на складе: {totalValue:N2} ₽")
                           .FontSize(12)
                           .Bold();

                        var outOfStock = _products.Count(p => p.StockCount == 0);
                        if (outOfStock > 0)
                        {
                            doc.InsertParagraph($"⚠️ Товаров с нулевым остатком: {outOfStock}")
                               .FontSize(12)
                               .Bold();
                        }

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
}