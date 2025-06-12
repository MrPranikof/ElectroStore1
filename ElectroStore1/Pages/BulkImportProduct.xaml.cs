using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using CsvHelper;
using CsvHelper.Configuration;
using ElectroStore1;

namespace ElectroStore.Pages
{
    /// <summary>
    /// Логика взаимодействия для BulkImportProduct.xaml
    /// </summary>
    public partial class BulkImportProduct : Window
    {
        private DataTable _csvData;
        private string _selectedFilePath;

        public BulkImportProduct()
        {
            InitializeComponent();
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                Title = "Выберите CSV файл с товарами"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                FileNameText.Text = Path.GetFileName(_selectedFilePath);
                LoadCsvPreview();
            }
        }
        private void LoadCsvPreview()
        {
            try
            {
                _csvData = new DataTable();

                // Читаем первые несколько строк для диагностики
                string[] firstLines = File.ReadAllLines(_selectedFilePath).Take(5).ToArray();
                System.Diagnostics.Debug.WriteLine("Первые строки CSV файла:");
                foreach (var line in firstLines)
                {
                    System.Diagnostics.Debug.WriteLine(line);
                }

                using (var reader = new StreamReader(_selectedFilePath, Encoding.UTF8))
                {
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        BadDataFound = null,
                        Mode = CsvMode.RFC4180,
                        TrimOptions = TrimOptions.Trim,
                        IgnoreBlankLines = true,
                        MissingFieldFound = null,
                        HeaderValidated = null,
                        DetectDelimiter = true // Автоматическое определение разделителя
                    };

                    using (var csv = new CsvReader(reader, config))
                    {
                        // Читаем все записи в DataTable
                        using (var dr = new CsvDataReader(csv))
                        {
                            _csvData.Load(dr);
                        }
                    }
                }

                // Выводим информацию о загруженных данных
                System.Diagnostics.Debug.WriteLine($"Загружено строк: {_csvData.Rows.Count}");
                System.Diagnostics.Debug.WriteLine($"Колонки: {string.Join(", ", _csvData.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

                // Показываем предпросмотр
                var previewData = _csvData.Clone();
                for (int i = 0; i < Math.Min(10, _csvData.Rows.Count); i++)
                {
                    previewData.ImportRow(_csvData.Rows[i]);
                }

                PreviewDataGrid.ItemsSource = previewData.DefaultView;
                PreviewDataGrid.Visibility = Visibility.Visible;
                PlaceholderText.Visibility = Visibility.Collapsed;
                ImportButton.IsEnabled = true;

                FileNameText.Text = $"{Path.GetFileName(_selectedFilePath)} ({_csvData.Rows.Count} товаров)";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при чтении CSV: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Ошибка при чтении CSV файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ImportButton.IsEnabled = false;
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            if (_csvData == null || _csvData.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для импорта", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохраняем значение CheckBox перед запуском фонового потока
            bool skipExisting = SkipExistingCheckBox.IsChecked == true;

            // Блокируем кнопки
            ImportButton.IsEnabled = false;
            SelectFileButton.IsEnabled = false;

            // Показываем прогресс
            ProgressPanel.Visibility = Visibility.Visible;
            ImportProgressBar.Value = 0;

            try
            {
                await Task.Run(() => ImportProducts(skipExisting));

                MessageBox.Show("Импорт завершен успешно!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Разблокируем кнопки
                ImportButton.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
                ProgressPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ImportProducts(bool skipExisting)
        {
            int totalRows = _csvData.Rows.Count;
            int processedRows = 0;
            int importedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            List<string> errorLog = new List<string>();

            using (SqlConnection connection = DBConnection.GetConnection())
            {
                connection.Open();

                var categories = GetCategoriesDict(connection);
                var brands = GetBrandsDict(connection);

                foreach (DataRow row in _csvData.Rows)
                {
                    try
                    {
                        string productName = GetColumnValue(row, "name");

                        if (string.IsNullOrEmpty(productName))
                        {
                            errorCount++;
                            errorLog.Add($"Строка {processedRows + 1}: Пустое название товара");
                            continue;
                        }

                        if (skipExisting && ProductExists(connection, productName))
                        {
                            skippedCount++;
                            continue;
                        }

                        string mainCategory = GetColumnValue(row, "main_category");
                        string subCategory = GetColumnValue(row, "sub_category");
                        string imageUrl = GetColumnValue(row, "image");
                        string actualPrice = GetColumnValue(row, "actual_price");
                        string discountPrice = GetColumnValue(row, "discount_price");

                        decimal price = ParsePrice(actualPrice);
                        decimal discPrice = ParsePrice(discountPrice);

                        // Используем дисконтную цену как основную, если она есть
                        decimal finalPrice = discPrice > 0 ? discPrice : price;

                        int discountPercent = 0;
                        if (price > 0 && discPrice > 0 && discPrice < price)
                        {
                            discountPercent = (int)Math.Round(((price - discPrice) / price) * 100);
                        }

                        // ИЗМЕНЕНИЕ: Используем main_category вместо sub_category
                        // Приоритет: main_category, затем sub_category если main_category пустая
                        string categoryName = !string.IsNullOrEmpty(mainCategory) ? mainCategory :
                                            (!string.IsNullOrEmpty(subCategory) ? subCategory : "Без категории");

                        int categoryId = GetOrCreateCategory(connection, categories, categoryName);

                        // Определяем бренд
                        string brandName = ExtractBrandFromName(productName);
                        int brandId = GetOrCreateBrand(connection, brands, brandName);

                        // Загружаем изображение
                        byte[] imageBytes = null;
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            imageBytes = DownloadImage(imageUrl);
                        }

                        // Вставляем товар в базу данных
                        string insertQuery = @"
                    INSERT INTO Products 
                    (ProductName, Description, Price, StockCount, BrandId, CategoryId, 
                     IsActive, DiscountPercent, ProductImage, CreatedAt)
                    VALUES 
                    (@ProductName, @Description, @Price, @StockCount, @BrandId, @CategoryId, 
                     1, @DiscountPercent, @ProductImage, GETDATE())";

                        using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@ProductName", productName);
                            cmd.Parameters.AddWithValue("@Description", DBNull.Value);
                            cmd.Parameters.AddWithValue("@Price", finalPrice);
                            cmd.Parameters.AddWithValue("@StockCount", 10);
                            cmd.Parameters.AddWithValue("@BrandId", brandId);
                            cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                            cmd.Parameters.AddWithValue("@DiscountPercent", discountPercent);

                            if (imageBytes != null)
                                cmd.Parameters.AddWithValue("@ProductImage", imageBytes);
                            else
                                cmd.Parameters.AddWithValue("@ProductImage", DBNull.Value);

                            cmd.ExecuteNonQuery();
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        string errorMsg = $"Строка {processedRows + 1}: {ex.Message}";
                        errorLog.Add(errorMsg);

                        if (errorCount <= 5)
                        {
                            System.Diagnostics.Debug.WriteLine($"ОШИБКА при импорте:\n{errorMsg}\n{ex.StackTrace}");
                        }

                        Logger.Error($"Ошибка при импорте товара: {ex.Message}");
                    }

                    processedRows++;

                    int progress = (int)((double)processedRows / totalRows * 100);

                    // Обновляем UI в главном потоке
                    Dispatcher.Invoke(() =>
                    {
                        ImportProgressBar.Value = progress;
                        ProgressText.Text = $"Импорт товаров... {progress}%";
                        ProgressDetailsText.Text = $"Обработано: {processedRows}/{totalRows} | Импортировано: {importedCount} | Пропущено: {skippedCount} | Ошибок: {errorCount}";
                    });
                }

                // После завершения импорта показываем первые несколько ошибок
                if (errorLog.Count > 0)
                {
                    string errorSummary = "Детали ошибок импорта:\n\n";
                    for (int i = 0; i < Math.Min(5, errorLog.Count); i++)
                    {
                        errorSummary += errorLog[i] + "\n\n";
                    }

                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(errorSummary, "Детали ошибок", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
            }
        }

        // Вспомогательный метод для безопасного получения значения колонки
        private string GetColumnValue(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName))
            {
                return row[columnName]?.ToString()?.Trim() ?? "";
            }
            return "";
        }
        private Dictionary<string, int> GetCategoriesDict(SqlConnection connection)
        {
            var categories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string query = "SELECT CategoryId, CategoryName FROM Categories";

            using (SqlCommand cmd = new SqlCommand(query, connection))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    categories[reader.GetString(1)] = reader.GetInt32(0);
                }
            }

            return categories;
        }

        private Dictionary<string, int> GetBrandsDict(SqlConnection connection)
        {
            var brands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string query = "SELECT BrandId, BrandName FROM Brands";

            using (SqlCommand cmd = new SqlCommand(query, connection))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    brands[reader.GetString(1)] = reader.GetInt32(0);
                }
            }

            return brands;
        }

