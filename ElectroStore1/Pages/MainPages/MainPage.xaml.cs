using ElectroStore1.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Threading;

namespace ElectroStore1.Pages.MainPages
{
    public partial class MainPage : Page
    {
        public ObservableCollection<ProductViewModel> RecommendedProducts { get; set; }
        public ObservableCollection<ProductViewModel> DiscountProducts { get; set; }
        private int _userId;
        private int _roleId;

        public MainPage(int userId, int roleId)
        {
            InitializeComponent();
            _userId = userId;
            _roleId = roleId;
            RecommendedProducts = new ObservableCollection<ProductViewModel>();
            DiscountProducts = new ObservableCollection<ProductViewModel>();
            this.DataContext = this;

            // Асинхронная загрузка
            Dispatcher.BeginInvoke(new Action(async () => await LoadProductsAsync()), DispatcherPriority.Background);
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var recommendedTask = LoadProductSectionAsync(GetRecommendedProductsQuery());
                var discountTask = LoadProductSectionAsync(GetDiscountProductsQuery());

                var results = await Task.WhenAll(recommendedTask, discountTask);

                // Обновляем UI в главном потоке
                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var product in results[0])
                        RecommendedProducts.Add(product);

                    foreach (var product in results[1])
                        DiscountProducts.Add(product);
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при загрузке товаров", ex);
                MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}");
            }
        }

        private string GetRecommendedProductsQuery()
        {
            return @"
            SELECT TOP 8 
                p.ProductId,
                p.ProductName AS Name,
                b.BrandName AS Brand,
                p.Price AS OriginalPrice,
                p.DiscountPercent,
                p.DiscountedPrice,
                p.StockCount,
                p.ProductImage,
                p.IsOnSale,
                ISNULL(p.ViewCount, 0) AS ViewCount,
                CASE WHEN f.UserId IS NULL THEN 0 ELSE 1 END AS IsFavourite,
                ISNULL(AVG(CAST(r.Rating AS FLOAT)), 0) as AverageRating,
                COUNT(r.ReviewId) as ReviewCount
            FROM Products p
            LEFT JOIN Brands b ON p.BrandId = b.BrandId
            LEFT JOIN Favourites f ON p.ProductId = f.ProductId AND f.UserId = @UserId
            LEFT JOIN Reviews r ON p.ProductId = r.ProductId
            WHERE p.IsActive = 1
            GROUP BY p.ProductId, p.ProductName, b.BrandName, p.Price, 
                     p.DiscountPercent, p.DiscountedPrice, p.StockCount, 
                     p.ProductImage, p.IsOnSale, p.ViewCount, f.UserId
            ORDER BY ISNULL(p.ViewCount, 0) DESC";
        }

        private string GetDiscountProductsQuery()
        {
            return @"
            SELECT TOP 8 
                p.ProductId,
                p.ProductName AS Name,
                b.BrandName AS Brand,
                p.Price AS OriginalPrice,
                p.DiscountPercent,
                p.DiscountedPrice,
                p.StockCount,
                p.ProductImage,
                p.IsOnSale,
                ISNULL(p.ViewCount, 0) AS ViewCount,
                CASE WHEN f.UserId IS NULL THEN 0 ELSE 1 END AS IsFavourite,
                ISNULL(AVG(CAST(r.Rating AS FLOAT)), 0) as AverageRating,
                COUNT(r.ReviewId) as ReviewCount
            FROM Products p
            LEFT JOIN Brands b ON p.BrandId = b.BrandId
            LEFT JOIN Favourites f ON p.ProductId = f.ProductId AND f.UserId = @UserId
            LEFT JOIN Reviews r ON p.ProductId = r.ProductId
            WHERE p.IsActive = 1 AND p.DiscountPercent > 0
            GROUP BY p.ProductId, p.ProductName, b.BrandName, p.Price, 
                     p.DiscountPercent, p.DiscountedPrice, p.StockCount, 
                     p.ProductImage, p.IsOnSale, p.ViewCount, f.UserId
            ORDER BY p.DiscountPercent DESC";
        }

        private async Task<List<ProductViewModel>> LoadProductSectionAsync(string sql)
        {
            return await Task.Run(() =>
            {
                var products = new List<ProductViewModel>();

                try
                {
                    using (SqlConnection conn = DBConnection.GetConnection())
                    {
                        conn.Open();
                        using (SqlCommand command = new SqlCommand(sql, conn))
                        {
                            command.Parameters.AddWithValue("@UserId", _userId);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var product = CreateProductFromReader(reader);
                                    products.Add(product);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при загрузке раздела товаров", ex);
                }

                return products;
            });
        }

        private ProductViewModel CreateProductFromReader(SqlDataReader reader)
        {
            var product = new ProductViewModel
            {
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? "Без названия" : reader.GetString(reader.GetOrdinal("Name")),
                Brand = reader.IsDBNull(reader.GetOrdinal("Brand")) ? "Без бренда" : reader.GetString(reader.GetOrdinal("Brand")),
                ViewCount = reader.IsDBNull(reader.GetOrdinal("ViewCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ViewCount")),
                OriginalPrice = reader.IsDBNull(reader.GetOrdinal("OriginalPrice")) ? 0 : reader.GetDecimal(reader.GetOrdinal("OriginalPrice")),
                DiscountedPrice = reader.IsDBNull(reader.GetOrdinal("DiscountedPrice")) ?
                    reader.GetDecimal(reader.GetOrdinal("OriginalPrice")) : reader.GetDecimal(reader.GetOrdinal("DiscountedPrice")),
                DiscountPercent = reader.IsDBNull(reader.GetOrdinal("DiscountPercent")) ? 0 : reader.GetInt32(reader.GetOrdinal("DiscountPercent")),
                StockCount = reader.IsDBNull(reader.GetOrdinal("StockCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("StockCount")),
                IsOnSale = !reader.IsDBNull(reader.GetOrdinal("IsOnSale")) && reader.GetBoolean(reader.GetOrdinal("IsOnSale")),
                IsFavourite = !reader.IsDBNull(reader.GetOrdinal("IsFavourite")) && reader.GetInt32(reader.GetOrdinal("IsFavourite")) == 1,
                Rating = reader.IsDBNull(reader.GetOrdinal("AverageRating")) ? 0 : Math.Round(reader.GetDouble(reader.GetOrdinal("AverageRating")), 1),
                ReviewsCount = reader.IsDBNull(reader.GetOrdinal("ReviewCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ReviewCount"))
            };

            // Загрузка изображения будет происходить асинхронно
            if (!reader.IsDBNull(reader.GetOrdinal("ProductImage")))
            {
                byte[] imageData = (byte[])reader["ProductImage"];
                // Отложенная загрузка изображения
                Task.Run(() => LoadImageAsync(product, imageData));
            }

            return product;
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
                        image.DecodePixelWidth = 200; // Ограничиваем размер для экономии памяти
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                    }

                    // Обновляем UI в главном потоке
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

        // Остальные методы остаются без изменений...
        private void ProductImage_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ProductViewModel product = button?.Tag as ProductViewModel;

            if (product != null)
            {
                try
                {
                    using (SqlConnection connection = DBConnection.GetConnection())
                    {
                        connection.Open();
                        string updateQuery = "UPDATE Products SET ViewCount = ViewCount + 1 WHERE ProductId = @ProductId";
                        using (SqlCommand command = new SqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@ProductId", product.ProductId);
                            command.ExecuteNonQuery();
                        }
                        product.ViewCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка при обновлении счетчика просмотров для товара {product.ProductId}", ex);
                }

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

                                    if (product.StockCount < 1)
                                    {
                                        MessageBox.Show($"Товар '{product.Name}' отсутствует на складе",
                                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                    }

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

        private void ToggleFavourite_Click(object sender, RoutedEventArgs e)
        {
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                return;
            }
            Button button = sender as Button;
            ProductViewModel product = button?.Tag as ProductViewModel;

            if (product == null) return;

            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    if (product.IsFavourite)
                    {
                        string deleteQuery = "DELETE FROM Favourites WHERE UserId = @UserId AND ProductId = @ProductId";
                        using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", _userId);
                            command.Parameters.AddWithValue("@ProductId", product.ProductId);
                            command.ExecuteNonQuery();
                        }
                        product.IsFavourite = false;
                        Logger.Info($"Товар {product.Name} удален из избранного");
                    }
                    else
                    {
                        string insertQuery = "INSERT INTO Favourites (UserId, ProductId, AddedDate) VALUES (@UserId, @ProductId, GETDATE())";
                        using (SqlCommand command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", _userId);
                            command.Parameters.AddWithValue("@ProductId", product.ProductId);
                            command.ExecuteNonQuery();
                        }
                        product.IsFavourite = true;
                        Logger.Info($"Товар {product.Name} добавлен в избранное");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении избранного: {ex.Message}");
                Logger.Error("Ошибка при обновлении избранного", ex);
            }
        }

        private void ShowGuestRestrictionMessage()
        {
            string message = "Гостевой доступ ограничен.\nЧтобы получить полный доступ к этой функции, необходимо зарегистрироваться.";
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