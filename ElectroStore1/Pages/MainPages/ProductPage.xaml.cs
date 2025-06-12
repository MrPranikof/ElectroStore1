using ElectroStore1.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ElectroStore1.Pages.MainPages
{
    public class StarRating
    {
        public int Value { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ProductReview
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string RatingStars { get; set; }
        public string ReviewText { get; set; }
        public string ReviewDate { get; set; }
        public bool IsCurrentUserReview { get; set; }
    }
    public partial class ProductPage : Page
    {
        private ProductViewModel _product;
        private int _userId;
        private int _roleId;
        private int _selectedRating = 0;
        private ObservableCollection<dynamic> _reviews = new ObservableCollection<dynamic>();

        public ProductPage()
        {
            InitializeComponent();
            InitializeStarRatings();
        }

        public ProductPage(ProductViewModel product, int userId, int roleId) : this()
        {
            _product = product;
            _userId = userId;
            _roleId = roleId;
            LoadProductDetails();
            LoadProductById(product.ProductId);
            CheckIfFavorite();
            LoadReviews();
        }

        private void InitializeStarRatings()
        {
            var stars = new ObservableCollection<StarRating>();
            for (int i = 1; i <= 5; i++)
            {
                stars.Add(new StarRating { Value = i, IsSelected = false });
            }
            RatingStars.ItemsSource = stars;
        }

        private void LoadReviews()
        {
            _reviews.Clear();

            using (SqlConnection conn = DBConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    string sql = @"
                SELECT 
                    r.ReviewId,
                    r.UserId,
                    u.Username AS UserName,
                    r.Rating,
                    r.Comment,
                    r.ReviewDate
                FROM Reviews r
                INNER JOIN Users u ON r.UserId = u.UserId
                WHERE r.ProductId = @ProductId
                ORDER BY r.ReviewDate DESC";

                    SqlCommand command = new SqlCommand(sql, conn);
                    command.Parameters.AddWithValue("@ProductId", _product.ProductId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _reviews.Add(new ProductReview
                            {
                                ReviewId = Convert.ToInt32(reader["ReviewId"]),
                                UserId = Convert.ToInt32(reader["UserId"]),
                                UserName = reader["UserName"].ToString(),
                                Rating = Convert.ToInt32(reader["Rating"]),
                                RatingStars = new string('★', Convert.ToInt32(reader["Rating"])) +
                                            new string('☆', 5 - Convert.ToInt32(reader["Rating"])),
                                ReviewText = reader["Comment"] is DBNull ? string.Empty : reader["Comment"].ToString(),
                                ReviewDate = Convert.ToDateTime(reader["ReviewDate"]).ToString("dd.MM.yyyy"),
                                IsCurrentUserReview = Convert.ToInt32(reader["UserId"]) == _userId
                            });
                        }
                    }

                    ReviewsList.ItemsSource = _reviews;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке отзывов: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void StarRating_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is StarRating star)
            {
                _selectedRating = star.Value;

                var stars = RatingStars.ItemsSource as ObservableCollection<StarRating>;
                for (int i = 0; i < stars.Count; i++)
                {
                    stars[i].IsSelected = (i + 1) <= _selectedRating;
                }
                RatingStars.ItemsSource = null;
                RatingStars.ItemsSource = stars;
            }
        }

        private void SubmitReview_Click(object sender, RoutedEventArgs e)
        {
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                return;
            }

            if (_selectedRating == 0)
            {
                MessageBox.Show("Пожалуйста, выберите оценку", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string reviewText = ReviewTextBox.Text.Trim();

            try
            {
                using (SqlConnection conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    // Проверка существующего отзыва
                    string checkQuery = @"SELECT COUNT(*) FROM Reviews 
                                WHERE UserId = @UserId AND ProductId = @ProductId";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@UserId", _userId);
                        checkCmd.Parameters.AddWithValue("@ProductId", _product.ProductId);

                        int existingReviews = (int)checkCmd.ExecuteScalar();
                        if (existingReviews > 0)
                        {
                            MessageBox.Show("Вы уже оставили отзыв на этот товар", "Внимание",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }

                    // Добавление отзыва (с комментарием)
                    string insertQuery = @"INSERT INTO Reviews 
                                 (UserId, ProductId, Rating, Comment, ReviewDate) 
                                 VALUES (@UserId, @ProductId, @Rating, @Comment, GETDATE())";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@UserId", _userId);
                        insertCmd.Parameters.AddWithValue("@ProductId", _product.ProductId);
                        insertCmd.Parameters.AddWithValue("@Rating", _selectedRating);
                        insertCmd.Parameters.AddWithValue("@Comment", string.IsNullOrEmpty(reviewText) ? DBNull.Value : (object)reviewText);

                        insertCmd.ExecuteNonQuery();

                        MessageBox.Show("Отзыв успешно добавлен!", "Успех",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        // Очищаем форму
                        ReviewTextBox.Clear();
                        _selectedRating = 0;
                        InitializeStarRatings();

                        // Обновляем отзывы и рейтинг товара
                        LoadReviews();
                        LoadProductById(_product.ProductId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении отзыва: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteReview_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int reviewId)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить свой отзыв?",
                                           "Подтверждение", MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = DBConnection.GetConnection())
                        {
                            conn.Open();
                            string deleteQuery = "DELETE FROM Reviews WHERE ReviewId = @ReviewId";
                            using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn))
                            {
                                deleteCmd.Parameters.AddWithValue("@ReviewId", reviewId);
                                deleteCmd.ExecuteNonQuery();

                                MessageBox.Show("Отзыв успешно удален", "Успех",
                                              MessageBoxButton.OK, MessageBoxImage.Information);

                                // Обновляем отзывы и рейтинг товара
                                LoadReviews();
                                LoadProductById(_product.ProductId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении отзыва: {ex.Message}", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void LoadProductDetails()
        {
            if (_product != null)
            {
                this.DataContext = _product;
            }
        }

        private void LoadProductById(int productId)
        {
            using (SqlConnection conn = DBConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    string sql = @"
                SELECT 
                    p.ProductId,
                    p.ProductName AS Name,
                    b.BrandName AS Brand,
                    p.Price AS OriginalPrice,
                    p.DiscountPercent,
                    p.DiscountedPrice,
                    p.StockCount,
                    p.ProductImage,
                    p.IsOnSale,
                    p.Description
                FROM Products p
                LEFT JOIN Brands b ON p.BrandId = b.BrandId
                WHERE p.ProductId = @ProductId";

                    SqlCommand command = new SqlCommand(sql, conn);
                    command.Parameters.AddWithValue("@ProductId", productId);

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        _product = new ProductViewModel
                        {
                            ProductId = (int)reader["ProductId"],
                            Name = reader["Name"]?.ToString() ?? "Без названия",
                            Brand = reader["Brand"]?.ToString() ?? "Без бренда",
                            OriginalPrice = reader["OriginalPrice"] != DBNull.Value ? Convert.ToDecimal(reader["OriginalPrice"]) : 0,
                            DiscountedPrice = reader["DiscountedPrice"] != DBNull.Value ? Convert.ToDecimal(reader["DiscountedPrice"]) : 0,
                            DiscountPercent = reader["DiscountPercent"] != DBNull.Value ? (int)reader["DiscountPercent"] : 0,
                            StockCount = reader["StockCount"] != DBNull.Value ? Convert.ToInt32(reader["StockCount"]) : 0,
                            IsOnSale = reader["IsOnSale"] != DBNull.Value && (bool)reader["IsOnSale"],
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "Описание отсутствует"
                        };

                        if (reader["ProductImage"] != DBNull.Value)
                        {
                            byte[] imageData = (byte[])reader["ProductImage"];
                            using (MemoryStream ms = new MemoryStream(imageData))
                            {
                                BitmapImage image = new BitmapImage();
                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.StreamSource = ms;
                                image.EndInit();
                                _product.ProductImage = image;
                            }
                        }

                        // Загружаем рейтинг и количество отзывов отдельно
                        LoadProductRating(_product);
                        this.DataContext = _product;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadProductRating(ProductViewModel product)
        {
            using (SqlConnection conn = DBConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    string sql = @"
                SELECT 
                    ISNULL(AVG(CAST(Rating AS FLOAT)), 0) as Rating,
                    COUNT(ReviewId) as ReviewsCount
                FROM Reviews
                WHERE ProductId = @ProductId";

                    SqlCommand command = new SqlCommand(sql, conn);
                    command.Parameters.AddWithValue("@ProductId", product.ProductId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            product.Rating = reader["Rating"] != DBNull.Value ? Convert.ToDouble(reader["Rating"]) : 0.0;
                            product.ReviewsCount = reader["ReviewsCount"] != DBNull.Value ? Convert.ToInt32(reader["ReviewsCount"]) : 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке рейтинга: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CheckIfFavorite()
        {
            if (_product == null) return;

            using (SqlConnection conn = DBConnection.GetConnection())
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT COUNT(1) FROM Favourites WHERE UserId = @UserId AND ProductId = @ProductId";
                    SqlCommand command = new SqlCommand(sql, conn);
                    command.Parameters.AddWithValue("@UserId", _userId);
                    command.Parameters.AddWithValue("@ProductId", _product.ProductId);

                    int isFavorite = (int)command.ExecuteScalar();
                    _product.IsFavourite = isFavorite > 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при проверке избранного: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                return;
            }

            if (_product == null || _product.StockCount <= 0) return;

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
                        checkCmd.Parameters.AddWithValue("@ProductId", _product.ProductId);

                        using (SqlDataReader reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int cartItemId = reader.GetInt32(0);
                                int currentQuantity = reader.GetInt32(1);
                                reader.Close();

                                if (currentQuantity + 1 > _product.StockCount)
                                {
                                    MessageBox.Show($"Невозможно добавить товар '{_product.Name}'. Максимальное доступное количество: {_product.StockCount}",
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

                                MessageBox.Show($"Товар '{_product.Name}' добавлен в корзину! Количество: {currentQuantity + 1}",
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
                                    insertCmd.Parameters.AddWithValue("@ProductId", _product.ProductId);
                                    insertCmd.ExecuteNonQuery();
                                }

                                MessageBox.Show($"Товар '{_product.Name}' добавлен в корзину!",
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
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void AddToFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (_roleId == 4)
            {
                ShowGuestRestrictionMessage();
                return;
            }
            if (_product == null) return;

            try
            {
                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    if (_product.IsFavourite)
                    {
                        // Удаляем из избранного
                        string deleteQuery = "DELETE FROM Favourites WHERE UserId = @UserId AND ProductId = @ProductId";
                        using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", _userId);
                            command.Parameters.AddWithValue("@ProductId", _product.ProductId);
                            command.ExecuteNonQuery();
                        }
                        _product.IsFavourite = false;
                        MessageBox.Show($"Товар '{_product.Name}' удален из избранного", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Добавляем в избранное
                        string insertQuery = "INSERT INTO Favourites (UserId, ProductId, AddedDate) VALUES (@UserId, @ProductId, GETDATE())";
                        using (SqlCommand command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", _userId);
                            command.Parameters.AddWithValue("@ProductId", _product.ProductId);
                            command.ExecuteNonQuery();
                        }
                        _product.IsFavourite = true;
                        MessageBox.Show($"Товар '{_product.Name}' добавлен в избранное", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении избранного: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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