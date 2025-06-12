using ElectroStore1.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;

namespace ElectroStore1.Pages.MainPages
{
    public partial class FavouritesPage : Page
    {
        public ObservableCollection<ProductViewModel> FavouriteProducts { get; set; }
        private int _userId;
        private int _roleId;
        public int UserId => _userId;
        public int RoleId => _roleId;

        public FavouritesPage(int userId, int roleId)
        {
            InitializeComponent();
            _userId = userId;
            _roleId = roleId;
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                NavigationService?.GoBack();
                return;
            }
            FavouriteProducts = new ObservableCollection<ProductViewModel>();
            DataContext = this;

            Dispatcher.BeginInvoke(new Action(async () => await LoadFavouritesAsync()), DispatcherPriority.Background);
        }

        private async Task LoadFavouritesAsync()
        {
            try
            {
                var favourites = await Task.Run(() => LoadFavouritesFromDatabase());

                await Dispatcher.InvokeAsync(() =>
                {
                    FavouriteProducts.Clear();
                    foreach (var product in favourites)
                    {
                        FavouriteProducts.Add(product);
                    }

                    EmptyFavouritesText.Visibility = FavouriteProducts.Count == 0 ?
                        Visibility.Visible : Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки избранного: {ex.Message}");
                Logger.Error("Ошибка загрузки избранного", ex);
            }
        }

        private List<ProductViewModel> LoadFavouritesFromDatabase()
        {
            var products = new List<ProductViewModel>();

            using (SqlConnection connection = DBConnection.GetConnection())
            {
                connection.Open();

                string query = @"
                SELECT p.ProductId, p.ProductName, p.Price, p.DiscountedPrice, 
                       p.DiscountPercent, p.StockCount, p.ProductImage, b.BrandName,
                       p.IsOnSale,
                       ISNULL(AVG(CAST(r.Rating AS FLOAT)), 0) as AverageRating,
                       COUNT(r.ReviewId) as ReviewCount
                FROM Favourites f
                JOIN Products p ON f.ProductId = p.ProductId
                JOIN Brands b ON p.BrandId = b.BrandId
                LEFT JOIN Reviews r ON p.ProductId = r.ProductId
                WHERE f.UserId = @UserId AND p.IsActive = 1
                GROUP BY p.ProductId, p.ProductName, p.Price, p.DiscountedPrice, 
                         p.DiscountPercent, p.StockCount, p.ProductImage, b.BrandName,
                         p.IsOnSale";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", _userId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var product = new ProductViewModel
                        {
                            ProductId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            OriginalPrice = reader.GetDecimal(2),
                            DiscountedPrice = reader.GetDecimal(3),
                            DiscountPercent = reader.GetInt32(4),
                            StockCount = reader.GetInt32(5),
                            Brand = reader.GetString(7),
                            IsOnSale = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                            IsFavourite = true,
                            Rating = reader.IsDBNull(9) ? 0 : Math.Round(reader.GetDouble(9), 1),
                            ReviewsCount = reader.IsDBNull(10) ? 0 : reader.GetInt32(10)
                        };

                        if (!reader.IsDBNull(6))
                        {
                            byte[] imageData = (byte[])reader[6];
                            Task.Run(() => LoadImageAsync(product, imageData));
                        }

                        products.Add(product);
                    }
                }
            }

            return products;
        }

