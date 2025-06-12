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
    public partial class Orders : Window, INotifyPropertyChanged
    {
        private SqlConnection _connection;
        private string _searchText = "";
        private ICollectionView _ordersView;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<OrderViewModel> OrdersList { get; } = new ObservableCollection<OrderViewModel>();
        public ObservableCollection<OrderItemViewModel> OrderItemsList { get; } = new ObservableCollection<OrderItemViewModel>();

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                _ordersView?.Refresh();
            }
        }

        public Orders()
        {
            InitializeComponent();
            DataContext = this;
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                _connection = DBConnection.GetConnection();
                _connection.Open();

                var ordersQuery = @"SELECT o.OrderId, u.UserName, o.OrderDate, o.TotalAmount, o.Status, 
                  o.TrackingNumber, o.PaymentStatus, o.CreatedAt, o.UpdatedAt, o.DeliveryDate 
                  FROM Orders o JOIN Users u ON o.UserId = u.UserId
                  ORDER BY o.OrderDate DESC";

                using (var cmd = new SqlCommand(ordersQuery, _connection))
                using (var reader = cmd.ExecuteReader())
                {
                    OrdersList.Clear();
                    while (reader.Read())
                    {
                        OrdersList.Add(new OrderViewModel
                        {
                            OrderId = reader.GetInt32(0),
                            UserName = reader.GetString(1),
                            OrderDate = reader.GetDateTime(2),
                            TotalAmount = reader.GetDecimal(3),
                            Status = reader.GetString(4),
                            TrackingNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
                            PaymentStatus = reader.GetString(6),
                            CreatedAt = reader.GetDateTime(7),
                            UpdatedAt = reader.IsDBNull(8) ? reader.GetDateTime(7) : reader.GetDateTime(8),
                            DeliveryDate = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9)
                        });
                    }
                }

                // Настройка фильтрации и сортировки
                _ordersView = CollectionViewSource.GetDefaultView(OrdersList);
                _ordersView.Filter = OrderFilter;
                OrdersDG.ItemsSource = _ordersView;
                OrderItemsDG.ItemsSource = OrderItemsList;

                OrdersDG.SelectionChanged += (s, e) =>
                {
                    if (OrdersDG.SelectedItem is OrderViewModel selectedOrder)
                        LoadOrderItems(selectedOrder.OrderId);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool OrderFilter(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var order = item as OrderViewModel;
            string search = SearchText.ToLower();

            return order.OrderId.ToString().Contains(search) ||
                   order.UserName.ToLower().Contains(search) ||
                   order.Status.ToLower().Contains(search) ||
                   (order.TrackingNumber?.ToLower().Contains(search) ?? false);
        }

        private void LoadOrderItems(int orderId)
        {
            try
            {
                var query = @"SELECT oi.OrderItemId, p.ProductName, oi.Quantity, oi.Price 
                            FROM OrderItems oi JOIN Products p ON oi.ProductId = p.ProductId
                            WHERE oi.OrderId = @OrderId";

                using (var cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        OrderItemsList.Clear();
                        while (reader.Read())
                        {
                            OrderItemsList.Add(new OrderItemViewModel
                            {
                                OrderItemId = reader.GetInt32(0),
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
                MessageBox.Show($"Ошибка загрузки позиций: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OrdersDG.CommitEdit(); // Фиксируем возможные незавершенные правки

                var changedOrders = OrdersList.Where(o => o.IsModified).ToList();
                if (!changedOrders.Any())
                {
                    MessageBox.Show("Нет изменений для сохранения", "Информация",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var order in changedOrders)
                {
                    var updateQuery = @"UPDATE Orders SET 
                                       Status = @Status, 
                                       TrackingNumber = @TrackingNumber,
                                       DeliveryDate = @DeliveryDate,
                                       UpdatedAt = GETDATE()
                                       WHERE OrderId = @OrderId";

                    using (var cmd = new SqlCommand(updateQuery, _connection))
                    {
                        cmd.Parameters.AddWithValue("@Status", order.Status);
                        cmd.Parameters.AddWithValue("@TrackingNumber",
                            string.IsNullOrWhiteSpace(order.TrackingNumber) ? (object)DBNull.Value : order.TrackingNumber);
                        cmd.Parameters.AddWithValue("@DeliveryDate",
                            order.DeliveryDate.HasValue ? (object)order.DeliveryDate.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@OrderId", order.OrderId);

                        cmd.ExecuteNonQuery();
                        order.IsModified = false;
                        order.UpdatedAt = DateTime.Now;
                    }
                }

                MessageBox.Show($"Успешно сохранено {changedOrders.Count} заказов", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDG.SelectedItem is not OrderViewModel selectedOrder)
            {
                MessageBox.Show("Выберите заказ для удаления", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить заказ №{selectedOrder.OrderId}? Это действие нельзя отменить.",
                                      "Подтверждение удаления",
                                      MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        var deleteItemsQuery = "DELETE FROM OrderItems WHERE OrderId = @OrderId";
                        using (var cmd = new SqlCommand(deleteItemsQuery, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", selectedOrder.OrderId);
                            cmd.ExecuteNonQuery();
                        }

                        // Удаляем сам заказ
                        var deleteOrderQuery = "DELETE FROM Orders WHERE OrderId = @OrderId";
                        using (var cmd = new SqlCommand(deleteOrderQuery, _connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderId", selectedOrder.OrderId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        OrdersList.Remove(selectedOrder);
                        OrderItemsList.Clear();

                        MessageBox.Show("Заказ успешно удален", "Успех",
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

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                FileName = $"Заказы_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                using (var writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                {
                    // Заголовки
                    writer.WriteLine("ID;Пользователь;Дата заказа;Сумма;Статус;Трек-номер;Статус оплаты;Дата создания");

                    // Данные
                    foreach (var order in OrdersList)
                    {
                        writer.WriteLine($"{order.OrderId};\"{order.UserName}\";" +
                                        $"{order.OrderDate:dd.MM.yyyy HH:mm};" +
                                        $"{order.TotalAmount:0.00};{order.Status};" +
                                        $"{order.TrackingNumber ?? "нет"};{order.PaymentStatus};" +
                                        $"{order.CreatedAt:dd.MM.yyyy};" +
                                        $"{(order.DeliveryDate.HasValue ? order.DeliveryDate.Value.ToString("dd.MM.yyyy") : "—")}");
                    }
                }

                MessageBox.Show($"Экспортировано {OrdersList.Count} заказов в CSV", "Экспорт завершен",
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
                FileName = $"Заказы_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Заказы");

                    // Заголовки
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Пользователь";
                    worksheet.Cell(1, 3).Value = "Дата заказа";
                    worksheet.Cell(1, 4).Value = "Сумма";
                    worksheet.Cell(1, 5).Value = "Статус";
                    worksheet.Cell(1, 6).Value = "Трек-номер";
                    worksheet.Cell(1, 7).Value = "Статус оплаты";
                    worksheet.Cell(1, 8).Value = "Дата создания";
                    worksheet.Cell(1, 9).Value = "Дата доставки";

                    for (int i = 0; i < OrdersList.Count; i++)
                    {
                        var order = OrdersList[i];
                        worksheet.Cell(i + 2, 1).Value = order.OrderId;
                        worksheet.Cell(i + 2, 2).Value = order.UserName;
                        worksheet.Cell(i + 2, 3).Value = order.OrderDate;
                        worksheet.Cell(i + 2, 4).Value = order.TotalAmount;
                        worksheet.Cell(i + 2, 5).Value = order.Status;
                        worksheet.Cell(i + 2, 6).Value = order.TrackingNumber ?? "нет";
                        worksheet.Cell(i + 2, 7).Value = order.PaymentStatus;
                        worksheet.Cell(i + 2, 8).Value = order.CreatedAt;
                        worksheet.Cell(i + 2, 9).Value = order.DeliveryDate;

                        worksheet.Cell(i + 2, 3).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                        worksheet.Cell(i + 2, 4).Style.NumberFormat.Format = "0.00 ₽";
                        worksheet.Cell(i + 2, 8).Style.DateFormat.Format = "dd.MM.yyyy";
                        worksheet.Cell(i + 2, 9).Style.DateFormat.Format = "dd.MM.yyyy";
                    }

                    // Авторазмер колонок
                    worksheet.Columns().AdjustToContents();

                    // Сохраняем файл
                    workbook.SaveAs(saveFileDialog.FileName);
                }

                MessageBox.Show($"Экспортировано {OrdersList.Count} заказов в Excel", "Экспорт завершен",
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
                FileName = $"Заказы_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                using (var document = DocX.Create(saveFileDialog.FileName))
                {
                    var title = document.InsertParagraph("Список заказов");
                    title.FontSize(16).Bold().Alignment = Alignment.center;
                    document.InsertParagraph("");

                    var table = document.AddTable(OrdersList.Count + 1, 7);
                    table.Design = TableDesign.LightGrid;

                    // Заголовки таблицы
                    table.Rows[0].Cells[0].Paragraphs.First().Append("ID").Bold();
                    table.Rows[0].Cells[1].Paragraphs.First().Append("Пользователь").Bold();
                    table.Rows[0].Cells[2].Paragraphs.First().Append("Дата заказа").Bold();
                    table.Rows[0].Cells[3].Paragraphs.First().Append("Сумма").Bold();
                    table.Rows[0].Cells[4].Paragraphs.First().Append("Статус").Bold();
                    table.Rows[0].Cells[5].Paragraphs.First().Append("Трек-номер").Bold();
                    table.Rows[0].Cells[6].Paragraphs.First().Append("Дата доставки").Bold();

                    // Заполняем таблицу данными
                    for (int i = 0; i < OrdersList.Count; i++)
                    {
                        var order = OrdersList[i];
                        table.Rows[i + 1].Cells[0].Paragraphs.First().Append(order.OrderId.ToString());
                        table.Rows[i + 1].Cells[1].Paragraphs.First().Append(order.UserName);
                        table.Rows[i + 1].Cells[2].Paragraphs.First().Append(order.OrderDate.ToString("dd.MM.yyyy HH:mm"));
                        table.Rows[i + 1].Cells[3].Paragraphs.First().Append(order.TotalAmount.ToString("0.00") + " ₽");
                        table.Rows[i + 1].Cells[4].Paragraphs.First().Append(order.Status);
                        table.Rows[i + 1].Cells[5].Paragraphs.First().Append(order.TrackingNumber ?? "нет");
                        table.Rows[i + 1].Cells[6].Paragraphs.First().Append(order.DeliveryDate.HasValue ? order.DeliveryDate.Value.ToString("dd.MM.yyyy") : "—");
                    }

                    document.InsertTable(table);
                    document.Save();
                }

                MessageBox.Show($"Экспортировано {OrdersList.Count} заказов в Word", "Экспорт завершен",
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
            _ordersView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SearchText = SearchTextBox.Text;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _connection?.Dispose();
        }
    }

    public class OrderViewModel : INotifyPropertyChanged
    {
        private string _status;
        private string _trackingNumber;

        public int OrderId { get; set; }
        public string UserName { get; set; }
        public DateTime OrderDate { get; set; }

        private DateTime? _deliveryDate;
        public DateTime? DeliveryDate
        {
            get => _deliveryDate;
            set
            {
                if (_deliveryDate != value)
                {
                    _deliveryDate = value;
                    OnPropertyChanged();
                    IsModified = true;
                }
            }
        }
        public decimal TotalAmount { get; set; }
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    IsModified = true;
                }
            }
        }

        public string TrackingNumber
        {
            get => _trackingNumber;
            set
            {
                if (_trackingNumber != value)
                {
                    _trackingNumber = value;
                    OnPropertyChanged();
                    IsModified = true;
                }
            }
        }

        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsModified { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class OrderItemViewModel
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
}