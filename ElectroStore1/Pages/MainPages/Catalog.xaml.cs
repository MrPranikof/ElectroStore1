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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;
using ElectroStore.Pages.MainPages;


namespace ElectroStore1.Windows.Pages
{
    /// <summary>
    /// Логика взаимодействия для Catalog.xaml
    /// </summary>
    public partial class Catalog : Page
    {
        private int _userId;
        private int _roleId;
        private bool _isInitialLoad = true;
        public Catalog()
        {
            InitializeComponent();
            Loaded += Catalog_Loaded;
        }

        public Catalog(int userId, int roleId) : this()
        {
            _userId = userId;
            _roleId = roleId;
        }

        private void Catalog_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialLoad)
            {
                LoadInitialData();
                LoadCategories();
                _isInitialLoad = false;
            }
        }

        private void LoadInitialData()
        {
            try
            {
                // Загружаем категории при первой загрузке
                var categories = GetCategoriesFromDatabase();
                CategoriesItemsControl.ItemsSource = categories;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = GetCategoriesFromDatabase();
                CategoriesItemsControl.ItemsSource = categories;
                CategoriesItemsControl.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<CategoryViewModel> GetCategoriesFromDatabase()
        {
            var categories = new List<CategoryViewModel>();

            using (var connection = DBConnection.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT c.CategoryId, c.CategoryName, 
                           COUNT(p.ProductId) as ProductCount
                    FROM Categories c
                    LEFT JOIN Products p ON c.CategoryId = p.CategoryId
                    GROUP BY c.CategoryId, c.CategoryName
                    ORDER BY c.CategoryName";

                using (var cmd = new SqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new CategoryViewModel
                            {
                                CategoryId = reader.GetInt32(0),
                                CategoryName = reader.GetString(1),
                                ProductCount = reader.GetInt32(2)
                            });
                        }
                    }
                }
            }

            return categories;
        }

        private ICommand _categoryClickCommand;
        public ICommand CategoryClickCommand
        {
            get
            {
                if (_categoryClickCommand == null)
                {
                    _categoryClickCommand = new RelayCommand<CategoryViewModel>(OnCategoryClick);
                }
                return _categoryClickCommand;
            }
        }

        private void OnCategoryClick(CategoryViewModel category)
        {
            if (category != null)
            {
                NavigationService?.Navigate(new CategoryProductsPage(category.CategoryId, category.CategoryName, _userId, _roleId));
            }
        }
    }

    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke((T)parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
    }
}