        private async Task LoadImageAsync(ProductViewModel product, byte[] imageData)
        {
            try
            {
                await Task.Run(() =>
                {
                    var image = new BitmapImage();
                    using (var ms = new MemoryStream(imageData))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.DecodePixelWidth = 200;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        product.ProductImage = image;
                    }), DispatcherPriority.Background);
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка загрузки изображения для товара {product.ProductId}", ex);
            }
        }

        // Остальные методы остаются без изменений
        private void ToggleFavourite_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ProductViewModel product = button?.Tag as ProductViewModel;

            if (product == null) return;

            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM Favourites WHERE UserId = @UserId AND ProductId = @ProductId";
                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", _userId);
                        command.Parameters.AddWithValue("@ProductId", product.ProductId);
                        command.ExecuteNonQuery();
                    }

                    FavouriteProducts.Remove(product);

                    EmptyFavouritesText.Visibility = FavouriteProducts.Count == 0 ?
                        Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении из избранного: {ex.Message}");
            }
        }

        private void ProductImage_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ProductViewModel product = button?.Tag as ProductViewModel;

            if (product != null)
            {
                Logger.Info($"Переход на страницу товара: {product.Name} (ID: {product.ProductId})");
                NavigationService.Navigate(new ProductPage(product, _userId, _roleId));
            }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                return;
            }

            // Код добавления в корзину остается тем же
            Button button = sender as Button;
            ProductViewModel product = button?.DataContext as ProductViewModel;

            if (product != null)
            {
                if (product.StockCount <= 0)
                {
                    MessageBox.Show($"Товар '{product.Name}' отсутствует на складе и не может быть добавлен в корзину",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = DBConnection.GetConnection())
                    {
                        conn.Open();

                        int cartId = GetOrCreateCart(conn);

                        string checkItemQuery = @"SELECT CartItemId, Quantity 
                                FROM CartItems 
                                WHERE CartId = @CartId AND ProductId = @ProductId";

                        using (SqlCommand checkCmd = new SqlCommand(checkItemQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@CartId", cartId);
                            checkCmd.Parameters.AddWithValue("@ProductId", product.ProductId);

                            using (SqlDataReader reader = checkCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int cartItemId = reader.GetInt32(0);
                                    int currentQuantity = reader.GetInt32(1);
                                    reader.Close();

                                    if (currentQuantity + 1 > product.StockCount)
                                    {
                                        MessageBox.Show($"Невозможно добавить товар '{product.Name}'. Максимальное доступное количество: {product.StockCount}",
                                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                    }

                                    string updateQuery = @"UPDATE CartItems 
                                             SET Quantity = Quantity + 1 
                                             WHERE CartItemId = @CartItemId";

                                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                                    {
                                        updateCmd.Parameters.AddWithValue("@CartItemId", cartItemId);
                                        updateCmd.ExecuteNonQuery();
                                    }

                                    MessageBox.Show($"Товар '{product.Name}' добавлен в корзину! Количество: {currentQuantity + 1}",
                                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    reader.Close();

                                    string insertQuery = @"INSERT INTO CartItems (CartId, ProductId, Quantity) 
                                         VALUES (@CartId, @ProductId, 1)";

                                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                                    {
                                        insertCmd.Parameters.AddWithValue("@CartId", cartId);
                                        insertCmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                                        insertCmd.ExecuteNonQuery();
                                    }

                                    MessageBox.Show($"Товар '{product.Name}' добавлен в корзину!",
                                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }

                        string updateCartQuery = "UPDATE Cart SET UpdatedAt = GETDATE() WHERE CartId = @CartId";
                        using (SqlCommand updateCartCmd = new SqlCommand(updateCartQuery, conn))
                        {
                            updateCartCmd.Parameters.AddWithValue("@CartId", cartId);
                            updateCartCmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении товара в корзину: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Error("Ошибка при добавлении в корзину", ex);
                }
            }
        }

        private int GetOrCreateCart(SqlConnection connection)
        {
            string getCartQuery = "SELECT CartId FROM Cart WHERE UserId = @UserId";
            using (SqlCommand getCommand = new SqlCommand(getCartQuery, connection))
            {
                getCommand.Parameters.AddWithValue("@UserId", _userId);
                object result = getCommand.ExecuteScalar();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
            }

            string createCartQuery = @"INSERT INTO Cart (UserId) 
                             OUTPUT INSERTED.CartId 
                             VALUES (@UserId)";

            using (SqlCommand command = new SqlCommand(createCartQuery, connection))
            {
                command.Parameters.AddWithValue("@UserId", _userId);
                int newCartId = Convert.ToInt32(command.ExecuteScalar());
                Logger.Info($"Создана новая корзина ID: {newCartId} для пользователя {_userId}");
                return newCartId;
            }
        }

        private void ShowGuestRestrictionMessage()
        {
            string message = "Гостевой доступ ограничен.\nЧтобы получить полный доступ к избранному, необходимо зарегистрироваться.";
            string caption = "Требуется регистрация";

            var result = MessageBox.Show(message, caption,
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Question,
                                      MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                var registration = new Registration();
                registration.Show();
                Window.GetWindow(this)?.Close();
            }
        }
    }
}