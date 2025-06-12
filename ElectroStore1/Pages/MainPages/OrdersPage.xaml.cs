using ElectroStore1;
using ElectroStore1.Pages.MainPages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElectroStore.Pages.MainPages
{
    /// <summary>
    /// Логика взаимодействия для OrdersPage.xaml
    /// </summary>
    public partial class OrdersPage : Page, INotifyPropertyChanged
    {
        private SqlConnection _connection;
        private ObservableCollection<OrderViewModel> _orders;

        public ObservableCollection<OrderViewModel> Orders
        {
            get => _orders;
            set
            {
                _orders = value;
                OnPropertyChanged();
            }
        }
        private int _userId;
        public OrdersPage(int UserId)
        {
            InitializeComponent();
            _userId = UserId;
            DataContext = this;
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                _connection = DBConnection.GetConnection();
                _connection.Open();

                int currentUserId = _userId;

                var ordersQuery = @"SELECT o.OrderId, o.OrderDate, o.TotalAmount, o.Status, 
                          o.TrackingNumber, o.PaymentStatus, o.DeliveryDate,
                          o.CreatedAt, o.UpdatedAt, 
                          a.PostalAddress, a.City, a.ZipCode, a.Country
                          FROM Orders o
                          LEFT JOIN Addresses a ON o.AddressId = a.AddressId
                          WHERE o.UserId = @UserId
                          ORDER BY o.OrderDate DESC";

                Orders = new ObservableCollection<OrderViewModel>();

                using (var cmd = new SqlCommand(ordersQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", currentUserId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var order = new OrderViewModel
                            {
                                OrderId = reader.GetInt32(0),
                                OrderDate = reader.GetDateTime(1),
                                TotalAmount = reader.GetDecimal(2),
                                Status = reader.GetString(3),
                                TrackingNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                                PaymentStatus = reader.GetString(5),
                                DeliveryDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                CreatedAt = reader.GetDateTime(7),
                                UpdatedAt = reader.IsDBNull(8) ? reader.GetDateTime(7) : reader.GetDateTime(8),
                                OrderItems = new ObservableCollection<OrderItemViewModel>()
                            };

                            if (!reader.IsDBNull(9))
                            {
                                order.DeliveryAddress = $"{reader.GetString(9)}, {reader.GetString(10)}, {reader.GetString(11)}, {reader.GetString(12)}";
                            }
                            else
                            {
                                order.DeliveryAddress = "Адрес не указан";
                            }

                            order.PaymentMethod = GetPaymentMethodDisplay(order.PaymentStatus);

                            Orders.Add(order);
                        }
                    }
                }

                // Загружаем товары для каждого заказа
                foreach (var order in Orders)
                {
                    LoadOrderItems(order);
                }

                // Показать/скрыть сообщение о пустом списке
                if (Orders.Count == 0)
                {
                    EmptyOrdersText.Visibility = Visibility.Visible;
                    OrdersItemsControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyOrdersText.Visibility = Visibility.Collapsed;
                    OrdersItemsControl.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _connection?.Close();
            }
        }

        private void LoadOrderItems(OrderViewModel order)
        {
            try
            {
                var itemsQuery = @"SELECT oi.OrderItemId, oi.ProductId, p.ProductName, p.ProductImage, 
                         oi.Quantity, oi.Price, (oi.Quantity * oi.Price) as TotalPrice
                         FROM OrderItems oi
                         INNER JOIN Products p ON oi.ProductId = p.ProductId
                         WHERE oi.OrderId = @OrderId";

                using (var cmd = new SqlCommand(itemsQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@OrderId", order.OrderId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new OrderItemViewModel
                            {
                                OrderItemId = reader.GetInt32(0),
                                ProductId = reader.GetInt32(1),
                                ProductName = reader.GetString(2),
                                Quantity = reader.GetInt32(4),
                                Price = reader.GetDecimal(5),
                                TotalPrice = reader.GetDecimal(6)
                            };

                            // Загрузка изображения из базы данных (как в MainPage)
                            if (reader["ProductImage"] != DBNull.Value)
                            {
                                byte[] imageData = (byte[])reader["ProductImage"];
                                using (MemoryStream ms = new MemoryStream(imageData))
                                {
                                    BitmapImage productImage = new BitmapImage();
                                    productImage.BeginInit();
                                    productImage.CacheOption = BitmapCacheOption.OnLoad;
                                    productImage.StreamSource = ms;
                                    productImage.EndInit();
                                    item.ProductImageSource = productImage;
                                }
                            }

                            order.OrderItems.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров заказа: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetPaymentMethodDisplay(string paymentStatus)
        {
            return paymentStatus?.ToLower() switch
            {
                "paid" => "Оплачено",
                "pending" => "Ожидает оплаты",
                "cancelled" => "Отменено",
                _ => "Не указано"
            };
        }
        private void RepeatOrder_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var order = button?.Tag as OrderViewModel;

            if (order == null) return;

            try
            {
                int currentUserId = _userId;

                int cartId = GetOrCreateCart(currentUserId);

                // Добавляем товары в корзину
                foreach (var item in order.OrderItems)
                {
                    AddToCart(cartId, item.ProductId, item.Quantity);
                }

                MessageBox.Show("Товары из заказа добавлены в корзину",
                    "Успешно",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                NavigationService?.Navigate(new BasketPage(currentUserId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при повторении заказа: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private int GetOrCreateCart(int userId)
        {
            using (var connection = DBConnection.GetConnection())
            {
                connection.Open();

                var checkQuery = "SELECT CartId FROM Cart WHERE UserId = @UserId";
                using (var cmd = new SqlCommand(checkQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    var result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        return (int)result;
                    }
                }

                var createQuery = @"INSERT INTO Cart (UserId, CreatedAt, UpdatedAt) 
                          VALUES (@UserId, GETDATE(), GETDATE());
                          SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(createQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        private void AddToCart(int cartId, int productId, int quantity)
        {
            using (var connection = DBConnection.GetConnection())
            {
                connection.Open();

                var checkQuery = @"SELECT CartItemId, Quantity FROM CartItems 
                         WHERE CartId = @CartId AND ProductId = @ProductId";

                using (var cmd = new SqlCommand(checkQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@CartId", cartId);
                    cmd.Parameters.AddWithValue("@ProductId", productId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int cartItemId = reader.GetInt32(0);
                            int currentQuantity = reader.GetInt32(1);
                            reader.Close();

                            var updateQuery = @"UPDATE CartItems 
                                      SET Quantity = @Quantity 
                                      WHERE CartItemId = @CartItemId";

                            using (var updateCmd = new SqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@Quantity", currentQuantity + quantity);
                                updateCmd.Parameters.AddWithValue("@CartItemId", cartItemId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            reader.Close();

                            var insertQuery = @"INSERT INTO CartItems (CartId, ProductId, Quantity) 
                                      VALUES (@CartId, @ProductId, @Quantity)";

                            using (var insertCmd = new SqlCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@CartId", cartId);
                                insertCmd.Parameters.AddWithValue("@ProductId", productId);
                                insertCmd.Parameters.AddWithValue("@Quantity", quantity);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                var updateCartQuery = "UPDATE Cart SET UpdatedAt = GETDATE() WHERE CartId = @CartId";
                using (var cmd = new SqlCommand(updateCartQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@CartId", cartId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _connection?.Dispose();
        }
    }

    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string TrackingNumber { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string DeliveryAddress { get; set; }
        public string PaymentMethod { get; set; }

        public ObservableCollection<OrderItemViewModel> OrderItems { get; set; }
    }

    public class OrderItemViewModel
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public BitmapImage ProductImageSource { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
}