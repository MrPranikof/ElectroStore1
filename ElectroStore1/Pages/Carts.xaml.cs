using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Win32;
using System.Text;
using Xceed.Document.NET;
using Xceed.Words.NET;
using ClosedXML.Excel;

namespace ElectroStore1.Pages.MainPages
{
    public partial class Carts : Window, INotifyPropertyChanged
    {
        private SqlConnection _connection;
        private string _searchText = "";
        private ICollectionView _cartsView;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<CartViewModel> CartsList { get; } = new ObservableCollection<CartViewModel>();
        public ObservableCollection<CartItemViewModel> CartItemsList { get; } = new ObservableCollection<CartItemViewModel>();

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                _cartsView?.Refresh();
            }
        }

        public Carts()
        {
            InitializeComponent();
            DataContext = this;
            LoadCarts();
        }

        private void LoadCarts()
        {
            try
            {
                _connection = DBConnection.GetConnection();
                _connection.Open();

                var cartsQuery = @"SELECT c.CartId, u.UserName, c.CreatedAt, c.UpdatedAt,
                                 COUNT(ci.CartItemId) AS ItemsCount,
                                 SUM(ci.Quantity * p.DiscountedPrice) AS TotalAmount
                                 FROM Cart c
                                 JOIN Users u ON c.UserId = u.UserId
                                 LEFT JOIN CartItems ci ON c.CartId = ci.CartId
                                 LEFT JOIN Products p ON ci.ProductId = p.ProductId
                                 GROUP BY c.CartId, u.UserName, c.CreatedAt, c.UpdatedAt
                                 ORDER BY c.CreatedAt DESC";

                using (var cmd = new SqlCommand(cartsQuery, _connection))
                using (var reader = cmd.ExecuteReader())
                {
                    CartsList.Clear();
                    while (reader.Read())
                    {
                        CartsList.Add(new CartViewModel
                        {
                            CartId = reader.GetInt32(0),
                            UserName = reader.GetString(1),
                            CreatedAt = reader.GetDateTime(2),
                            UpdatedAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                            ItemsCount = reader.GetInt32(4),
                            TotalAmount = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5)
                        });
                    }
                }

                // Настройка фильтрации и сортировки
                _cartsView = CollectionViewSource.GetDefaultView(CartsList);
                _cartsView.Filter = CartFilter;
                CartsDG.ItemsSource = _cartsView;
                CartItemsDG.ItemsSource = CartItemsList;

                CartsDG.SelectionChanged += (s, e) =>
                {
                    if (CartsDG.SelectedItem is CartViewModel selectedCart)
                        LoadCartItems(selectedCart.CartId);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки корзин: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CartFilter(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var cart = item as CartViewModel;
            string search = SearchText.ToLower();

            return cart.CartId.ToString().Contains(search) ||
                   cart.UserName.ToLower().Contains(search) ||
                   cart.ItemsCount.ToString().Contains(search) ||
                   cart.TotalAmount.ToString().Contains(search);
        }

        private void LoadCartItems(int cartId)
        {
            try
            {
                var query = @"SELECT ci.CartItemId, p.ProductName, ci.Quantity, p.DiscountedPrice 
                            FROM CartItems ci 
                            JOIN Products p ON ci.ProductId = p.ProductId
                            WHERE ci.CartId = @CartId";

                using (var cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@CartId", cartId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        CartItemsList.Clear();
                        while (reader.Read())
                        {
                            CartItemsList.Add(new CartItemViewModel
                            {
                                CartItemId = reader.GetInt32(0),
                                ProductName = reader.GetString(1),
                                Quantity = reader.GetInt32(2),
                                Price = reader.GetDecimal(3),
                                TotalPrice = reader.GetInt32(2) * reader.GetDecimal(3)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CartsDG.SelectedItem is not CartViewModel selectedCart)
            {
                MessageBox.Show("Выберите корзину для удаления", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить корзину №{selectedCart.CartId}? Это действие нельзя отменить.",
                                      "Подтверждение удаления",
                                      MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        // Удаляем все товары из корзины
                        var deleteItemsQuery = "DELETE FROM CartItems WHERE CartId = @CartId";
                        using (var cmd = new SqlCommand(deleteItemsQuery, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CartId", selectedCart.CartId);
                            cmd.ExecuteNonQuery();
                        }

                        // Удаляем саму корзину
                        var deleteCartQuery = "DELETE FROM Cart WHERE CartId = @CartId";
                        using (var cmd = new SqlCommand(deleteCartQuery, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CartId", selectedCart.CartId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        CartsList.Remove(selectedCart);
                        CartItemsList.Clear();

                        MessageBox.Show("Корзина успешно удалена", "Успех",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = true;
        }

        private void ExportToCSV_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = $"Корзины_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                using (var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                {
                    // Заголовки
                    writer.WriteLine("ID;Пользователь;Дата создания;Дата обновления;Кол-во товаров;Общая сумма");

                    // Данные
                    foreach (var cart in CartsList)
                    {
                        writer.WriteLine($"{cart.CartId};\"{cart.UserName}\";" +
                                       $"{cart.CreatedAt:dd.MM.yyyy HH:mm};" +
                                       $"{(cart.UpdatedAt.HasValue ? cart.UpdatedAt.Value.ToString("dd.MM.yyyy HH:mm") : "—")};" +
                                       $"{cart.ItemsCount};{cart.TotalAmount:0.00}");
                    }
                }

                MessageBox.Show($"Экспортировано {CartsList.Count} корзин в CSV", "Экспорт завершен",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в CSV: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel файлы (*.xlsx)|*.xlsx",
                FileName = $"Корзины_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Корзины");

                    // Заголовки
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Пользователь";
                    worksheet.Cell(1, 3).Value = "Дата создания";
                    worksheet.Cell(1, 4).Value = "Дата обновления";
                    worksheet.Cell(1, 5).Value = "Кол-во товаров";
                    worksheet.Cell(1, 6).Value = "Общая сумма";

                    // Данные
                    for (int i = 0; i < CartsList.Count; i++)
                    {
                        var cart = CartsList[i];
                        worksheet.Cell(i + 2, 1).Value = cart.CartId;
                        worksheet.Cell(i + 2, 2).Value = cart.UserName;
                        worksheet.Cell(i + 2, 3).Value = cart.CreatedAt;
                        worksheet.Cell(i + 2, 4).Value = (XLCellValue)(cart.UpdatedAt ?? (object)"—");
                        worksheet.Cell(i + 2, 5).Value = cart.ItemsCount;
                        worksheet.Cell(i + 2, 6).Value = cart.TotalAmount;

                        // Форматирование даты и чисел
                        worksheet.Cell(i + 2, 3).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                        if (cart.UpdatedAt.HasValue)
                            worksheet.Cell(i + 2, 4).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                        worksheet.Cell(i + 2, 6).Style.NumberFormat.Format = "0.00 ₽";
                    }

                    // Авторазмер колонок
                    worksheet.Columns().AdjustToContents();

                    // Сохраняем файл
                    workbook.SaveAs(saveFileDialog.FileName);
                }

                MessageBox.Show($"Экспортировано {CartsList.Count} корзин в Excel", "Экспорт завершен",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Excel: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToWord_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Word документы (*.docx)|*.docx",
                FileName = $"Корзины_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                using (var document = DocX.Create(saveFileDialog.FileName))
                {
                    // Заголовок документа
                    var title = document.InsertParagraph("Список корзин");
                    title.FontSize(16).Bold().Alignment = Alignment.center;
                    document.InsertParagraph(""); // Пустая строка

                    // Создаем таблицу
                    var table = document.AddTable(CartsList.Count + 1, 6); // +1 для заголовков
                    table.Design = TableDesign.LightGrid;

                    // Заголовки таблицы
                    table.Rows[0].Cells[0].Paragraphs.First().Append("ID").Bold();
                    table.Rows[0].Cells[1].Paragraphs.First().Append("Пользователь").Bold();
                    table.Rows[0].Cells[2].Paragraphs.First().Append("Дата создания").Bold();
                    table.Rows[0].Cells[3].Paragraphs.First().Append("Дата обновления").Bold();
                    table.Rows[0].Cells[4].Paragraphs.First().Append("Кол-во товаров").Bold();
                    table.Rows[0].Cells[5].Paragraphs.First().Append("Общая сумма").Bold();

                    // Заполняем таблицу данными
                    for (int i = 0; i < CartsList.Count; i++)
                    {
                        var cart = CartsList[i];
                        table.Rows[i + 1].Cells[0].Paragraphs.First().Append(cart.CartId.ToString());
                        table.Rows[i + 1].Cells[1].Paragraphs.First().Append(cart.UserName);
                        table.Rows[i + 1].Cells[2].Paragraphs.First().Append(cart.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
                        table.Rows[i + 1].Cells[3].Paragraphs.First().Append(cart.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—");
                        table.Rows[i + 1].Cells[4].Paragraphs.First().Append(cart.ItemsCount.ToString());
                        table.Rows[i + 1].Cells[5].Paragraphs.First().Append(cart.TotalAmount.ToString("0.00") + " ₽");
                    }

                    document.InsertTable(table);
                    document.Save();
                }

                MessageBox.Show($"Экспортировано {CartsList.Count} корзин в Word", "Экспорт завершен",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Word: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _cartsView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SearchText = SearchTextBox.Text;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _connection?.Dispose();
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CartViewModel : INotifyPropertyChanged
    {
        public int CartId { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ItemsCount { get; set; }
        public decimal TotalAmount { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
}