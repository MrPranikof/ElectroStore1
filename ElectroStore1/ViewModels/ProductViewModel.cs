using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ElectroStore1.ViewModels
{
    public class ProductViewModel : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        private BitmapImage _productImage;
        public BitmapImage ProductImage
        {
            get => _productImage;
            set
            {
                _productImage = value;
                OnPropertyChanged();
            }
        }
        public string Name { get; set; }
        public string Brand { get; set; }
        public int StockCount { get; set; }
        public double Rating { get; set; }
        public int ReviewsCount { get; set; }
        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public int DiscountPercent { get; set; }
        public int ViewCount {  get; set; }
        public bool IsOnSale { get; set; }

        private bool _isFavourite;
        public bool IsFavourite
        {
            get => _isFavourite;
            set
            {
                _isFavourite = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}