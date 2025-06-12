using System;
using System.Collections.Generic;
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
using System.Data.SqlClient;

namespace ElectroStore1.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddCategory.xaml
    /// </summary>
    public partial class AddCategory : Window
    {
        public AddCategory()
        {
            InitializeComponent();
        }
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string categoryName = CategoryNameBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    MessageBox.Show("Введите название категории", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection connection = DBConnection.GetConnection())
                {
                    connection.Open();

                    var cmd = new SqlCommand(
                        "INSERT INTO Categories (CategoryName, CreatedAt) VALUES (@name, GETDATE())",
                        connection);

                    cmd.Parameters.AddWithValue("@name", categoryName);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Категория успешно добавлена", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении категории: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
