using ElectroStore.Dialogs;
using ElectroStore.Pages;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ElectroStore1.Pages.MainPages
{
    public partial class BasketPage : Page
    {
        private int _userId;
        private int _cartId;

        public class CartItem : INotifyPropertyChanged
        {
            private int _quantity;

            public int CartItemId { get; set; }
            public int ProductId { get; set; }
            public string Name { get; set; }
            public string Brand { get; set; }
            public decimal Price { get; set; }
            public BitmapImage Image { get; set; }
            public int StockCount { get; set; }

            public int Quantity
            {
                get => _quantity;
                set
                {
                    if (_quantity != value)
                    {
                        _quantity = value;
                        OnPropertyChanged(nameof(Quantity));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<CartItem> cartItems = new ObservableCollection<CartItem>();

        public BasketPage(int userId)
        {
            _userId = userId;
            InitializeComponent();
            LoadCartFromDatabase();
            CartListView.ItemsSource = cartItems;
        }

        private void LoadCartFromDatabase()
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    _cartId = GetOrCreateCart(connection);

                    string sql = @"
                            SELECT 
                                ci.CartItemId,
                                p.ProductId,
                                p.ProductName AS Name,
                                b.BrandName AS Brand,
                                p.DiscountedPrice AS Price,
                                ci.Quantity,
                                p.StockCount,
                                p.ProductImage
                            FROM CartItems ci
                            JOIN Products p ON ci.ProductId = p.ProductId
                            LEFT JOIN Brands b ON p.BrandId = b.BrandId
                            WHERE ci.CartId = @CartId";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@CartId", _cartId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cartItems.Add(new CartItem
                                {
                                    CartItemId = reader.GetInt32(0),
                                    ProductId = reader.GetInt32(1),
                                    Name = reader.GetString(2),
                                    Brand = reader.IsDBNull(3) ? "Без бренда" : reader.GetString(3),
                                    Price = reader.GetDecimal(4),
                                    Quantity = reader.GetInt32(5),
                                    StockCount = reader.GetInt32(6),
                                    Image = reader.IsDBNull(7) ? null : LoadImageFromBytes((byte[])reader[7])
                                });
                            }
                        }
                    }
                }
                UpdateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке корзины: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetOrCreateCart(SqlConnection connection)
        {
            try
            {
                string sql = "SELECT CartId FROM Cart WHERE UserId = @UserId";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", _userId);
                    object result = command.ExecuteScalar();
                    if (result != null) return Convert.ToInt32(result);
                }

                sql = "INSERT INTO Cart (UserId) OUTPUT INSERTED.CartId VALUES (@UserId)";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", _userId);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при работе с корзиной: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }

        private BitmapImage LoadImageFromBytes(byte[] imageData)
        {
            if (imageData == null) return null;

            var image = new BitmapImage();
            using (var ms = new System.IO.MemoryStream(imageData))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }
            return image;
        }

        private void UpdateTotal()
        {
            decimal total = 0;
            foreach (var item in cartItems)
            {
                total += item.Price * item.Quantity;
            }
            TotalText.Text = $"{total}₽";
        }

        private async void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is CartItem item)
            {
                if (item.Quantity >= item.StockCount)
                {
                    MessageBox.Show("Нельзя добавить больше товара, чем есть на складе", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                item.Quantity++;
                await UpdateCartItemInDatabase(item);
                UpdateTotal();
            }
        }

        private async void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is CartItem item && item.Quantity > 1)
            {
                item.Quantity--;
                await UpdateCartItemInDatabase(item);
                UpdateTotal();
            }
        }

        private async void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is CartItem item)
            {
                if (MessageBox.Show("Удалить товар из корзины?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await RemoveCartItemFromDatabase(item.CartItemId);
                    cartItems.Remove(item);
                    UpdateTotal();
                }
            }
        }

        private async Task UpdateCartItemInDatabase(CartItem item)
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "UPDATE CartItems SET Quantity = @Quantity WHERE CartItemId = @CartItemId";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Quantity", item.Quantity);
                        command.Parameters.AddWithValue("@CartItemId", item.CartItemId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении корзины: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private async Task RemoveCartItemFromDatabase(int cartItemId)
        {
            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    await connection.OpenAsync();
                    string sql = "DELETE FROM CartItems WHERE CartItemId = @CartItemId";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@CartItemId", cartItemId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении товара: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var addressDialog = new AddressDialog();
            if (addressDialog.ShowDialog() != true) return;

            try
            {
                int addressId = await SaveAddress(addressDialog.Address);
                decimal totalAmount = CalculateTotalAmount();

                // Открываем окно оплаты
                var paymentWindow = new PaymentWindow(totalAmount);
                if (paymentWindow.ShowDialog() != true)
                {
                    MessageBox.Show("Оплата не была завершена", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Создаем заказ с учетом статуса оплаты
                int orderId = await CreateOrder(addressId, "paid");
                await MoveCartItemsToOrder(orderId);
                await ClearCart();

                cartItems.Clear();
                UpdateTotal();

                MessageBox.Show($"Заказ #{orderId} успешно оформлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<int> SaveAddress(Address address)
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                await connection.OpenAsync();

                string sql = @"
            INSERT INTO Addresses (UserId, PostalAddress, City, ZipCode, Country, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.AddressId
            VALUES (@UserId, @PostalAddress, @City, @ZipCode, @Country, GETDATE(), GETDATE())";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", _userId);
                    command.Parameters.AddWithValue("@PostalAddress", address.PostalAddress);
                    command.Parameters.AddWithValue("@City", address.City);
                    command.Parameters.AddWithValue("@ZipCode", address.ZipCode);
                    command.Parameters.AddWithValue("@Country", address.Country);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        private async Task<int> CreateOrder(int addressId, string paymentStatus)
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                await connection.OpenAsync();
                string sql = @"
            INSERT INTO Orders (UserId, AddressId, OrderDate, TotalAmount, Status, PaymentStatus) 
            OUTPUT INSERTED.OrderId
            VALUES (@UserId, @AddressId, GETDATE(), @TotalAmount, 'Новый', @PaymentStatus)";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserId", _userId);
                    command.Parameters.AddWithValue("@AddressId", addressId);
                    command.Parameters.AddWithValue("@TotalAmount", CalculateTotalAmount());
                    command.Parameters.AddWithValue("@PaymentStatus", paymentStatus);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        private decimal CalculateTotalAmount()
        {
            decimal total = 0;
            foreach (var item in cartItems)
            {
                total += item.Price * item.Quantity;
            }
            return total;
        }

        private async Task MoveCartItemsToOrder(int orderId)
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                await connection.OpenAsync();

                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    string insertSql = @"
            INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price)
            SELECT @OrderId, ProductId, Quantity, @Price
            FROM CartItems 
            WHERE CartId = @CartId AND ProductId = @ProductId";

                    string updateStockSql = @"
            UPDATE Products 
            SET StockCount = StockCount - @Quantity 
            WHERE ProductId = @ProductId";

                    foreach (var item in cartItems)
                    {
                        string checkStockSql = "SELECT StockCount FROM Products WHERE ProductId = @ProductId";
                        int currentStock;

                        using (SqlCommand checkCommand = new SqlCommand(checkStockSql, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                            currentStock = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        }

                        if (currentStock < item.Quantity)
                        {
                            throw new Exception($"Недостаточно товара '{item.Name}' на складе. Доступно: {currentStock}, запрошено: {item.Quantity}");
                        }

                        using (SqlCommand insertCommand = new SqlCommand(insertSql, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@OrderId", orderId);
                            insertCommand.Parameters.AddWithValue("@CartId", _cartId);
                            insertCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                            insertCommand.Parameters.AddWithValue("@Price", item.Price);
                            await insertCommand.ExecuteNonQueryAsync();
                        }

                        using (SqlCommand updateCommand = new SqlCommand(updateStockSql, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                            updateCommand.Parameters.AddWithValue("@ProductId", item.ProductId);
                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }

        private async Task ClearCart()
        {
            using (SqlConnection connection = DBConnection.GetConnection())
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM CartItems WHERE CartId = @CartId";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CartId", _cartId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}