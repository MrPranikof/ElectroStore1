using ElectroStore.ViewModels;
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

namespace ElectroStore.Pages
{
    /// <summary>
    /// Логика взаимодействия для PaymentWindow.xaml
    /// </summary>
    public partial class PaymentWindow : Window
    {
        private readonly FakePaymentService _paymentService = new FakePaymentService();
        public PaymentData PaymentData { get; } = new PaymentData();

        public PaymentWindow(decimal amount)
        {
            InitializeComponent();
            PaymentData.Amount = amount;
            DataContext = PaymentData;
        }

        private async void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
            {
                MessageBox.Show("Проверьте правильность данных карты!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PayButton.Visibility = Visibility.Collapsed;
            PaymentProgress.Visibility = Visibility.Visible;

            bool isSuccess = await _paymentService.ProcessPaymentAsync(PaymentData);

            PaymentProgress.Visibility = Visibility.Collapsed;
            PayButton.Visibility = Visibility.Visible;

            if (isSuccess)
            {
                MessageBox.Show("Оплата прошла успешно!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Оплата не прошла. Попробуйте другую карту.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            return !string.IsNullOrWhiteSpace(PaymentData.CardNumber) &&
                   PaymentData.CardNumber.Replace(" ", "").Length == 16 &&
                   !string.IsNullOrWhiteSpace(PaymentData.ExpiryDate) &&
                   PaymentData.ExpiryDate.Length == 5 &&
                   !string.IsNullOrWhiteSpace(PaymentData.CVV) &&
                   PaymentData.CVV.Length == 3 &&
                   !string.IsNullOrWhiteSpace(PaymentData.CardHolderName);
        }

        private void CardNumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0)) e.Handled = true;

            var textBox = sender as TextBox;
            if (textBox.Text.Replace(" ", "").Length % 4 == 0 &&
                textBox.Text.Length > 0 &&
                !textBox.Text.EndsWith(" "))
            {
                textBox.Text += " ";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void ExpiryDateBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0)) e.Handled = true;

            var textBox = sender as TextBox;
            if (textBox.Text.Length == 2 && !textBox.Text.Contains("/"))
            {
                textBox.Text += "/";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }
    }
}
