using ElectroStore1.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using System.Data.SqlClient;

namespace ElectroStore1.Pages.MainPages
{
    public partial class MainPage : Page
    {
        public ObservableCollection<ProductViewModel> Products { get; set; }
        public MainPage()
        {
            InitializeComponent();
            LoadProducts();
            this.DataContext = this;
        }
        private void LoadProducts()
        {
            using (SqlConnection conn = DBConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    string sql = @"
                SELECT 
                    p.ProductName AS Name,
                    b.BrandName AS Brand,
                    p.Price AS OriginalPrice,
                    p.DiscountPercent,
                    p.DiscountedPrice,
                    p.StockCount,
                    p.ImagePath,
                    p.IsOnSale
                FROM Products p
                LEFT JOIN Brands b ON p.BrandId = b.BrandId
                WHERE p.IsActive = 1";

                    SqlCommand command = new SqlCommand(sql, conn);
                    SqlDataReader reader = command.ExecuteReader();

                    if (Products == null)
                        Products = new ObservableCollection<ProductViewModel>();
                    else
                        Products.Clear();

                    while (reader.Read())
                    {
                        string name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() : "Без названия";
                        string brand = reader["Brand"] != DBNull.Value ? reader["Brand"].ToString() : "Без бренда";

                        decimal originalPrice = reader["OriginalPrice"] != DBNull.Value ? Convert.ToDecimal(reader["OriginalPrice"]) : 0;
                        decimal discountedPrice = reader["DiscountedPrice"] != DBNull.Value ? Convert.ToDecimal(reader["DiscountedPrice"]) : originalPrice;
                        int DiscountPercent = (int)reader["DiscountPercent"];

                        int stockCount = reader["StockCount"] != DBNull.Value ? Convert.ToInt32(reader["StockCount"]) : 0;
                        bool IsOnSale = (bool)reader["IsOnSale"];

                        string imagePath = reader["ImagePath"] != DBNull.Value ? reader["ImagePath"].ToString() : null;

                        Products.Add(new ProductViewModel
                        {
                            Name = name,
                            Brand = brand,
                            StockCount = stockCount,
                            Rating = 0.0,
                            ReviewsCount = 0,
                            OriginalPrice = originalPrice,
                            DiscountedPrice = discountedPrice,
                            DiscountPercent = DiscountPercent,
                            IsOnSale = IsOnSale,
                            ImagePath = string.IsNullOrWhiteSpace(imagePath) ? "/Images/defaultImage.png" : imagePath
                        });
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке товаров: {ex.Message}");
                }
            }
        }


    }
}
