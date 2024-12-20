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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArtWiz.View.Widgets
{
    /// <summary>
    /// Interaction logic for SmoothProgressbar.xaml
    /// </summary>
    public partial class SmoothProgressbar : UserControl
    {
        private double _currentValue = 0;

        // DependencyProperty để binding giá trị
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(SmoothProgressbar),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public SmoothProgressbar()
        {
            InitializeComponent();
            SizeChanged += OnSizeChanged; // Lắng nghe thay đổi kích thước
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmoothProgressbar progressBar)
            {
                progressBar.AnimateProgress((double)e.NewValue);
            }
        }

        private void AnimateProgress(double newValue)
        {
            // Hủy animation cũ nếu đang chạy
            ProgressIndicator.BeginAnimation(WidthProperty, null);

            // Tính toán chiều rộng hiện tại của ProgressBar
            double currentWidth = ProgressIndicator.ActualWidth;

            // Tính toán chiều rộng mới dựa trên giá trị mới
            double newWidth = Math.Max(0, Math.Min(ActualWidth, ActualWidth * (newValue / 100)));

            // Tạo animation
            var animation = new DoubleAnimation
            {
                From = currentWidth, // Bắt đầu từ chiều rộng hiện tại
                To = newWidth,       // Đến chiều rộng mới
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase() // Hiệu ứng mượt mà
            };

            // Gán animation vào Width
            ProgressIndicator.BeginAnimation(WidthProperty, animation);

            // Cập nhật giá trị hiện tại
            _currentValue = newValue;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Khi kích thước thay đổi, cập nhật lại chiều rộng của ProgressIndicator
            double adjustedWidth = Math.Max(0, Math.Min(ActualWidth, ActualWidth * (_currentValue / 100)));
            ProgressIndicator.Width = adjustedWidth;
        }
    }
}
