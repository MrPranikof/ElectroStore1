using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Data.SqlClient;

namespace ElectroStore1.Views
{
    public partial class AddProductView : UserControl
    {
        public AddProductView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var conn = DBConnection.GetConnection();
                conn.Open();

                var catCmd = new SqlCommand("SELECT CategoryId, CategoryName FROM Categories", conn);
                var catReader = catCmd.ExecuteReader();
                var categories = new List<dynamic>();
                while (catReader.Read())
                {
                    categories.Add(new
                    {
                        CategoryId = (int)catReader["CategoryId"],
                        CategoryName = catReader["CategoryName"].ToString()
                    });
                }
                CategoryBox.ItemsSource = categories;
                catReader.Close();

                var brandCmd = new SqlCommand("SELECT BrandId, BrandName FROM Brands", conn);
                var brandReader = brandCmd.ExecuteReader();
                var brands = new List<dynamic>();
                while (brandReader.Read())
                {
                    brands.Add(new
                    {
                        BrandId = (int)brandReader["BrandId"],
                        BrandName = brandReader["BrandName"].ToString()
                    });
                }
                BrandBox.ItemsSource = brands;
                brandReader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке категорий и брендов: " + ex.Message);
            }
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Image Files (*.jpg;*.png)|*.jpg;*.png";

            if (dialog.ShowDialog() == true)
            {
                string fileName = Path.GetFileName(dialog.FileName);
                string destDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                Directory.CreateDirectory(destDir);
                string destPath = Path.Combine(destDir, fileName);

                File.Copy(dialog.FileName, destPath, true);
                ImagePathBox.Text = GetAbsolutePath($"/Images/{fileName}");
            }
        }
        public static string GetAbsolutePath(string relativePath)
        {
            relativePath = relativePath.TrimStart('\\', '/');

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        }
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = ProductNameBox.Text.Trim();
                decimal price = decimal.Parse(PriceBox.Text);
                int discount = int.Parse(DiscountBox.Text);
                int categoryId = (int)CategoryBox.SelectedValue;
                int brandId = (int)BrandBox.SelectedValue;
                string imagePath = ImagePathBox.Text.Trim();

                using var conn = DBConnection.GetConnection();
                conn.Open();

                var cmd = new SqlCommand(@"
                    INSERT INTO Products 
                    (ProductName, Price, DiscountPercent, CategoryId, BrandId, ImagePath, IsActive, CreatedAt)
                    VALUES 
                    (@name, @price, @discount, @category, @brand, @img, 1, GETDATE())", conn);

                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@discount", discount);
                cmd.Parameters.AddWithValue("@category", categoryId);
                cmd.Parameters.AddWithValue("@brand", brandId);
                cmd.Parameters.AddWithValue("@img", imagePath);
                cmd.ExecuteNonQuery();

                MessageBox.Show("✅ Товар успешно добавлен!");

                ProductNameBox.Text = "";
                PriceBox.Text = "";
                DiscountBox.Text = "";
                ImagePathBox.Text = "";
                CategoryBox.SelectedIndex = -1;
                BrandBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении товара: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}