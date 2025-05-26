using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace ElectroStore1.ViewModels
{
    public class ProductViewModel
    {
        public string ImagePath { get; set; } = "/Images/defaultImage.png";
        public string Name { get; set; }
        public string Brand { get; set; }
        public int StockCount { get; set; }
        public double Rating { get; set; }
        public int ReviewsCount { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public int DiscountPercent { get; set; }
        public bool IsOnSale { get; set; }
    }
}
