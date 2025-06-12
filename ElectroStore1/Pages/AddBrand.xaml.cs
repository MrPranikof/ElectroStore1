using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Windows.Shapes;

namespace ElectroStore1.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddBrand.xaml
    /// </summary>
    public partial class AddBrand : Window
    {
        public AddBrand()
        {
            InitializeComponent();
        }

        private void AddBrand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string brandName = BrandNameBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(brandName))
                {
                    MessageBox.Show("Введите название бренда", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    var cmd = new SqlCommand(
                        "INSERT INTO Brands (BrandName, CreatedAt) VALUES (@name, GETDATE())",
                        connection);

                    cmd.Parameters.AddWithValue("@name", brandName);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Бренд успешно добавлен", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении бренда: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