        private int GetOrCreateBrand(SqlConnection connection, Dictionary<string, int> brands, string brandName)
        {
            if (string.IsNullOrEmpty(brandName))
                brandName = "Без бренда";

            // Проверяем в кэше
            if (brands.ContainsKey(brandName))
                return brands[brandName];

            // Создаем новый бренд
            string insertQuery = "INSERT INTO Brands (BrandName, CreatedAt) OUTPUT INSERTED.BrandId VALUES (@Name, GETDATE())";
            using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Name", brandName);
                int brandId = (int)cmd.ExecuteScalar();
                brands[brandName] = brandId;
                return brandId;
            }
        }

        private int GetOrCreateCategory(SqlConnection connection, Dictionary<string, int> categories, string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                categoryName = "Без категории";

            // Проверяем в кэше
            if (categories.ContainsKey(categoryName))
                return categories[categoryName];

            // Создаем новую категорию
            string insertQuery = "INSERT INTO Categories (CategoryName, CreatedAt) OUTPUT INSERTED.CategoryId VALUES (@Name, GETDATE())";
            using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Name", categoryName);
                int categoryId = (int)cmd.ExecuteScalar();
                categories[categoryName] = categoryId;
                return categoryId;
            }
        }

        private bool ProductExists(SqlConnection connection, string productName)
        {
            string query = "SELECT COUNT(*) FROM Products WHERE ProductName = @Name";
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Name", productName);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private string ExtractBrandFromName(string productName)
        {
            // Извлекаем бренд из названия товара (первое слово обычно бренд)
            if (string.IsNullOrEmpty(productName))
                return "Без бренда";

            string[] words = productName.Split(' ');
            if (words.Length > 0)
            {
                // Список известных брендов для лучшего распознавания
                string[] knownBrands = { "Redmi", "Samsung", "Apple", "Sony", "LG", "Xiaomi", "OnePlus", "Realme", "Oppo", "Vivo" };

                foreach (var brand in knownBrands)
                {
                    if (productName.IndexOf(brand, StringComparison.OrdinalIgnoreCase) >= 0)
                        return brand;
                }

                return words[0];
            }

            return "Без бренда";
        }

        private byte[] DownloadImage(string imageUrl)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    return client.DownloadData(imageUrl);
                }
            }
            catch
            {
                return null;
            }
        }

        private decimal ParsePrice(string priceStr)
        {
            if (string.IsNullOrEmpty(priceStr))
                return 0;

            // Удаляем символ валюты и пробелы
            string cleanPrice = priceStr.Replace("₹", "").Replace("в‚№", "").Trim();

            // Удаляем запятые (разделители тысяч в индийском формате)
            cleanPrice = cleanPrice.Replace(",", "");

            if (decimal.TryParse(cleanPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
            {
                // Конвертируем из рупий в рубли (примерный курс 1 INR = 1.1 RUB)
                return Math.Round(price * 1.1m, 2);
            }

            return 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}