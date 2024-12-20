using ArtWiz.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ArtWiz.View.Widgets
{
    public partial class IconToggle : UserControl
    {

        public static readonly DependencyProperty IconBorderThicknessProperty
           = DependencyProperty.Register("IconBorderThickness", typeof(Thickness), typeof(IconToggle),
                                         new FrameworkPropertyMetadata(
                                               new Thickness(0, 0, 0, 0),
                                               FrameworkPropertyMetadataOptions.AffectsMeasure),
                                         new ValidateValueCallback(IsMarginValid));

        private static bool IsMarginValid(object value)
        {
            Thickness m = (Thickness)value;
            return m.IsValid(true, false, true, false);
        }

        public Thickness IconBorderThickness
        {
            get { return (Thickness)GetValue(IconBorderThicknessProperty); }
            set { SetValue(IconBorderThicknessProperty, value); }
        }

        public static readonly DependencyProperty IsEnableToggleClickProperty =
               DependencyProperty.Register(
                       "IsEnableToggleClick",
                       typeof(bool),
                       typeof(IconToggle),
                       new PropertyMetadata(true));

        public bool IsEnableToggleClick
        {
            get
            {
                return (bool)GetValue(IsEnableToggleClickProperty);
            }
            set
            {
                SetValue(IsEnableToggleClickProperty, value);
            }
        }

        public static readonly DependencyProperty IsEllipseProperty =
                DependencyProperty.Register(
                        "IsEllipse",
                        typeof(bool),
                        typeof(IconToggle),
                        new FrameworkPropertyMetadata(
                                true,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal));

        public bool IsEllipse
        {
            get
            {
                return (bool)GetValue(IsEllipseProperty);
            }
            set
            {
                SetValue(IsEllipseProperty, value);
            }
        }

        public static readonly DependencyProperty IsCheckedProperty =
                DependencyProperty.Register(
                        "IsChecked",
                        typeof(bool?),
                        typeof(IconToggle),
                        new FrameworkPropertyMetadata(
                                false,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal));

        public bool? IsChecked
        {
            get
            {
                return (bool?)GetValue(IsCheckedProperty);
            }
            set
            {
                SetValue(IsCheckedProperty, value);
            }
        }

        public static readonly DependencyProperty StrokeThicknessProperty =
                DependencyProperty.Register(
                        "StrokeThickness",
                        typeof(double),
                        typeof(IconToggle),
                        new FrameworkPropertyMetadata(
                                5d,
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public static readonly DependencyProperty FillProperty =
                DependencyProperty.Register(
                        "Fill",
                        typeof(Brush),
                        typeof(IconToggle),
                        new FrameworkPropertyMetadata(
                                new SolidColorBrush(Colors.Transparent),
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static readonly DependencyProperty StrokeProperty =
                DependencyProperty.Register(
                        "Stroke",
                        typeof(Brush),
                        typeof(IconToggle),
                        new FrameworkPropertyMetadata(
                                new SolidColorBrush(Colors.Aqua),
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));
        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public static readonly DependencyProperty DefaultIconDataProperty = DependencyProperty.Register(
            "DefaultIconData",
            typeof(Geometry),
            typeof(IconToggle),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
            null);

        public Geometry DefaultIconData
        {
            get
            {
                return (Geometry)GetValue(DefaultIconDataProperty);
            }
            set
            {
                SetValue(DefaultIconDataProperty, value);
            }
        }

        public static readonly DependencyProperty OnIconDataProperty = DependencyProperty.Register(
            "OnIconData",
            typeof(Geometry),
            typeof(IconToggle),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
            null);

        public Geometry OnIconData
        {
            get
            {
                return (Geometry)GetValue(OnIconDataProperty) ?? DefaultIconData;
            }
            set
            {
                SetValue(OnIconDataProperty, value);
            }
        }

        public static readonly DependencyProperty OffIconDataProperty = DependencyProperty.Register(
            "OffIconData",
            typeof(Geometry),
            typeof(IconToggle),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
            null);

        public Geometry OffIconData
        {
            get
            {
                return (Geometry)GetValue(OffIconDataProperty) ?? DefaultIconData;
            }
            set
            {
                SetValue(OffIconDataProperty, value);
            }
        }

        public static readonly DependencyProperty IconRatioProperty =
                DependencyProperty.Register(
                        "IconRatio",
                        typeof(double),
                        typeof(IconToggle),
                        new FrameworkPropertyMetadata(
                                0.4d,
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal),
                        new ValidateValueCallback(OnValidateIconRatio));

        private static bool OnValidateIconRatio(object value)
        {
            var rat = Convert.ToDouble(value);
            return rat > 0d && rat <= 1d;
        }

        public double IconRatio
        {
            get
            {
                return (double)GetValue(IconRatioProperty);
            }
            set
            {
                SetValue(IconRatioProperty, value);
            }
        }


        public event RoutedEventHandler? Click;

        public IconToggle()
        {
            InitializeComponent();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void IconToggleContainerMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsEnableToggleClick)
            {
                Click?.Invoke(this, e);
            }
        }
    }
}
