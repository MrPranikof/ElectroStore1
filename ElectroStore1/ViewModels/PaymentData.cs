using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStore.ViewModels
{
    public class PaymentData
    {
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }
        public string CardHolderName { get; set; }
        public decimal Amount { get; set; }
    }
    public class FakePaymentService
    {
        public async Task<bool> ProcessPaymentAsync(PaymentData paymentData)
        {
            await Task.Delay(2000);
            Random random = new Random();
            return random.Next(0, 10) < 9; // 90% успешных платежей
        }
    }
}
